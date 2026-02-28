# Phase 2: 코어 시스템 - 설계

## 설계 방침

Phase 1의 Facade(정적 서비스), Notifier(이벤트 버스), FSM(상태 기계), ObjectPool을 적극 활용한다. 9개 시스템이 상호작용하므로 **직접 참조를 최소화**하고 Notifier 기반 이벤트로 느슨하게 결합한다.

**GameController**가 모든 pure C# 시스템을 소유하고 Update를 위임하는 중앙 오케스트레이터 역할을 한다. 입력은 GameController를 통해 Player에 전달되며, 게임 페이즈(플레이/일시정지/업그레이드)에 따라 GameController가 입력과 Update를 게이팅한다.

### 구현 방식

각 시스템을 한 번에 전부 구현하지 않고 **단계별로 구현 → 리뷰 → 반영**을 반복한다. 각 시스템은 독립적으로 컨셉·설계·리뷰·구현을 거치며, 순서는 의존성 레이어를 기준으로 한다 (concept.md의 구현 순서 참고).

### MonoBehaviour 사용 기준

Unity 생명주기나 씬 직렬화가 직접 필요한 클래스만 MonoBehaviour를 상속한다.

| 유형 | MonoBehaviour | 이유 |
|------|:---:|-------|
| CharacterView (PlayerView, MonsterView, SquadMemberView) | O | 씬에 배치, Transform/컴포넌트 직렬화 |
| Character (Player, Monster, SquadMember) | X | 순수 게임 로직, View를 통해 씬과 간접 연결 |
| GameController, Squad, CombatSystem, TamingSystem, EntitySpawner | X | 로직만 담당, MonoBehaviour 불필요 |
| HitStop, CameraShake, HitEffectPlayer | X | Facade.Coroutine으로 타이밍 처리 |
| GameLoop | O | Unity Update를 GameController.Update()에 브리지 |
| MapGenerator | O | Tilemap 직렬화 필요 |
| FogOfWar, Minimap | O | 렌더링·UI 컴포넌트 직렬화 필요 |

---

## 폴더 구조

```
Scripts/
├── 01.Scene/
│   └── PlayScene/
│       ├── GameLoop.cs          # GameController를 소유하는 MonoBehaviour 브리지
│       └── States/
│           └── InPlayState.cs   # GameLoop 시작·종료 관리
└── 04.Game/
    ├── 01.Entity/
    │   ├── Common/
    │   │   ├── IUnit.cs
    │   │   ├── UnitCombat.cs
    │   │   ├── Character.cs         # Presenter 추상 베이스 (pure C#)
    │   │   ├── CharacterView.cs     # View 추상 베이스 (MonoBehaviour)
    │   │   ├── UnitHealth.cs        # 체력 컴포넌트 (CharacterView 소유)
    │   │   └── UnitMovement.cs      # 이동 컴포넌트 (CharacterView 소유)
    │   ├── Player/
    │   │   ├── Player.cs            # Presenter (pure C#)
    │   │   ├── PlayerView.cs        # View (MonoBehaviour)
    │   │   └── PlayerInput.cs       # 입력 (MonoBehaviour)
    │   ├── Squad/
    │   │   ├── SquadMember.cs       # Presenter (pure C#), MonsterData 보유
    │   │   ├── SquadMemberView.cs   # View (MonoBehaviour)
    │   │   └── FlockBehavior.cs     # 군집 계산 (pure C#)
    │   └── Monster/
    │       ├── Monster.cs           # Presenter (pure C#), MonsterData 보유
    │       ├── MonsterView.cs       # View (MonoBehaviour)
    │       ├── MonsterAI.cs         # FSM (pure C#)
    │       └── States/
    │           ├── MonsterIdleState.cs
    │           ├── MonsterChaseState.cs
    │           └── MonsterAttackState.cs
    ├── 02.System/
    │   ├── Game/
    │   │   ├── GameController.cs    # 중앙 오케스트레이터 (pure C#)
    │   │   ├── GamePhase.cs         # 게임 상태 열거형
    │   │   └── GameSnapshot.cs      # 직렬화 가능한 게임 상태 스냅샷
    │   ├── Squad/
    │   │   └── Squad.cs             # 부대 관리 (pure C#)
    │   ├── Combat/
    │   │   ├── CombatSystem.cs      # 자동 교전 (pure C#)
    │   │   ├── DamageProcessor.cs
    │   │   └── TamingSystem.cs      # 테이밍 판정 (pure C#)
    │   ├── Entity/
    │   │   └── EntitySpawner.cs     # Monster/SquadMember 스폰 관리 (pure C#)
    │   ├── Map/
    │   │   ├── MapGenerator.cs      # 타일맵 생성 (MonoBehaviour)
    │   │   ├── ObstacleGrid.cs      # 장애물 그리드 (pure C#)
    │   │   ├── FogOfWar.cs          # 시야 (MonoBehaviour)
    │   │   └── Minimap.cs           # 미니맵 UI (MonoBehaviour)
    │   ├── VFX/
    │   │   ├── HitStop.cs           # 역경직 (pure C#)
    │   │   ├── CameraShake.cs       # 카메라 흔들림 (pure C#)
    │   │   └── HitEffectPlayer.cs   # 이펙트/사운드 (pure C#)
    │   └── Spatial/
    │       └── SpatialGrid.cs       # 공간 분할 (pure C#)
    └── 03.Data/
        ├── MonsterData.cs           # 몬스터/부대원 공통 기획 데이터
        └── BossPattern.cs
```

---

## 하위 설계 문서

각 시스템의 상세 클래스 설계는 아래 문서에 분리되어 있다.

| 문서 | 주제 | 포함 클래스 |
|------|------|-------------|
| [공통 개체 구조](design/entity_common.md) | IUnit, Character/View 계층, UnitCombat, Notifier 인터페이스 | IUnit, Character, CharacterView, UnitHealth, UnitMovement, UnitCombat, IOnHitListener 등 |
| [GameController](design/game_controller.md) | 중앙 오케스트레이터, 게임 루프, 스냅샷 | GameController, GameLoop, GamePhase, GameSnapshot |
| [2.1 플레이어](design/player.md) | 입력 및 이동 | Player, PlayerView, PlayerInput, QuarterViewCamera |
| [2.2 군집 및 부대](design/squad.md) | 부대 관리, 군집 이동, 공간 분할 | Squad, SquadMember, SquadMemberView, FlockBehavior, SpatialGrid |
| [2.3 월드맵](design/world_map.md) | 맵 생성 및 장애물 | MapGenerator, ObstacleGrid |
| [2.4 몬스터](design/monster.md) | 몬스터 개체, AI, 기획 데이터, 스폰 | Monster, MonsterView, MonsterAI, MonsterData, EntitySpawner |
| [2.5 자동 전투](design/combat.md) | 교전 로직, 데미지 처리 | CombatSystem, DamageProcessor |
| [2.6 테이밍](design/taming.md) | 테이밍 판정 및 부대 합류 | TamingSystem |
| [2.7 전투 연출](design/vfx.md) | 역경직, 카메라 셰이크, 이펙트 | HitStop, CameraShake, HitEffectPlayer |
| [2.8 전장의 안개](design/fog_of_war.md) | 시야 및 안개 렌더링 | FogOfWar, FogState |
| [2.9 미니맵](design/minimap.md) | 미니맵 UI | Minimap |

---

## 디자인 패턴

| 패턴 | 적용 위치 | 이유 |
|------|-----------|------|
| **MVP** | Character/CharacterView 계층 | View(MonoBehaviour)와 게임 로직(pure C#) 분리 |
| **Mediator** | GameController | 시스템 간 직접 참조 없이 중앙 조율 |
| **Memento** | GameSnapshot | 게임 상태 스냅샷·복원 |
| **Observer (Notifier)** | 전투→연출, 전투→테이밍 | 시스템 간 느슨한 결합 |
| **FSM** | MonsterAI | 상태별 행동 분리 |
| **Object Pool** | EntitySpawner | 다수 개체 생성/파괴 최적화 |
| **Spatial Partitioning** | SpatialGrid | 탐색 O(n²) → O(n) 최적화 |

---

## 데이터 흐름

```
[PlayerInput (MB)]
    │ MoveDirection
    ▼
[GameController (C#)] ── Phase == Play? ──→ 이하 Update 진행
    │
    ├─ player.Move(direction) + Combat.Tick(dt)
    │      └──→ OnMoveRequested ──→ [PlayerView] ──→ UnitMovement.Move()
    │
    ├─ squad.Update(leader, obstacleGrid, dt)
    │      └──→ FlockBehavior → member.Move()
    │              └──→ OnMoveRequested ──→ [SquadMemberView]
    │
    ├─ entitySpawner.Update(dt)
    │      └──→ monster.Update() ──→ MonsterAI.Update()
    │              └──→ SpatialGrid 조회(탐지) → 트리거 → 상태 전이
    │              └──→ monster.Move() ──→ [MonsterView]
    │
    ├─ combatSystem.Update()
    │      └──→ DamageProcessor.ProcessDamage()
    │              ├──→ Notifier<IOnHitListener>
    │              │       ├──→ [HitStop] (isActive 중복 방지)
    │              │       ├──→ [CameraShake]
    │              │       └──→ [HitEffectPlayer]
    │              └──→ Notifier<IOnUnitDeathListener>
    │                      └──→ [TamingSystem]
    │                              ├──→ EntitySpawner.SpawnSquadMember()
    │                              ├──→ Squad.AddMember() ──→ OnMemberAdded ──→ CombatSystem.RegisterUnit()
    │                              └──→ monster.PlayTamingEffect()
    │
    ├─ fogOfWar.RevealAround(player.position)
    └─ minimap.Refresh(...)

CombatSystem 유닛 등록 흐름:
  Player 생성 시:                   combatSystem.RegisterUnit(Player)
  Squad.OnMemberAdded:              combatSystem.RegisterUnit(SquadMember)
  Squad.OnMemberRemoved:            combatSystem.UnregisterUnit(SquadMember)
  EntitySpawner.OnMonsterSpawned:   combatSystem.RegisterUnit(Monster)
  EntitySpawner.OnMonsterDespawned: combatSystem.UnregisterUnit(Monster)
```

---

## Phase 1 모듈 활용

| Phase 1 서비스 | Phase 2 사용처 |
|----------------|---------------|
| `Facade.Pool` | EntitySpawner(스폰/디스폰), HitEffectPlayer(이펙트 생성/회수) |
| `Facade.Sound` | HitEffectPlayer(타격 사운드) |
| `Facade.Coroutine` | HitStop(역경직 타이밍), CameraShake(흔들림 타이밍) |
| `Facade.DB` | MonsterData 조회 (EntitySpawner 스폰 시) |
| `Notifier` | 전투↔연출, 전투↔테이밍 이벤트 통신 |
| `StateMachine` | MonsterAI (Idle/Chase/Attack) |
| `Facade.Data` | GameSnapshot 직렬화·저장 (Save/Load) |

---

## 의존성 구조

```
GameController (C#)
  ├── 소유: Player, Squad, CombatSystem, TamingSystem, EntitySpawner, VFX 시스템
  ├── 참조(씬): PlayerInput, FogOfWar, Minimap, ObstacleGrid
  └── 생성: GameSnapshot (Memento)

01.Entity/Common (IUnit, Character, CharacterView, UnitHealth, UnitMovement, UnitCombat)
    ←── 01.Entity/Player (Player, PlayerView)
    ←── 01.Entity/Squad  (SquadMember, SquadMemberView)
    ←── 01.Entity/Monster (Monster, MonsterView)

03.Data/MonsterData ──→ 01.Entity/Monster (Monster 생성)
                    ──→ 01.Entity/Squad   (SquadMember 생성, 테이밍 후)

01.Entity/Monster ──→ Modules/FiniteStateMachine (MonsterAI)
MonsterAI ←── CombatSystem.UnitGrid (탐지용 SpatialGrid 공유)

02.System/Combat (CombatSystem, TamingSystem) ──→ 01.Entity/Common (IUnit)
02.System/Combat ──Notifier──→ 02.System/VFX (HitStop, CameraShake, HitEffectPlayer)
02.System/Entity (EntitySpawner) ──→ 01.Entity/Monster + 01.Entity/Squad
Squad.OnMemberAdded/Removed ──→ CombatSystem.Register/Unregister
EntitySpawner.OnMonsterSpawned/Despawned ──→ CombatSystem.Register/Unregister

모두 → Modules/Base (Facade, Notifier)
```
