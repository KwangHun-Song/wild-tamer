# GC 압력 분석 보고서

> 작성일: 2026-03-03
> 분석 배경: 스쿼드 ~200마리 소환 시 매 프레임 GC 1.5MB 누적 발생

---

## 핵심 요약

200마리 스쿼드 기준으로 **프레임당 곱셈**이 일어나는 할당이 주범이다.
크게 4가지 카테고리로 분류된다:

| 카테고리 | 대표 증상 |
|---------|---------|
| LINQ 메서드 (per-unit) | `Aggregate()`, `Where().ToList()` — 유닛 수만큼 할당 |
| SpatialGrid 쿼리 결과 | 매 쿼리마다 `new List<T>()` 생성 |
| Update 루프 내 임시 컬렉션 | `new List<>()` / `ToList()` 매 프레임 생성 |
| Coroutine yield 객체 | `new WaitForSeconds()` 등 매번 생성 |

---

## 1. CRITICAL — 유닛 수 곱셈 할당 (per-unit per-frame)

### 1-1. `CharacterView.UpdateFacing()` — LINQ Aggregate
- **파일:** `Assets/Scripts/04.Game/01.Entity/Common/CharacterView.cs:191`
- **코드:**
  ```csharp
  var averageDirection = directionQueue.Aggregate((a, b) => a + b) / directionQueue.Count;
  ```
- **문제:** `Aggregate()` 가 IEnumerator + 람다 클로저를 매 호출마다 할당
- **호출 빈도:** 이동 중인 유닛 1마리당 매 프레임 1회 → 200마리 = **200회/프레임**
- **수정 방향:** 수동 for 루프로 교체

---

### 1-2. `CharacterView.LateUpdate()` — new Vector3 per frame
- **파일:** `Assets/Scripts/04.Game/01.Entity/Common/CharacterView.cs:61`
- **코드:**
  ```csharp
  transform.position = new Vector3(pos.x, pos.y, pos.y);
  ```
- **문제:** struct 타입이라 스택 할당이므로 직접 GC 원인은 아님. 단, 매 프레임 transform.position setter 호출이 내부적으로 unmanaged 처리. 영향 미미하나 확인 필요.
- **호출 빈도:** 유닛 1마리당 매 프레임 1회 → 200마리 = **200회/프레임**
- **수정 방향:** 실제 GC 원인인지 Profiler로 재검증 필요

---

## 2. HIGH — SpatialGrid 쿼리 결과 List 매번 생성

### 2-1. `SpatialGrid.Query()` — new List 반환
- **파일:** `Assets/Scripts/04.Game/02.System/Spatial/SpatialGrid.cs:32`
- **코드:**
  ```csharp
  var result = new List<T>();
  ...
  return result;
  ```
- **문제:** Query() 호출마다 새 List<T> 힙 할당
- **Query() 호출 위치:**
  | 호출처 | 빈도 |
  |--------|------|
  | `CombatSystem` — 공격 가능 유닛 탐색 | 유닛 수 × 프레임 |
  | `MonsterSquad.Update()` — 적 탐지 | 스쿼드 수 × 프레임 |
  | `MonsterStandaloneFSM` — 감지/공격 | 스탠드얼론 몬스터 수 × 프레임 |
  | `SquadMemberFSM` — 공격 대상 탐색 | 스쿼드 멤버 수 × 프레임 |
  | `BossIdleState`, `BossPatternUtils` | 보스 상태별 |
- **수정 방향:** 결과 리스트를 외부에서 전달받는 오버로드 추가 (`Query(center, radius, List<T> result)`) 또는 ArrayPool<T> 활용

---

## 3. HIGH — Update 루프 내 임시 컬렉션

### 3-1. `MonsterSquad.Update()` — Where().ToList()
- **파일:** `Assets/Scripts/04.Game/02.System/Squad/MonsterSquad.cs:63`
- **코드:**
  ```csharp
  var aliveMembers = members.Where(m => m.IsAlive).ToList();
  ```
- **문제:** LINQ Where 이터레이터 + ToList() 결과 List 매 프레임 할당
- **호출 빈도:** 활성 MonsterSquad 수 × 프레임
- **수정 방향:** 멤버 필드 List를 재사용하거나 IsAlive 상태 변경 시점에만 갱신

---

### 3-2. `Squad.Update()` — new List x2
- **파일:** `Assets/Scripts/04.Game/02.System/Squad/Squad.cs:67, 75`
- **코드:**
  ```csharp
  var stopped = new List<SquadMember>();          // line 67
  var stopSnapshot = new List<SquadMemberSnapshot>(stopped); // line 75
  ```
- **문제:** 플레이어 스쿼드 Update 매 프레임 List 2개 신규 생성
- **호출 빈도:** 매 프레임 1회 (플레이어 스쿼드)
- **수정 방향:** 클래스 필드로 끌어올려 Clear() 후 재사용

---

### 3-3. `EntitySpawner.Update()` — 스냅샷 List
- **파일:** `Assets/Scripts/04.Game/02.System/Entity/EntitySpawner.cs:144`
- **코드:**
  ```csharp
  var snapshot = new List<Monster>(activeMonsters);
  ```
- **문제:** 스탠드얼론 몬스터 업데이트용 스냅샷을 매 프레임 생성
- **호출 빈도:** 매 프레임 1회
- **수정 방향:** 필드 List로 끌어올려 Clear() + AddRange() 재사용

---

### 3-4. `MonsterSquadSpawner.TryDespawnFarSquads()` — ToList()
- **파일:** `Assets/Scripts/04.Game/02.System/Entity/MonsterSquadSpawner.cs:96`
- **코드:**
  ```csharp
  foreach (var squad in entitySpawner.ActiveSquads.ToList())
  ```
- **문제:** 매 프레임 ActiveSquads의 ToList() 스냅샷 생성
- **호출 빈도:** 매 프레임 1회
- **수정 방향:** 삭제 대상을 별도 List에 모아 2-pass 처리하되 해당 List는 재사용

---

### 3-5. `GameController.HandleBossSpawned()` — ToList() x2
- **파일:** `Assets/Scripts/04.Game/02.System/Game/GameController.cs:186-189`
- **코드:**
  ```csharp
  foreach (var squad in entitySpawner.ActiveSquads.ToList())
  foreach (var monster in entitySpawner.ActiveMonsters.ToList())
  ```
- **문제:** 보스 스폰 이벤트 시 스냅샷 생성
- **호출 빈도:** 보스 등장 시 1회 (빈번하지 않음)
- **수정 방향:** 우선순위 낮음. 필요 시 필드 List 재사용

---

### 3-6. `BossChaseState.OnUpdate()` — new List<int>
- **파일:** `Assets/Scripts/04.Game/01.Entity/Boss/States/BossChaseState.cs:57`
- **코드:**
  ```csharp
  var ready = new List<int>();
  ```
- **문제:** 보스가 Chase 상태일 때 매 프레임 List<int> 생성
- **호출 빈도:** 보스 Chase 상태 매 프레임
- **수정 방향:** 필드로 끌어올려 Clear() 재사용 또는 고정 배열로 교체

---

## 4. MEDIUM — Coroutine yield 객체 반복 생성

### 4-1. `BossMonsterView.FireRoutine()` — new WaitForSeconds
- **파일:** `Assets/Scripts/04.Game/01.Entity/Boss/BossMonsterView.cs:179`
- **코드:**
  ```csharp
  yield return new WaitForSeconds(data.fireInterval);
  ```
- **문제:** 투사체 발사마다 WaitForSeconds 새 인스턴스 생성
- **수정 방향:** 코루틴 시작 시 한 번만 생성하여 캐싱

---

### 4-2. `HitStop.ApplyHitStop()` — new WaitForSecondsRealtime
- **파일:** `Assets/Scripts/04.Game/02.System/VFX/HitStop.cs:39`
- **코드:**
  ```csharp
  yield return new WaitForSecondsRealtime(duration);
  ```
- **문제:** 타격마다 WaitForSecondsRealtime 새 인스턴스 생성
- **수정 방향:** duration이 고정이라면 필드에 캐싱

---

### 4-3. `AutoDespawn.WaitForParticle()` — new WaitUntil + 람다
- **파일:** `Assets/Scripts/04.Game/02.System/VFX/AutoDespawn.cs:37`
- **코드:**
  ```csharp
  yield return new WaitUntil(() => !particle.IsAlive(withChildren: true));
  ```
- **문제:** VFX 스폰마다 WaitUntil + 람다 클로저 할당
- **수정 방향:** WaitUntil 재사용 불가(캡처 변수 있음), 대안으로 `yield return null` 루프로 교체

---

## 5. 우선순위 요약

| 우선순위 | 항목 | 예상 효과 |
|---------|------|---------|
| 🔴 CRITICAL | `CharacterView.UpdateFacing()` Aggregate → 수동 루프 | 200회/프레임 LINQ 제거 |
| 🔴 CRITICAL | `SpatialGrid.Query()` List 재사용 오버로드 | 쿼리 수 × 프레임 List 제거 |
| 🔴 CRITICAL | `MonsterSquad.Update()` Where().ToList() 제거 | 스쿼드 수 × 프레임 |
| 🟠 HIGH | `Squad.Update()` 임시 List 2개 필드화 | 매 프레임 2 List 제거 |
| 🟠 HIGH | `EntitySpawner.Update()` 스냅샷 List 필드화 | 매 프레임 1 List 제거 |
| 🟠 HIGH | `MonsterSquadSpawner` ToList() 제거 | 매 프레임 1 List 제거 |
| 🟡 MEDIUM | `BossChaseState` new List<int> 필드화 | 보스 Chase 중 매 프레임 |
| 🟡 MEDIUM | WaitForSeconds 계열 캐싱 | 이벤트 기반, 상대적 소량 |
| 🟢 LOW | AutoDespawn WaitUntil → yield null 루프 | VFX 생성 시 1회 |

---

## 6. 수정 시 주의사항

- `SpatialGrid.Query()` 오버로드 추가 시 기존 반환형 API를 유지하거나, 모든 호출처를 함께 수정
- `MonsterSquad.aliveMembers` 캐시는 멤버 사망 이벤트(`OnDeath`) 트리거 시점에 동기화 필요
- `Squad.stopped` 필드 재사용 시 Clear() 누락 주의
- `EntitySpawner` 스냅샷 List는 이터레이션 중 원본 수정(Despawn)이 일어나므로 스냅샷 목적이 있음 — 유지하되 인스턴스만 재사용
