# 월드맵(WorldMap) 구현 계획 리뷰

## 리뷰 대상 문서

| 문서 | 경로 |
|------|------|
| `implementation_plan_world_map.md` | `Assets/Docs/System/Phase2_Core/implementation_plan_world_map.md` |

---

## 구현 컨벤션 충족도

| 항목 | 충족 | 비고 |
|------|------|------|
| 1. 기술 스택 | X | 섹션 자체가 없음 |
| 2. 시스템 설계 (클래스 목록, 책임, 의존성) | △ | "관련 파일" 테이블이 있으나 책임/의존성 설명 없음 |
| 3. 구현 순서 (병렬 가능 여부 명시) | △ | 10단계 순서는 명확하나 `[병렬 가능]` 마커 없음 |
| 4. 테스트 계획 (유닛/수동 검증) | △ | 검증 체크리스트만 있고, 유닛 테스트 대상 명시 없음 |

---

## 긍정적인 점

- **Unity 에디터 작업 가이드 수준의 구체성**: Inspector 설정값(PPU, Filter Mode, Sorting Layer, Render Order 등)까지 명시하여 작업자가 문서만 보고도 진행할 수 있다.
- **에셋 출처가 명확**: 사용 에셋 테이블에 파일 경로와 용도를 함께 기재하여 에셋 탐색 시간을 줄였다.
- **검증 체크리스트 존재**: Play Mode 진입 시 에러 없음을 마지막 확인 항목으로 포함한 점이 실용적이다.
- **프리팹 계층 구조 명시**: 컴포넌트와 역할이 함께 표기된 계층 다이어그램이 구현 방향을 명확히 한다.

---

## 이슈

### 1. 기술 스택 섹션 누락 (중요도: 높음)

컨벤션 필수 항목 #1이 없다. 이 구현 계획은 특정 Unity 패키지에 의존하므로 명시가 필요하다.

포함되어야 할 항목:

| 항목 | 내용 |
|------|------|
| Unity 2D Tilemap | Grid/Tilemap/TilemapRenderer 컴포넌트 |
| Tile Palette | Window → 2D → Tile Palette 기능 |
| 2D Sprite (PPU 64) | 픽셀 아트 기준 PPU 통일 기준 |

---

### 2. 테스트 계획 섹션 누락 (중요도: 높음)

검증 체크리스트가 있지만 컨벤션이 요구하는 **테스트 계획 섹션**이 없다. 유닛 테스트 대상과 수동 검증 항목이 분리되어야 한다.

이 구현 계획에서 유닛 테스트 가능한 항목:

| 대상 | 테스트 방법 |
|------|------------|
| `ObstacleGrid.IsWalkable()` | 장애물 타일 위치를 mock하여 true/false 검증 |
| `ObstacleGrid.WorldToGrid()` / `GridToWorld()` | 좌표 변환 역산 일치 검증 |
| `MapGenerator.Generate()` | Tilemap mock → ObstacleGrid 생성 후 walkable 배열 검증 |

수동 검증 항목(Play Mode):
- Ground 타일맵 50×50 페인팅 확인
- Obstacles 타일 → 실제 이동 불가 처리 확인
- Decoration 오브젝트 Z-Sort 이상 없음

---

### 3. 병렬 실행 가능 항목 미표시 (중요도: 중간)

Step 7(Decoration 배치)과 Step 8(Buildings 배치)은 서로 독립적이어서 병렬로 진행할 수 있다. 컨벤션은 `[병렬 가능]` 마커 사용을 요구하지만 표시가 없다.

또한 Step 1(스프라이트 설정)과 Step 2(Tile 에셋 생성)는 실질적으로 연속 작업이지만, Step 5(Ground 페인팅)와 Step 6(Obstacles 배치)도 동일 Tilemap 편집이 아닌 **별도 레이어** 작업이므로 병렬 가능하다.

**수정 방향**: Step 7, 8에 `[병렬 가능]` 마커 추가. Step 5, 6도 검토 후 표시.

---

### 4. 시스템 설계 섹션 부재 (중요도: 중간)

"관련 파일" 테이블은 파일과 역할만 나열하며, 컨벤션이 요구하는 **클래스별 책임과 의존성** 서술이 없다. 특히 `MapGenerator → ObstacleGrid` 데이터 흐름과 `GameLoop → GameController` 전달 경로가 구현 관점에서 기술되어야 한다.

**수정 방향**: "관련 파일" 테이블을 확장하거나 "시스템 설계" 섹션을 별도로 추가:
- `MapGenerator.Generate()`: Tilemap → bool[,] walkable 변환 책임
- `GameLoop`: MapGenerator.Generate() 호출 → ObstacleGrid를 GameController에 전달 책임

---

### 5. PlayerSpawn 코드 연동 처리 미완성 (중요도: 중간)

Step 9에 다음 내용이 있다.

> 추후 `Player.SetPosition(spawnPoint.position)` 추가

"추후"로 남겨져 있어서 이 구현 계획의 범위인지, 다음 작업의 범위인지 불명확하다. `GameLoop`가 PlayerSpawn Transform을 SerializeField로 받아 `Player.SetPosition()`을 호출하는 코드가 이 계획에 포함되어야 하는지 확인이 필요하다.

현재 `GameLoop`의 `Start()`가 MapGenerator.Generate() → GameController 생성까지만 처리하면 플레이어는 `(0,0)`에 스폰되어 Obstacles 위에 생성될 수 있다.

**수정 방향**: Step 9에 "GameLoop에 PlayerSpawn 연동" 서브스텝을 추가하고 범위를 명시.

---

### 6. Obstacles 타일과 Decoration 오브젝트 위치 동기화 미언급 (중요도: 낮음)

Step 6(Obstacles 타일)과 Step 7(Decoration 배치)은 서로 위치가 일치해야 한다. 나무가 있는 곳에는 Obstacle 타일이 있어야 하고, Decoration 오브젝트가 없는 곳의 Obstacle 타일은 불필요하다. 이 동기화 작업의 순서나 방법이 명시되어 있지 않다.

**수정 방향**: Step 7에 "Obstacles 타일과 위치 일치 확인" 체크 항목 추가.

---

## 종합 평가

| 항목 | 등급 | 설명 |
|------|------|------|
| 컨벤션 충족도 | **C+** | 기술 스택·테스트 계획 2개 필수 섹션 누락 |
| 작업 구체성 | **A** | Unity 에디터 작업을 문서만으로 따라할 수 있는 수준 |
| 에셋 명세 | **A** | 파일 경로·용도·Inspector 설정값까지 상세 기술 |
| 코드-에디터 통합 흐름 | **B** | MapGenerator↔GameLoop 연동은 명시되었으나 PlayerSpawn 연동이 미완성 |

### 구현 시작 전 보완 권장 사항 3가지

1. **기술 스택 섹션 추가** — Tilemap 패키지, PPU 기준 명시
2. **테스트 계획 섹션 추가** — ObstacleGrid 유닛 테스트 대상 분리
3. **PlayerSpawn 코드 연동 범위 확정** — Step 9에 GameLoop 수정 서브스텝 포함 여부 결정
