# GameCore (GameController, GamePhase, GameSnapshot, GameLoop, EntitySpawner) - 코드 리뷰

## 리뷰 대상 파일

| 파일 | 경로 | 역할 |
|------|------|------|
| `GameController.cs` | `Scripts/04.Game/02.System/Game/GameController.cs` | 중앙 오케스트레이터 |
| `GamePhase.cs` | `Scripts/04.Game/02.System/Game/GamePhase.cs` | 게임 상태 열거형 |
| `GameSnapshot.cs` | `Scripts/04.Game/02.System/Game/GameSnapshot.cs` | 게임 상태 스냅샷 |
| `GameLoop.cs` | `Scripts/01.Scene/PlayScene/GameLoop.cs` | MonoBehaviour 브리지 |
| `EntitySpawner.cs` | `Scripts/04.Game/02.System/Entity/EntitySpawner.cs` | Monster/SquadMember 스폰 관리 |

## 리뷰 기준

설계 리뷰(`concept_design_review.md`)에서 도출된 이슈 반영 여부를 중심으로 검증한다.

| # | 기준 | 관련 설계 리뷰 이슈 |
|---|------|---------------------|
| 1 | GameController가 Squad.OnMemberAdded/Removed, EntitySpawner.OnMonsterSpawned/Despawned를 CombatSystem에 이벤트로 연결하는가 | 이슈 4 |
| 2 | Notifier가 public 필드가 아닌 프로퍼티로 노출되는가 | 이슈 10 |
| 3 | CreateSnapshot()에서 SquadMemberSnapshot에 playerPos 파라미터를 전달하는가 | 이슈 2 |
| 4 | RestoreFromSnapshot()에서 Player.SetPosition()을 사용하는가 (View 직접 접근 X) | 이슈 3 |
| 5 | Update()에서 UnitCombat.Tick(dt)이 모든 유닛 대상으로 호출되는가 | 설계 문서 데이터 흐름 |
| 6 | EntitySpawner가 OnMonsterSpawned/Despawned 이벤트를 발행하는가 | 이슈 4 |

---

## 설계 리뷰 반영 검증

| # | 기준 | 충족 | 비고 |
|---|------|------|------|
| 1 | Squad/EntitySpawner 이벤트 → CombatSystem 연결 | O | `GameController.cs:52-57` — 4개 이벤트 모두 구독 |
| 2 | Notifier 프로퍼티 노출 | O | `GameController.cs:24` — `public Notifier Notifier { get; } = new()` |
| 3 | SquadMemberSnapshot에 playerPos 전달 | O | `GameController.cs:83-86` — playerPos 계산 후 전달 |
| 4 | Player.SetPosition() 사용 | O | `GameController.cs:103` — `Player.SetPosition(snapshot.PlayerPosition)` |
| 5 | 모든 유닛 UnitCombat.Tick(dt) | △ | Player, Monster는 Tick 호출됨. **SquadMember는 누락** (이슈 1 참조) |
| 6 | EntitySpawner 이벤트 발행 | O | `EntitySpawner.cs:17-18` 선언, `:32`/`:46` 발행 |

---

## 긍정적인 점

- **설계 리뷰 이슈 충실 반영**: 설계 리뷰에서 지적된 10개 이슈 중 이번 리뷰 범위에 해당하는 6개 기준이 거의 모두 정확히 구현되었다. 특히 CombatSystem 유닛 등록 흐름(이슈 4)은 이벤트 기반 자동 등록으로 Mediator 패턴을 깨끗하게 유지한다.
- **UnitCombat의 Time 의존 제거**: `UnitCombat`이 `elapsed` 누적 방식으로 구현되어(이슈 6) EditMode 단위 테스트가 가능한 구조이다.
- **SetPosition 캡슐화**: `Character.SetPosition()`을 통해 View 접근을 계층 내로 캡슐화(이슈 3)하여 외부 코드가 View를 직접 참조하지 않는다.
- **EntitySpawner의 SpatialGrid 주입**: 설계 문서에는 EntitySpawner 생성자에 SpatialGrid가 없었으나, 구현에서 `unitGrid`를 주입받아 Monster 생성 시 전달하는 방식으로 개선하였다. MonsterAI가 SpatialGrid를 필요로 하므로 올바른 판단이다.
- **이벤트 델리게이트 타입 호환**: `Squad.OnMemberAdded`는 `Action<SquadMember>`, `EntitySpawner.OnMonsterSpawned`는 `Action<Monster>`이고, `CombatSystem.RegisterUnit`은 `Action<IUnit>`을 받는다. C#의 `Action<T>` 반공변성(contravariance)에 의해 정상 동작하며, 별도 래퍼 없이 깔끔한 구독이 가능하다.
- **GameLoop의 간결한 브리지 역할**: `GameLoop`는 SerializeField 연결과 `Update()` 위임만 담당하여 MonoBehaviour 책임을 최소화했다.

---

## 이슈

### 1. SquadMember의 UnitCombat.Tick(dt) 미호출 (중요도: 높음)

**파일**: `GameController.cs:60-78`, `Squad.cs:43-50`

설계 문서의 데이터 흐름에서 GameController.Update()는 "player.Move(direction) + Combat.Tick(dt)"로 모든 유닛의 전투 쿨다운을 관리한다고 명시한다. 실제 구현에서 Player(`GameController.cs:69`)와 Monster(`EntitySpawner.cs:56`)는 `Combat.Tick(dt)`가 호출되지만, **SquadMember는 `Squad.Update()` 내에서 이동만 처리**하고 `Combat.Tick(dt)`가 호출되지 않는다.

```csharp
// Squad.cs:43-50 — 이동만 처리, Combat.Tick 누락
public void Update(Transform leader, ObstacleGrid obstacleGrid, float deltaTime)
{
    foreach (SquadMember member in members)
    {
        Vector2 direction = flock.CalculateDirection(member, members, leader, obstacleGrid);
        member.Move(direction);
    }
}
```

SquadMember의 `Combat.Tick(dt)`이 호출되지 않으면 `elapsed`가 증가하지 않아 **쿨다운이 영원히 리셋되지 않는다**. 단, `UnitCombat` 생성자에서 `elapsed = cooldown`으로 초기화하므로 첫 공격은 가능하나, `ResetCooldown()` 호출 후 다시 공격 가능 상태로 복귀하지 못한다. 결과적으로 SquadMember는 **첫 공격 이후 더 이상 공격할 수 없는** 버그가 된다.

**제안**: `Squad.Update()`에서 각 멤버의 `Combat.Tick(deltaTime)`을 호출하거나, `GameController.Update()`에서 별도로 호출한다.

```csharp
// Squad.Update()에서 추가
foreach (SquadMember member in members)
{
    member.Combat.Tick(deltaTime);
    Vector2 direction = flock.CalculateDirection(member, members, leader, obstacleGrid);
    member.Move(direction);
}
```

---

### 2. EntitySpawner.Update() 순회 중 컬렉션 변경 위험 (중요도: 높음)

**파일**: `EntitySpawner.cs:53-58`

```csharp
public void Update(float deltaTime)
{
    foreach (var monster in activeMonsters)
    {
        monster.Combat.Tick(deltaTime);
        monster.Update();
    }
}
```

`monster.Update()`는 `MonsterAI.Update()`를 호출하며, AI 상태 전이 과정에서 전투 → 사망 → `TamingSystem.OnUnitDeath()` → `entitySpawner.DespawnMonster()` 흐름이 발생할 수 있다. `DespawnMonster()`는 `activeMonsters.Remove(monster)`를 수행하므로 **foreach 순회 중 컬렉션이 변경**되어 `InvalidOperationException`이 발생한다.

현재 `CombatSystem.Update()`가 별도로 호출되므로 즉시 문제가 되지 않을 수 있으나, 향후 AI에서 직접 사망 처리를 하거나 DespawnMonster가 다른 경로로 호출될 경우 런타임 크래시로 이어진다.

**제안**: 순회 전 리스트를 복사하거나, 삭제 대상을 별도로 수집하여 순회 후 처리한다.

```csharp
public void Update(float deltaTime)
{
    // 복사본으로 순회
    var snapshot = new List<Monster>(activeMonsters);
    foreach (var monster in snapshot)
    {
        monster.Combat.Tick(deltaTime);
        monster.Update();
    }
}
```

---

### 3. 설계 문서 대비 구현 차이 — GameController 생성자 파라미터 및 시스템 누락 (중요도: 중간)

**파일**: `GameController.cs:27-30` vs `design/game_controller.md:48-54`

| 항목 | 설계 문서 | 실제 구현 | 판단 |
|------|-----------|-----------|------|
| 생성자 파라미터 | `PlayerView, PlayerInput, FogOfWar, Minimap, ObstacleGrid, Transform cameraTransform` (6개) | `PlayerView, PlayerInput, ObstacleGrid` (3개) | 단계적 구현으로 판단됨 |
| CombatSystem 생성자 | `new CombatSystem(Notifier)` | `new CombatSystem(unitGrid, Notifier)` | 구현이 개선된 형태 |
| EntitySpawner 생성자 | `new EntitySpawner()` | `new EntitySpawner(unitGrid)` | 구현이 개선된 형태 |
| VFX 시스템 (HitStop, CameraShake, HitEffectPlayer) | 포함 | 미포함 | 단계적 구현 예상 |
| FogOfWar, Minimap | 포함 | 미포함 | 단계적 구현 예상 |

CombatSystem과 EntitySpawner에 `SpatialGrid<IUnit>` 주입을 추가한 것은 설계 대비 **개선**이다. `SpatialGrid`를 GameController에서 생성하여 공유 자원으로 관리하는 패턴이 설계 의도("공유 SpatialGrid")에 부합한다.

VFX, FogOfWar, Minimap 누락은 단계적 구현 계획에 따른 것으로 판단되나, 설계 문서와의 차이를 **구현 계획 문서에 명시**하여 누락이 아님을 기록해 두는 것을 권장한다.

---

### 4. GameController 이벤트 구독 해제 미구현 (중요도: 중간)

**파일**: `GameController.cs:52-57`

```csharp
Squad.OnMemberAdded   += combatSystem.RegisterUnit;
Squad.OnMemberRemoved += combatSystem.UnregisterUnit;
entitySpawner.OnMonsterSpawned   += combatSystem.RegisterUnit;
entitySpawner.OnMonsterDespawned += combatSystem.UnregisterUnit;
```

GameController가 소유한 객체의 이벤트를 구독하지만, **구독 해제(unsubscribe) 메서드나 IDisposable 패턴이 없다**. GameController가 GameLoop와 동일한 생명주기를 가지므로 현재는 문제가 되지 않으나, 씬 전환이나 재시작 시 GameLoop가 파괴되면서 GC가 이벤트 체인 전체를 회수해야 한다.

Unity에서는 MonoBehaviour 파괴 시 이벤트 구독이 자동으로 정리되지 않으므로, 반복적인 씬 로드/언로드 시 **메모리 누수 가능성**이 있다.

**제안**: `GameController`에 `Dispose()` 또는 `Cleanup()` 메서드를 추가하고, `GameLoop.OnDestroy()`에서 호출한다.

```csharp
// GameController
public void Cleanup()
{
    Squad.OnMemberAdded   -= combatSystem.RegisterUnit;
    Squad.OnMemberRemoved -= combatSystem.UnregisterUnit;
    entitySpawner.OnMonsterSpawned   -= combatSystem.RegisterUnit;
    entitySpawner.OnMonsterDespawned -= combatSystem.UnregisterUnit;
}

// GameLoop
private void OnDestroy() => gameController?.Cleanup();
```

---

### 5. GameSnapshot에 복수 클래스 정의 (중요도: 낮음)

**파일**: `GameSnapshot.cs`

하나의 파일에 `GameSnapshot`, `SquadMemberSnapshot`, `MonsterSnapshot` 3개 클래스가 정의되어 있다. 코딩 컨벤션(`csharp_coding_convention.md`)에서 "한 클래스는 한 파일로 분리한다"를 원칙으로 하되 "대표 클래스의 하위 클래스들이고, 함께 보는 것이 가독성에 유리한 경우 일부 예외 허용"으로 명시하고 있다.

`SquadMemberSnapshot`과 `MonsterSnapshot`은 `GameSnapshot`의 구성 요소이므로 **예외 허용 범위에 해당**한다고 볼 수 있으나, 각 스냅샷 클래스가 독립적인 생성자 로직을 가지고 있어 파일 분리도 고려할 수 있다.

**제안**: 현재 구조가 가독성에 유리하다면 유지해도 무방하다. 다만 스냅샷 클래스가 늘어나거나 로직이 복잡해질 경우 분리를 검토한다.

---

### 6. RestoreFromSnapshot()에서 몬스터 복원 누락 (중요도: 중간)

**파일**: `GameController.cs:95-113`

```csharp
public void RestoreFromSnapshot(GameSnapshot snapshot)
{
    // 기존 몬스터 정리
    var monsters = new System.Collections.Generic.List<Monster>(entitySpawner.ActiveMonsters);
    foreach (var monster in monsters)
        entitySpawner.DespawnMonster(monster);

    // 플레이어 위치 복원
    Player.SetPosition(snapshot.PlayerPosition);

    // 부대원 복원
    Squad.Clear();
    foreach (var memberSnap in snapshot.SquadMembers)
    {
        var pos    = snapshot.PlayerPosition + memberSnap.PositionOffset;
        var member = entitySpawner.SpawnSquadMember(memberSnap.Data, pos);
        Squad.AddMember(member);
    }
}
```

설계 문서(`game_controller.md:133-153`)에서는 몬스터 정리, 플레이어 복원, **부대원 복원**, **안개 복원**을 수행한다. 구현에서는 몬스터를 정리하지만 **스냅샷에 저장된 몬스터를 다시 스폰하는 로직이 없다**. `GameSnapshot.Monsters` 리스트가 존재하고 `MonsterSnapshot`에 `Data`, `Position`, `CurrentHp`가 모두 기록되어 있으나 복원에 사용되지 않는다.

안개(`FogOfWar`) 복원은 단계적 구현으로 누락된 것으로 판단되나, 몬스터 복원은 이미 스냅샷 데이터가 준비되어 있으므로 의도적 누락인지 확인이 필요하다.

**제안**: 몬스터 복원 로직을 추가한다.

```csharp
// 몬스터 복원
foreach (var monsterSnap in snapshot.Monsters)
{
    var monster = entitySpawner.SpawnMonster(monsterSnap.Data, monsterSnap.Position);
    // HP 복원이 필요하면 monster.Health.SetCurrentHp(monsterSnap.CurrentHp) 등 추가
}
```

---

## 설계 일관성 요약

| 항목 | 설계 문서 | 실제 구현 | 판단 |
|------|-----------|-----------|------|
| Notifier 프로퍼티 | `public Notifier Notifier { get; } = new()` | 동일 | 일치 |
| CombatSystem 유닛 등록 | 이벤트 기반 자동 등록 | 동일 | 일치 |
| SquadMemberSnapshot(playerPos) | 2개 파라미터 | 동일 | 일치 |
| Player.SetPosition() | Character.SetPosition() 경유 | 동일 | 일치 |
| SpatialGrid 주입 | 설계에 부분 명시 | GameController에서 생성/공유 | 구현이 개선 |
| Monster 생성자 | `new Monster(view, data)` | `new Monster(view, data, unitGrid)` | 구현이 개선 |
| VFX, FogOfWar, Minimap | 포함 | 미포함 | 단계적 구현 |

---

## 코드 리뷰 체크리스트

| 항목 | 충족 | 비고 |
|------|------|------|
| 설계 일관성 | △ | 핵심 구조 일치. SquadMember Tick 누락, 몬스터 복원 누락 |
| 코딩 컨벤션 준수 | O | PascalCase 프로퍼티, camelCase 필드, 접근 제한자 명시 등 준수 |
| 에러 처리 | △ | 컬렉션 순회 중 변경 위험, 이벤트 구독 해제 미구현 |
| 캡슐화 | O | View 직접 접근 없음, 내부 시스템 private, 적절한 IReadOnlyList 노출 |
| 테스트 존재 | X | 해당 파일에 대한 유닛 테스트 미확인 (별도 리뷰 범위) |

---

## 종합 평가

| 항목 | 등급 | 설명 |
|------|------|------|
| 설계 반영도 | **B+** | 설계 리뷰 이슈 6개 중 5개 완벽 반영, SquadMember Tick 누락 1건 |
| 구조 설계 | **A** | Mediator 패턴 유지, 이벤트 기반 느슨한 결합, SpatialGrid 공유 개선 |
| 구현 품질 | **B** | 핵심 로직은 정확하나 런타임 안정성 이슈(컬렉션 변경, Tick 누락) 존재 |
| 컨벤션 준수 | **A** | 명명 규칙, 접근 제한자, 코드 스타일 모두 컨벤션에 부합 |

### 우선 보강이 필요한 3가지

1. **SquadMember의 UnitCombat.Tick(dt) 호출 추가** — 이것이 없으면 부대원이 첫 공격 이후 전투 불능 상태가 되는 게임플레이 버그가 발생한다.
2. **EntitySpawner.Update() 순회 안전성 확보** — foreach 중 컬렉션 변경 가능성을 차단하여 런타임 크래시를 예방한다.
3. **RestoreFromSnapshot() 몬스터 복원 로직 추가** — 스냅샷 데이터가 준비되어 있으나 복원에 사용되지 않아 저장/불러오기 시 몬스터가 사라진다.
