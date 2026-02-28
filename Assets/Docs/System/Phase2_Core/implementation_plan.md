# Phase 2: 코어 시스템 - 구현 계획

## 개요

설계 문서([design.md](design.md))를 바탕으로 13개 구현 단계를 정의한다. 각 단계는 독립적으로 빌드·테스트 가능한 단위로 구성하며, 이전 단계가 완료되어야 다음 단계를 시작한다.

```
Layer 1 — 기반          : Step 1 공통 개체  →  Step 2 플레이어  →  Step 3 월드맵  →  Step 4 GameController 골격
Layer 2 — 개체          : Step 5 군집/부대  →  Step 6 몬스터 개체  →  Step 7 몬스터 AI
Layer 3 — 상호작용       : Step 8 자동 전투
Layer 4 — 결과/연출      : Step 9 테이밍  →  Step 10 전투 연출  →  Step 11 스냅샷
Layer 5 — 시야/UI        : Step 12 전장의 안개  →  Step 13 미니맵
```

---

## 병렬 실행 계획 (OMC 멀티에이전트)

의존성 분석을 통해 병렬 실행 가능한 웨이브로 재구성한다. 같은 웨이브 내 스텝은 서로 다른 에이전트가 동시에 진행할 수 있다.

```
Wave A ──────────────────────────────────────────── 병렬
  Agent-1: Step 1  공통 개체 (IUnit, Character, CharacterView, UnitCombat...)
  Agent-2: Step 3  월드맵 (MapGenerator, ObstacleGrid)
           ↓ Wave A 완료
Wave B ──────────────────────────────────────────── 단독 (Step 1 의존)
  Agent-1: Step 2  플레이어 (Player, PlayerView, PlayerInput, Camera)
           ↓ Wave B 완료 (Steps 1 + 2 + 3 완료)
Wave C ──────────────────────────────────────────── 단독 (게이트)
  Agent-1: Step 4  GameController 기본 골격
           ↓ Wave C 완료
Wave D ──────────────────────────────────────────── 병렬
  Agent-1: Step 5  군집/부대 (Squad, SquadMember, FlockBehavior, SpatialGrid)
  Agent-2: Step 6  몬스터 개체 (Monster, MonsterView, MonsterData)
           ↓ Wave D 완료
Wave E ──────────────────────────────────────────── 단독 (게이트)
  Agent-1: Step 7  몬스터 AI + EntitySpawner (MonsterAI, States, EntitySpawner)
           ↓ Wave E 완료
Wave F ──────────────────────────────────────────── 단독 (게이트)
  Agent-1: Step 8  자동 전투 (CombatSystem, DamageProcessor, Notifier 인터페이스)
           ↓ Wave F 완료
Wave G ──────────────────────────────────────────── 병렬
  Agent-1: Step 9  테이밍 (TamingSystem)
  Agent-2: Step 10 전투 연출 (HitStop, CameraShake, HitEffectPlayer)
           ↓ Wave G 완료
Wave H ──────────────────────────────────────────── 병렬
  Agent-1: Step 11 게임 스냅샷 (GameSnapshot)
  Agent-2: Step 12 전장의 안개 (FogOfWar)
           ↓ Wave H 완료
Wave I ──────────────────────────────────────────── 단독
  Agent-1: Step 13 미니맵 (Minimap)
```

### 웨이브별 병렬 이득

| 웨이브 | 병렬 스텝 | 절약 효과 | 게이트 조건 |
|--------|-----------|-----------|-------------|
| A | Step 1 \|\| Step 3 | Step 3(월드맵)이 독립적이므로 즉시 병렬 시작 가능 | — |
| D | Step 5 \|\| Step 6 | 군집 로직과 몬스터 개체가 서로 의존하지 않음 | Step 4 완료 |
| G | Step 9 \|\| Step 10 | 테이밍과 VFX 모두 Notifier 구독만 하면 독립적 | Step 8 완료 |
| H | Step 11 \|\| Step 12 | 스냅샷과 안개는 서로 의존하지 않음 | Wave G 완료 |

### 게이트 스텝 (반드시 단독 완료 후 진행)

| 게이트 | 이유 |
|--------|------|
| Step 4 GameController 골격 | 이후 모든 시스템이 GameController에 주입되므로 인터페이스 확정 필요 |
| Step 7 몬스터 AI | CombatSystem(Step 8)이 SpatialGrid를 공유하므로 MonsterAI 완성 후 진행 |
| Step 8 자동 전투 | Notifier 인터페이스가 확정되어야 Step 9/10의 구독 코드 작성 가능 |

### OMC 팀 실행 예시

Wave D처럼 Step 5와 Step 6을 병렬로 실행하는 경우:

```
/team "Step 5(군집/부대)와 Step 6(몬스터 개체)를 병렬로 구현해줘.
Step 5: Squad, SquadMember, SquadMemberView, FlockBehavior, SpatialGrid
Step 6: Monster, MonsterView, MonsterData, BossPattern
각 파일은 Scripts/04.Game/ 아래 설계 문서 폴더 구조에 맞게 생성할 것."
```

---

## 구현 파일 목록

| 파일 | 경로 | 단계 |
|------|------|:----:|
| `IUnit.cs` | `04.Game/01.Entity/Common/` | 1 |
| `UnitHealth.cs` | `04.Game/01.Entity/Common/` | 1 |
| `UnitMovement.cs` | `04.Game/01.Entity/Common/` | 1 |
| `UnitCombat.cs` | `04.Game/01.Entity/Common/` | 1 |
| `CharacterView.cs` | `04.Game/01.Entity/Common/` | 1 |
| `Character.cs` | `04.Game/01.Entity/Common/` | 1 |
| `PlayerInput.cs` | `04.Game/01.Entity/Player/` | 2 |
| `Player.cs` | `04.Game/01.Entity/Player/` | 2 |
| `PlayerView.cs` | `04.Game/01.Entity/Player/` | 2 |
| `QuarterViewCamera.cs` | `04.Game/01.Entity/Player/` | 2 |
| `ObstacleGrid.cs` | `04.Game/02.System/Map/` | 3 |
| `MapGenerator.cs` | `04.Game/02.System/Map/` | 3 |
| `GamePhase.cs` | `04.Game/02.System/Game/` | 4 |
| `GameController.cs` | `04.Game/02.System/Game/` | 4 |
| `GameLoop.cs` | `01.Scene/PlayScene/` | 4 |
| `SpatialGrid.cs` | `04.Game/02.System/Spatial/` | 5 |
| `FlockBehavior.cs` | `04.Game/01.Entity/Squad/` | 5 |
| `SquadMember.cs` | `04.Game/01.Entity/Squad/` | 5 |
| `SquadMemberView.cs` | `04.Game/01.Entity/Squad/` | 5 |
| `Squad.cs` | `04.Game/02.System/Squad/` | 5 |
| `MonsterData.cs` | `04.Game/03.Data/` | 6 |
| `BossPattern.cs` | `04.Game/03.Data/` | 6 |
| `Monster.cs` | `04.Game/01.Entity/Monster/` | 6 |
| `MonsterView.cs` | `04.Game/01.Entity/Monster/` | 6 |
| `MonsterIdleState.cs` | `04.Game/01.Entity/Monster/States/` | 7 |
| `MonsterChaseState.cs` | `04.Game/01.Entity/Monster/States/` | 7 |
| `MonsterAttackState.cs` | `04.Game/01.Entity/Monster/States/` | 7 |
| `MonsterAI.cs` | `04.Game/01.Entity/Monster/` | 7 |
| `EntitySpawner.cs` | `04.Game/02.System/Entity/` | 7 |
| `IOnHitListener.cs` | `04.Game/01.Entity/Common/` | 8 |
| `IOnUnitDeathListener.cs` | `04.Game/01.Entity/Common/` | 8 |
| `IOnTamingListener.cs` | `04.Game/01.Entity/Common/` | 8 |
| `DamageProcessor.cs` | `04.Game/02.System/Combat/` | 8 |
| `CombatSystem.cs` | `04.Game/02.System/Combat/` | 8 |
| `TamingSystem.cs` | `04.Game/02.System/Combat/` | 9 |
| `HitStop.cs` | `04.Game/02.System/VFX/` | 10 |
| `CameraShake.cs` | `04.Game/02.System/VFX/` | 10 |
| `HitEffectPlayer.cs` | `04.Game/02.System/VFX/` | 10 |
| `GameSnapshot.cs` | `04.Game/02.System/Game/` | 11 |
| `FogOfWar.cs` | `04.Game/02.System/Map/` | 12 |
| `Minimap.cs` | `04.Game/02.System/Map/` | 13 |

총 41개 파일

---

## 단계별 구현 계획

### Step 1 — 공통 개체 기반

**관련 설계**: [entity_common.md](design/entity_common.md)

**구현 파일**

| 파일 | 타입 | 핵심 내용 |
|------|------|-----------|
| `IUnit.cs` | interface | Team, Transform, Health, Combat, IsAlive |
| `UnitHealth.cs` | MonoBehaviour | MaxHp, CurrentHp, TakeDamage(), OnDamaged/OnDeath 이벤트 |
| `UnitMovement.cs` | MonoBehaviour | MoveSpeed, Move()/MoveTo()/Stop() |
| `CharacterView.cs` | MonoBehaviour abstract | [SerializeField] health, movement |
| `UnitCombat.cs` | pure C# | AttackDamage/Range/DetectionRange, Tick(dt), CanAttack |
| `Character.cs` | pure C# abstract | View(protected), Combat, SetPosition() |

**의존성**: 없음 (최초 구현 단위)

**구현 순서**

1. `IUnit` 인터페이스 → `UnitHealth` → `UnitMovement`
2. `CharacterView` (UnitHealth, UnitMovement SerializeField 연결)
3. `UnitCombat` (deltaTime 누적, CanAttack 프로퍼티)
4. `Character` (CharacterView protected, IUnit 위임, SetPosition)

**테스트**

- `UnitCombat` EditMode 단위 테스트: `Tick(0.5f)` × 2 → `CanAttack == true` (cooldown 1.0f 기준)
- `UnitHealth`: `TakeDamage()` 호출 후 `CurrentHp` 감소, `OnDeath` 이벤트 발행 확인
- 컴파일 오류 없음 확인

**리스크**: 낮음

---

### Step 2 — 플레이어 이동 및 입력

**관련 설계**: [player.md](design/player.md)

**구현 파일**

| 파일 | 타입 | 핵심 내용 |
|------|------|-----------|
| `PlayerInput.cs` | MonoBehaviour | GetAxisRaw → MoveDirection 노출 |
| `Player.cs` | pure C# | Move() → OnMoveRequested 이벤트 |
| `PlayerView.cs` | MonoBehaviour | Subscribe(Player) → Movement.Move() 구독 |
| `QuarterViewCamera.cs` | MonoBehaviour | LateUpdate에서 Lerp 추적 |

**의존성**: Step 1

**구현 순서**

1. `PlayerInput` (MoveDirection 프로퍼티)
2. `Player` : `Character` (OnMoveRequested 이벤트, Move())
3. `PlayerView` : `CharacterView` (Subscribe, 이벤트 구독)
4. `QuarterViewCamera` (target Transform Lerp)

**테스트**

- 씬에 PlayerView 프리팹 배치 후 수동 Play: WASD/화살표로 캐릭터 이동 확인
- 카메라가 플레이어를 부드럽게 추적하는지 확인
- PlayerInput을 직접 읽지 않고 GameController 경유 흐름인지 확인 (PlayerView.Update()가 없어야 함)

**리스크**: 낮음 — Input 축 이름("Horizontal"/"Vertical")이 Project Settings에 등록되어 있는지 확인 필요

---

### Step 3 — 월드맵

**관련 설계**: [world_map.md](design/world_map.md)

**구현 파일**

| 파일 | 타입 | 핵심 내용 |
|------|------|-----------|
| `ObstacleGrid.cs` | pure C# | bool[,] walkable, IsWalkable(), WorldToGrid(), GridToWorld() |
| `MapGenerator.cs` | MonoBehaviour | Generate() → ObstacleGrid 빌드, Tilemap 배치 |

**의존성**: 없음 (Step 1과 병렬 가능)

**구현 순서**

1. `ObstacleGrid` (좌표 변환 공식 먼저 정의, 그리드 채우기)
2. `MapGenerator` (Tilemap 읽어 walkable 배열 구성, ObstacleGrid 생성)

**테스트**

- MapGenerator.Generate() 호출 후 ObstacleGrid.IsWalkable() 반환값이 Tilemap 배치와 일치하는지 확인
- 경계값(맵 밖 좌표) 처리 확인

**리스크**: 중간 — WorldToGrid 좌표 변환 공식이 Tilemap 원점 기준과 일치해야 함. 2D 쿼터뷰에서 타일 크기(PPU)와 그리드 셀 크기 불일치 주의

---

### Step 4 — GameController 기본 골격

**관련 설계**: [game_controller.md](design/game_controller.md)

이 단계에서 GameController는 Player 이동만 처리하는 최소 구현으로 시작한다. 이후 단계마다 시스템을 추가 주입한다.

**구현 파일**

| 파일 | 타입 | 핵심 내용 |
|------|------|-----------|
| `GamePhase.cs` | enum | Play, Paused, UpgradeSelection |
| `GameController.cs` | pure C# | Player 소유, Update() (Phase 게이팅, 입력→Player) |
| `GameLoop.cs` | MonoBehaviour | Start()에서 GameController 생성, Update() 위임 |

**의존성**: Step 1, 2, 3

**구현 순서**

1. `GamePhase` enum
2. `GameController` (생성자: PlayerView, PlayerInput, ObstacleGrid, cameraTransform만 받는 최소 버전)
3. `GameLoop` (SerializeField로 씬 참조 주입, mapGenerator.Generate() 후 GameController 생성)

**테스트**

- 씬에 GameLoop 배치, PlayerView/PlayerInput/MapGenerator 연결 후 Play
- 플레이어가 GameController.Update()를 통해 이동하는지 확인
- GamePhase.Paused로 전환 시 이동이 멈추는지 확인

**리스크**: 낮음

**체크포인트 ✓ Layer 1 완료**: 플레이어가 맵에서 이동하고 카메라가 추적한다.

---

### Step 5 — 군집 및 부대 시스템

**관련 설계**: [squad.md](design/squad.md)

**구현 파일**

| 파일 | 타입 | 핵심 내용 |
|------|------|-----------|
| `SpatialGrid.cs` | pure C# generic | Dictionary<Vector2Int, List<T>>, Insert/Query |
| `FlockBehavior.cs` | pure C# | Alignment/Cohesion/Separation/Follow/Avoidance 벡터 합산 |
| `SquadMember.cs` | pure C# | MonsterData 보유, OnMoveRequested 이벤트 |
| `SquadMemberView.cs` | MonoBehaviour | Subscribe(SquadMember) |
| `Squad.cs` | pure C# | members 목록, OnMemberAdded/Removed 이벤트, Update() |

이후 **GameController에 Squad 추가**:

```csharp
// GameController 생성자에 추가
Squad = new Squad();
Squad.OnMemberAdded   += combatSystem.RegisterUnit;   // Step 8에서 연결
Squad.OnMemberRemoved += combatSystem.UnregisterUnit;
```

```csharp
// GameController.Update()에 추가
Squad.Update(Player.Transform, obstacleGrid, dt);
```

**의존성**: Step 1, 3, 4

**구현 순서**

1. `SpatialGrid<T>` (Insert, Query — 반경 내 셀 순회)
2. `FlockBehavior` (각 행동 벡터를 개별 메서드로 분리 후 합산)
3. `SquadMember` + `SquadMemberView` (Step 2의 Player와 동일한 이벤트 패턴)
4. `Squad` (Update에서 FlockBehavior로 방향 계산 후 member.Move() 호출)
5. GameController에 Squad 주입 및 Update 연결

**테스트**

- 씬에 임시로 SquadMember 2~3개 수동 추가 후 Play
- 플레이어를 이동시키면 부대원들이 대형을 유지하며 따라오는지 확인
- 장애물에 막히지 않고 우회하는지 확인
- `SpatialGrid.Query()` EditMode 단위 테스트: 삽입한 아이템이 반경 내에서 조회되는지 확인

**리스크**: 높음 — FlockBehavior 가중치 튜닝이 필요하다. 초기에는 단순 Follow만 구현하고 나머지 행동을 순차적으로 추가할 것. NeighborRadius와 셀 크기 불일치 시 Query가 이웃을 찾지 못할 수 있음.

**체크포인트 ✓**: 부대원들이 플레이어를 따라 군집 이동한다.

---

### Step 6 — 몬스터 개체

**관련 설계**: [monster.md](design/monster.md)

AI 없이 Monster 개체 자체만 구현한다. 이 단계에서 몬스터는 스폰은 되지만 움직이지 않는다.

**구현 파일**

| 파일 | 타입 | 핵심 내용 |
|------|------|-----------|
| `MonsterData.cs` | ScriptableObject | 스탯, tamingChance, prefab, squadPrefab |
| `BossPattern.cs` | ScriptableObject | 보스 패턴 데이터 (stub 수준) |
| `Monster.cs` | pure C# | MonsterData, MonsterView, OnMoveRequested, PlayTamingEffect() |
| `MonsterView.cs` | MonoBehaviour | Subscribe, PlayHitEffect/DeathEffect/TamingEffect |

**의존성**: Step 1, 4

**구현 순서**

1. `MonsterData` ScriptableObject 정의 및 에디터에서 테스트용 에셋 생성
2. `MonsterView` : `CharacterView` (Subscribe, 이펙트 메서드 stub)
3. `Monster` : `Character` (MonsterData 주입, 이벤트 연결, PlayTamingEffect 위임)

**테스트**

- 씬에 MonsterView 프리팹 배치 후 수동으로 `new Monster(view, data)` 생성
- `monster.Health.TakeDamage(10)` 호출 → OnDamaged 이벤트 발행, MonsterView.PlayHitEffect() 실행 확인
- 체력 0 → OnDeath 이벤트 발행 확인

**리스크**: 낮음 — ScriptableObject 프리팹 참조 설정이 번거로울 수 있음 (prefab 슬롯 누락 주의)

---

### Step 7 — 몬스터 AI 및 스폰

**관련 설계**: [monster.md](design/monster.md)

**구현 파일**

| 파일 | 타입 | 핵심 내용 |
|------|------|-----------|
| `MonsterIdleState.cs` | pure C# | UnitGrid 조회 → DetectEnemy 트리거 |
| `MonsterChaseState.cs` | pure C# | 타겟 추적 이동, LoseEnemy/InAttackRange 트리거 |
| `MonsterAttackState.cs` | pure C# | 공격 타이밍, OutOfAttackRange 트리거 |
| `MonsterAI.cs` | pure C# | StateMachine, UnitGrid 주입, 전이 정의 |
| `EntitySpawner.cs` | pure C# | SpawnMonster/SpawnSquadMember/DespawnMonster, 이벤트 |

이후 **GameController에 EntitySpawner 추가**:

```csharp
// GameController 생성자에 추가
entitySpawner = new EntitySpawner();
entitySpawner.OnMonsterSpawned   += combatSystem.RegisterUnit;   // Step 8에서 연결
entitySpawner.OnMonsterDespawned += combatSystem.UnregisterUnit;
```

```csharp
// GameController.Update()에 추가
entitySpawner.Update(dt);
```

**의존성**: Step 5, 6 (SpatialGrid, Monster 필요)

**구현 순서**

1. `MonsterAI` 클래스 및 `MonsterTrigger` enum (StateMachine 연결)
2. `MonsterIdleState` (UnitGrid.Query로 Player 팀 탐지)
3. `MonsterChaseState` (가장 가까운 적을 향해 MoveTo)
4. `MonsterAttackState` (CanAttack 체크, combat.ResetCooldown — 실제 DamageProcessor는 Step 8에서 연결)
5. `EntitySpawner` (Facade.Pool 연동, 이벤트 발행)
6. GameController에 EntitySpawner 주입, Monster.Update() 호출 흐름 연결
7. MonsterAI 생성 시 `combatSystem.UnitGrid` 주입 (Step 8 이후 완성이나 인터페이스 먼저 정의)

**테스트**

- EntitySpawner.SpawnMonster()로 몬스터 1마리 스폰 → 플레이어에게 접근하는지 확인
- DetectionRange 밖에서 시작 → Idle 상태 유지 확인
- DetectionRange 진입 → Chase 상태 전이 확인
- AttackRange 진입 → Attack 상태 전이 확인
- DetectionRange 밖으로 이탈 → Idle 복귀 확인

**리스크**: 높음 — MonsterAI가 SpatialGrid를 참조하는데, Step 8의 CombatSystem이 완성되기 전에는 unitGrid가 빈 상태다. Idle에서 탐지가 동작하려면 CombatSystem.RegisterUnit이 호출되어야 한다. Step 7 테스트 시에는 수동으로 unitGrid에 Insert하는 임시 코드를 사용한다.

**체크포인트 ✓ Layer 2 완료**: 몬스터가 스폰되고 플레이어를 탐지·추적한다.

---

### Step 8 — 자동 전투 시스템

**관련 설계**: [combat.md](design/combat.md), [entity_common.md](design/entity_common.md) (Notifier 인터페이스)

**구현 파일**

| 파일 | 타입 | 핵심 내용 |
|------|------|-----------|
| `IOnHitListener.cs` | interface | OnHit(attacker, target, damage) |
| `IOnUnitDeathListener.cs` | interface | OnUnitDeath(deadUnit, killer) |
| `IOnTamingListener.cs` | interface | OnTamingSuccess(monster, newMember) |
| `DamageProcessor.cs` | static class | ProcessDamage() — TakeDamage + Notifier 발행 |
| `CombatSystem.cs` | pure C# | RegisterUnit, UnitGrid, RebuildGrid, ProcessCombat |

이후 **GameController에 CombatSystem 추가** 및 이벤트 등록 완성:

```csharp
combatSystem = new CombatSystem(Notifier);
combatSystem.RegisterUnit(Player);
Squad.OnMemberAdded   += combatSystem.RegisterUnit;
Squad.OnMemberRemoved += combatSystem.UnregisterUnit;
entitySpawner.OnMonsterSpawned   += combatSystem.RegisterUnit;
entitySpawner.OnMonsterDespawned += combatSystem.UnregisterUnit;
```

```csharp
// GameController.Update()에 추가
combatSystem.Update();
// 각 유닛 쿨다운 Tick
Player.Combat.Tick(dt);
foreach (var m in Squad.Members) m.Combat.Tick(dt);
foreach (var m in entitySpawner.ActiveMonsters) m.Combat.Tick(dt);
```

**의존성**: Step 5, 6, 7

**구현 순서**

1. Notifier 인터페이스 3개 정의
2. `DamageProcessor.ProcessDamage()` (TakeDamage 호출 + Notify)
3. `CombatSystem`: RegisterUnit/UnregisterUnit, RebuildGrid(SpatialGrid clear + Insert), ProcessCombat(적팀 Query → CanAttack → ProcessDamage)
4. GameController에 CombatSystem 주입 및 유닛 등록 이벤트 연결
5. GameController.Update()에 combatSystem.Update() 추가

**테스트**

- 부대원 1명 + 몬스터 1마리 근접 배치 → 서로 자동 공격 확인
- 체력이 0이 되면 OnDeath 발행 → 처치 후 상대가 사라지는지 확인
- Player도 CombatSystem에 등록되어 몬스터의 공격 대상이 되는지 확인

**리스크**: 중간 — ProcessCombat에서 자기 팀끼리 공격하지 않도록 Team 비교 로직 필수. SpatialGrid RebuildGrid 호출 빈도가 성능에 영향을 줄 수 있으므로 매 프레임 리빌드가 부담스러우면 주기를 조절할 것.

**체크포인트 ✓ Layer 3 완료**: 아군과 적이 자동으로 교전하고 체력이 감소한다.

---

### Step 9 — 테이밍

**관련 설계**: [taming.md](design/taming.md)

**구현 파일**

| 파일 | 타입 | 핵심 내용 |
|------|------|-----------|
| `TamingSystem.cs` | pure C# | IOnUnitDeathListener, tamingChance 판정, SpawnSquadMember + AddMember |

이후 **GameController에 TamingSystem 추가**:

```csharp
tamingSystem = new TamingSystem(Squad, entitySpawner, Notifier);
```

**의존성**: Step 7, 8 (EntitySpawner, Squad, CombatSystem, Notifier 필요)

**구현 순서**

1. `TamingSystem` (Notifier.Subscribe, OnUnitDeath 구현)
2. GameController 생성자에 TamingSystem 추가

**테스트**

- MonsterData.tamingChance = 1.0f 로 설정 후 몬스터 처치
- 처치 직후 같은 위치에 SquadMemberView 스폰, Squad에 추가되는지 확인
- CombatSystem에 자동 등록(OnMemberAdded → RegisterUnit) 확인
- tamingChance = 0.0f 로 설정 시 테이밍이 발생하지 않는지 확인

**리스크**: 낮음

**체크포인트 ✓**: 코어 루프 완성 — 탐험 → 전투 → 테이밍 → 부대 확장이 동작한다.

---

### Step 10 — 전투 연출

**관련 설계**: [vfx.md](design/vfx.md)

**구현 파일**

| 파일 | 타입 | 핵심 내용 |
|------|------|-----------|
| `HitStop.cs` | pure C# | IOnHitListener, isActive 플래그, WaitForSecondsRealtime |
| `CameraShake.cs` | pure C# | IOnHitListener, Coroutine으로 transform 흔들기 |
| `HitEffectPlayer.cs` | pure C# | IOnHitListener, Pool.Spawn + Sound.PlaySFX |

이후 **GameController에 VFX 추가**:

```csharp
hitStop         = new HitStop(0.05f, Notifier);
cameraShake     = new CameraShake(cameraTransform, 0.2f, 0.1f, Notifier);
hitEffectPlayer = new HitEffectPlayer(hitEffectPrefab, "hit_sfx", Notifier);
```

**의존성**: Step 8 (IOnHitListener, Notifier 필요), Facade.Coroutine, Facade.Pool, Facade.Sound

**구현 순서**

1. `HitStop` (isActive 방어 로직 포함)
2. `CameraShake` (Coroutine에서 Random.insideUnitCircle × intensity 오프셋 적용)
3. `HitEffectPlayer` (Pool에서 이펙트 오브젝트 스폰)
4. GameController에 VFX 시스템 주입

**테스트**

- 전투 중 타격 시 시간이 짧게 멈추는지 확인 (Time.timeScale = 0)
- 다수 유닛이 동시 공격 시 HitStop이 한 번만 발동하는지 확인 (isActive 방어)
- 카메라가 흔들리는지 확인
- 타격 위치에 이펙트 오브젝트가 생성되는지 확인

**리스크**: 중간 — HitStop과 CameraShake가 동시에 실행될 때 timeScale = 0이면 WaitForSeconds가 동작하지 않으므로 반드시 `WaitForSecondsRealtime` 사용 확인

**체크포인트 ✓ Layer 4 완료**: 전투에 타격감(히트스톱, 카메라 셰이크, 이펙트)이 추가되었다.

---

### Step 11 — 게임 스냅샷

**관련 설계**: [game_controller.md](design/game_controller.md)

**구현 파일**

| 파일 | 타입 | 핵심 내용 |
|------|------|-----------|
| `GameSnapshot.cs` | pure C# | GameSnapshot, SquadMemberSnapshot, MonsterSnapshot |

이후 **GameController에 CreateSnapshot / RestoreFromSnapshot 구현**:

```csharp
public GameSnapshot CreateSnapshot() { ... }
public void RestoreFromSnapshot(GameSnapshot snapshot) { ... }
```

**의존성**: Step 7, 8, 9, 12 (FogOfWar는 Step 12에서 완성. 이 단계에서는 FogGrid를 null 허용 또는 stub으로 처리)

**구현 순서**

1. `SquadMemberSnapshot`, `MonsterSnapshot`, `GameSnapshot` 클래스 정의
2. `GameController.CreateSnapshot()` 구현 (playerPos 먼저 추출 후 전달)
3. `GameController.RestoreFromSnapshot()` 구현 (DespawnAll → SetPosition → SpawnFromSnapshot)

**테스트**

- 전투 중 CreateSnapshot() → 몬스터/부대원 수 변경 → RestoreFromSnapshot() 호출
- 복원 후 플레이어 위치, 부대원 상대 위치, 몬스터 위치가 스냅샷과 일치하는지 확인
- CombatSystem 등록이 RestoreFromSnapshot 후에도 정상 동작하는지 확인

**리스크**: 낮음 — FogGrid 의존이 있으므로 Step 12 완료 전에는 fogGrid 파라미터를 null로 허용하는 오버로드를 임시로 사용

---

### Step 12 — 전장의 안개

**관련 설계**: [fog_of_war.md](design/fog_of_war.md)

**구현 파일**

| 파일 | 타입 | 핵심 내용 |
|------|------|-----------|
| `FogOfWar.cs` | MonoBehaviour | FogState[,], Texture2D, RevealAround, CopyFogGrid, RestoreFogGrid |

이후 **GameController에 FogOfWar 연결**:

```csharp
// GameController.Update()에 추가
fogOfWar.RevealAround(Player.Transform.position);
```

**의존성**: Step 2, 3 (Player 위치, 맵 크기 필요)

**구현 순서**

1. `FogState` enum
2. `FogOfWar`: fogGrid 초기화 (전체 Hidden), RevealAround (viewRadius 내 셀 → Visible, 나머지 Visible → Explored), UpdateTexture (FogState → Texture2D 픽셀 색상)
3. GameController 생성자에 FogOfWar 파라미터 추가 및 Update 연결
4. Step 11 GameSnapshot에 fogGrid 통합

**테스트**

- 플레이어가 이동하면 이동 경로 주변의 안개가 걷히는지 확인
- 이동을 멈추면 현재 시야만 Visible, 이전 탐색 경로는 Explored로 표시되는지 확인
- 맵 경계에서 IndexOutOfRange가 발생하지 않는지 확인

**리스크**: 중간 — Texture2D.Apply() 호출 빈도가 높으면 GC 부담이 발생할 수 있다. RevealAround 호출 시마다 Apply하지 않고 변경된 픽셀이 있을 때만 Apply하는 Dirty 플래그 활용.

**체크포인트 ✓**: 탐험한 지역과 미탐험 지역이 시각적으로 구분된다.

---

### Step 13 — 미니맵

**관련 설계**: [minimap.md](design/minimap.md)

**구현 파일**

| 파일 | 타입 | 핵심 내용 |
|------|------|-----------|
| `Minimap.cs` | MonoBehaviour | Refresh() — 아이콘 위치 업데이트, FogOfWar 연동 |

이후 **GameController에 Minimap 연결**:

```csharp
// GameController.Update()에 추가
minimap.Refresh(Player.Transform, Squad.Members, entitySpawner.ActiveMonsters);
```

**의존성**: Step 12 (FogOfWar), 모든 Entity

**구현 순서**

1. `Minimap`: Refresh() 구현 (월드 좌표 → 미니맵 UV 변환, 아이콘 RectTransform 위치 설정)
2. FogOfWar와 연동 (Hidden 영역의 아이콘 숨기기)
3. GameController에 Minimap 주입 및 Update 연결

**테스트**

- 플레이어 아이콘이 화면 이동에 따라 미니맵에서 정확히 이동하는지 확인
- 적 아이콘이 탐색된 영역에만 표시되는지 확인
- 부대원 아이콘이 실제 위치와 일치하는지 확인

**리스크**: 낮음 — 월드→미니맵 좌표 변환 공식이 맵 크기와 미니맵 UI 크기에 맞게 정규화되어야 함

**체크포인트 ✓ Phase 2 완료**: 탐험·전투·테이밍·부대 확장 코어 루프가 전체 동작한다.

---

## 기술 리스크 요약

| 리스크 | 해당 단계 | 대응 방안 |
|--------|-----------|-----------|
| FlockBehavior 가중치 튜닝 | Step 5 | 각 행동 벡터를 개별 구현 후 Inspector에서 실시간 조정 가능하도록 SerializeField 노출 |
| MapGenerator 좌표계 불일치 | Step 3 | Tilemap.WorldToCell / CellToWorld 공식 검증용 에디터 Gizmo 먼저 작성 |
| MonsterAI 탐지 — Step 7 임시 처리 | Step 7 | CombatSystem 완성 전 테스트용 임시 Insert 코드 사용 후 Step 8에서 제거 |
| HitStop + Coroutine + timeScale | Step 10 | WaitForSecondsRealtime 필수. HitStop 이전 timeScale이 1이 아닌 경우 복원값 저장 필요 |
| Texture2D.Apply() GC | Step 12 | Dirty 플래그 패턴으로 변경된 프레임에만 Apply |
| CombatSystem 매 프레임 RebuildGrid | Step 8 | 유닛 수가 많아지면 주기를 2~3 프레임에 1회로 조정 |

---

## 테스트 전략

### EditMode 단위 테스트 대상

Unity 의존 없이 테스트 가능한 pure C# 클래스를 우선한다.

| 테스트 대상 | 검증 내용 |
|-------------|-----------|
| `UnitCombat.Tick()` | deltaTime 누적 후 CanAttack 전환 |
| `SpatialGrid.Query()` | 삽입한 아이템이 반경 내에서 조회, 반경 밖에서 미조회 |
| `FlockBehavior.CalculateDirection()` | Separation 벡터가 이웃 방향의 반대인지 |
| `TamingSystem.OnUnitDeath()` | tamingChance = 1.0 시 항상 성공, 0.0 시 항상 실패 |
| `GameSnapshot` 직렬화 | CreateSnapshot → RestoreFromSnapshot 후 포지션 일치 |

### PlayMode 씬 테스트 (단계별 체크포인트)

| 체크포인트 | 확인 항목 |
|-----------|-----------|
| Step 4 완료 | 플레이어 이동, 카메라 추적, Phase 게이팅 |
| Step 5 완료 | 부대원 군집 이동, 대형 유지, 장애물 회피 |
| Step 7 완료 | 몬스터 탐지, 추적, 공격 상태 전이 |
| Step 8 완료 | 아군-적 자동 교전, 체력 감소, 사망 처리 |
| Step 9 완료 | 코어 루프 (탐험→전투→테이밍→부대 확장) |
| Step 10 완료 | 히트스톱, 카메라 셰이크, 타격 이펙트 |
| Step 13 완료 | 전장의 안개, 미니맵 아이콘 |
