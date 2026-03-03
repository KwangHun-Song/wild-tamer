# 최적화 현황

> 최종 업데이트: 2026-03-03

---

## 적용 완료

### 1. 스프라이트 배칭 — Camera Custom Sort Axis
- **문제**: 모든 유닛/오브젝트가 Z=Y로 개별 Z값을 가져 배칭 불가 → Batches ≈ 유닛 수
- **해결**: `QuarterViewCamera.Awake()`에서 `TransparencySortMode.CustomAxis` + `{0,1,0}` 설정, 전체 Z=Y 코드 제거
- **영향 범위**: CharacterView, ObstacleView, MapGenerator, MapDecorationGenerator, MapScatterGenerator, BossMonsterView, ZoneIndicatorView, SortingOrder
- **효과**: 동일 머티리얼/텍스처 스프라이트들의 배칭 가능, SetPass Call 대폭 감소
- **상세**: [sprite_batching.md](sprite_batching.md)

### 2. LINQ Aggregate 제거 — CharacterView.UpdateFacing()
- **문제**: `directionQueue.Aggregate()` 가 유닛당 매 프레임 IEnumerator + 람다 클로저 할당 (200마리 = 200회/프레임)
- **해결**: 수동 `foreach` 루프로 교체
- **효과**: 유닛 수 × 프레임 GC 할당 제거

---

## 미적용 (우선순위순)

### CRITICAL

| # | 항목 | 위치 | 문제 | 수정 방향 |
|---|------|------|------|-----------|
| 1 | SpatialGrid.Query() List 매번 생성 | `SpatialGrid.cs:32` | Query() 호출마다 `new List<T>()` 힙 할당. CombatSystem·MonsterSquad·FSM 등에서 유닛 수 × 프레임 호출 | 결과 List를 외부에서 전달받는 오버로드 추가 |
| 2 | MonsterSquad.Update() Where().ToList() | `MonsterSquad.cs:63` | LINQ Where 이터레이터 + ToList() 매 프레임 할당 | 필드 List 재사용 또는 IsAlive 변경 시점 캐시 |

### HIGH

| # | 항목 | 위치 | 문제 | 수정 방향 |
|---|------|------|------|-----------|
| 3 | Squad.Update() 임시 List 2개 | `Squad.cs:67,75` | 매 프레임 `new List<SquadMember>()` × 2 | 클래스 필드로 승격 후 Clear() 재사용 |
| 4 | EntitySpawner.Update() 스냅샷 List | `EntitySpawner.cs:144` | 매 프레임 `new List<Monster>(activeMonsters)` | 필드 List로 Clear() + AddRange() 재사용 |
| 5 | MonsterSquadSpawner ToList() | `MonsterSquadSpawner.cs:96` | 매 프레임 ActiveSquads.ToList() | 삭제 대상 별도 List에 모아 2-pass 처리 |

### MEDIUM

| # | 항목 | 위치 | 문제 | 수정 방향 |
|---|------|------|------|-----------|
| 6 | BossChaseState new List\<int\> | `BossChaseState.cs:57` | 보스 Chase 중 매 프레임 List 생성 | 필드로 승격 후 Clear() 재사용 |
| 7 | WaitForSeconds 계열 캐싱 | BossMonsterView, HitStop | 이벤트마다 yield 객체 새로 생성 | 코루틴 시작 시 1회 생성 후 캐싱 |

### LOW

| # | 항목 | 위치 | 문제 | 수정 방향 |
|---|------|------|------|-----------|
| 8 | AutoDespawn WaitUntil + 람다 | `AutoDespawn.cs:37` | VFX 스폰마다 WaitUntil + 클로저 할당 | `yield return null` 루프로 교체 |
| 9 | GameController 보스 스폰 시 ToList() ×2 | `GameController.cs:186-189` | 보스 등장 시 1회 (빈도 낮음) | 필요 시 필드 List 재사용 |

---

## 참고 문서
- [GC 압력 분석 보고서](gc_analysis.md) — 상세 프로파일링 결과 및 코드 위치
- [스프라이트 배칭 최적화](sprite_batching.md) — Custom Sort Axis 설계 및 변경 내역
