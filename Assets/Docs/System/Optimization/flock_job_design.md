# FlockBehavior Job System 전환 설계

## 1. 목표

200마리 스쿼드에서 `FlockBehavior.CalculateDirection()` CPU 비용을 Unity Job System + Burst Compiler로 워커 스레드에 분산해 메인 스레드 부하를 최소화한다.

---

## 2. 현재 구조의 병목

```
메인 스레드:
  Squad.Update()
    → for i in 0..200 (스태거드 → 40마리/프레임)
      → FlockBehavior.CalculateDirection()   ← 전부 메인 스레드 직렬 실행
        → CollectNeighbors  O(N)
        → CalculateSeparation
        → CalculateCohesion
        → CalculateFollow
        → CalculateAvoidance
```

- 스태거드로 80% 절감했지만 여전히 메인 스레드 독점
- 유닛 수 증가 시 선형 증가(O(N))

---

## 3. Job System 접근 방식

### 실행 흐름 (3 Phase)

```
Phase 1 [메인 스레드] — 데이터 준비
  positions NativeArray에 Transform.position 복사 (200회)
  설정값(weights, radius 등) Job struct에 설정

Phase 2 [워커 스레드 × N코어] — 병렬 계산
  IJobParallelFor: 인덱스 i마다 독립 실행
    멤버 i의 Separation + Cohesion + Follow 계산
    결과를 directions[i]에 기록

Phase 3 [메인 스레드] — 결과 적용
  job.Complete() 대기
  directions NativeArray → member.SetMoveDirection()
  Avoidance 후처리 (장애물 그리드는 관리 코드 → 메인 스레드 처리)
```

### 핵심 원칙

- Transform 접근은 반드시 메인 스레드 (Phase 1/3)
- IUnit 인터페이스 (가상 함수)는 Job 내부에서 사용 불가 → 위치/결과만 NativeArray로 교환
- Avoidance(ObstacleGrid)는 관리 코드 의존 → Job 제외 후 메인 스레드 후처리

---

## 4. 데이터 모델

### Job에 전달되는 NativeArray

| 배열 | 타입 | 크기 | 방향 |
|------|------|------|------|
| `positions` | `NativeArray<Vector2>` | N (멤버 수) | Read |
| `stoppedFlags` | `NativeArray<bool>` | N | Read |
| `outDirections` | `NativeArray<Vector2>` | N | Write |

### 상수 설정 (Job struct 필드)

```
NeighborRadius, MinSeparationDistance, ArrivalRadius
SeparationWeight, CohesionWeight, FollowWeight
LeaderPos (Vector2)
```

AlignmentWeight, AvoidanceWeight는 Job 외부 처리로 제외.

---

## 5. Avoidance 처리 전략

`ObstacleGrid.IsWalkable()`은 관리 코드(`Dictionary`, `Tilemap` 등)에 의존한다. Burst Job 내에서 호출 불가.

**선택안 A — 메인 스레드 후처리 (추천)**
- Job이 Separation + Cohesion + Follow 방향 계산
- Phase 3에서 메인 스레드가 Avoidance 보정만 추가
- 구현 단순, 성능에 미치는 영향 미미 (Avoidance는 전체 비용의 ~5%)

**선택안 B — 장애물 데이터 Bake**
- ObstacleGrid를 `NativeArray<byte>` 비트맵으로 직렬화 → Job 내부에서 직접 조회
- 구현 복잡, 장애물이 자주 바뀌지 않는 경우 효과적

→ **선택안 A로 진행**

---

## 6. 이웃 탐색 전략

### O(N²) 브루트포스 in Burst (200마리 기준 추천)

- 각 Job 인덱스가 전체 positions 배열을 순회
- 관리 비용 없음, Burst SIMD 벡터화로 실제 처리량 매우 빠름
- 200² = 40,000 연산 → Burst에서 ~0.1ms 이내 예상
- SpatialGrid 대비 로직 단순화

### NativeHashMap 공간 분할 (500마리 이상 필요 시)

- 격자 셀별 NativeMultiHashMap으로 인덱스 사전 분류
- Job 내에서 인접 셀만 조회 → O(N·k)
- 구현 복잡도 높음, 200마리에서는 오버엔지니어링

→ **200마리 시나리오: 브루트포스 채택**

---

## 7. NativeArray 수명 관리

| 시점 | 동작 |
|------|------|
| Squad 생성 | `Allocator.Persistent`로 N 크기 배열 할당 |
| 멤버 수 변경 | 기존 배열 Dispose 후 새 크기로 재할당 |
| Phase 2 | `Schedule()` → JobHandle 반환 |
| Phase 3 | `jobHandle.Complete()` → 결과 읽기 |
| Squad 소멸 | `Dispose()` 필수 |

---

## 8. 기존 스태거드 업데이트와의 관계

Job System 도입 후:
- **스태거드 제거 가능**: 200마리 전부 매 프레임 병렬 계산해도 메인 스레드 비용 미미
- **또는 유지 가능**: Job 내 인덱스 수를 절반으로 줄여 워커 스레드 부하 추가 감소

초기 구현에서는 스태거드 제거 후 전체 계산으로 검증 권장.

---

## 9. 기대 성능

| 시나리오 | 메인 스레드 비용 |
|----------|----------------|
| 현재 (스태거드) | ~4ms (40마리/프레임) |
| Job System (200마리 전체) | ~0.3ms (Phase 1+3 비용만) |

4코어 기준 이론 가속: 4× + Burst SIMD 2~4× = 총 8~16× 빠름

---

## 10. 변경 영향 범위

| 파일 | 변경 |
|------|------|
| `FlockJob.cs` | 신규 — IJobParallelFor 구현 |
| `FlockJobRunner.cs` | 신규 — NativeArray 수명 관리, Schedule/Complete |
| `Squad.cs` | Update() Phase 1/2/3으로 분리, 스태거드 제거 |
| `FlockBehavior.cs` | 유지 (에디터 디버그 / 비-Job 경로용) |
| `SquadContext.cs` | MemberPositions 배열 재사용 가능 |
