# 2.4 몬스터

> 상위 문서: [Phase 2 설계](../design.md)

야생 몬스터 개체(Monster), FSM 기반 AI(`MonsterStandaloneFSM` / `MonsterLeaderFSM`), 기획 데이터(MonsterData), 스폰 관리(EntitySpawner)를 포함한다.

---

## MonsterData (ScriptableObject)

Monster와 SquadMember가 공통으로 참조하는 기획 데이터. 테이밍 후 SquadMemberView 프리팹도 여기에 보유한다.

```csharp
[CreateAssetMenu(menuName = "Data/MonsterData")]
public class MonsterData : ScriptableObject
{
    public string id;
    public string displayName;
    public MonsterGrade grade;
    public int maxHp;
    public int attackDamage;
    public float attackRange;
    public float attackCooldown;
    public float detectionRange;
    public float moveSpeed;
    public float tamingChance;
    public GameObject prefab;           // MonsterView 프리팹
    public GameObject squadPrefab;      // SquadMemberView 프리팹 (테이밍 후 사용)
    public BossPattern[] bossPatterns;
}

public enum MonsterGrade { Normal, Boss }
```

---

## Monster (pure C#) : Character

`MonsterRole`에 따라 적절한 FSM을 생성하여 내부에 소유한다.
Follower 역할(MonsterSquad 소속)은 FSM 없이 `Move()` 직접 호출로 이동·애님을 처리한다.

```csharp
public enum MonsterRole { Standalone, Leader, Follower }

public class Monster : Character
{
    public override UnitTeam Team => UnitTeam.Enemy;
    public MonsterData Data { get; }

    private StateMachine<Monster, MonsterTrigger> fsm;

    public Monster(MonsterView view, MonsterData data, SpatialGrid<IUnit> unitGrid, MonsterRole role, ObstacleGrid obstacleGrid = null)
        : base(view, CreateCombat(data))
    {
        // ...
        fsm = role switch
        {
            MonsterRole.Leader   => new MonsterLeaderFSM(this, unitGrid, obstacleGrid),
            MonsterRole.Follower => null,
            _                    => new MonsterStandaloneFSM(this, unitGrid),
        };
        fsm?.SetUp();
    }

    public void Update() => fsm?.Update();

    /// <summary>Follower 전용. FSM 없이 이동·애님을 직접 처리한다.</summary>
    public void Move(Vector2 direction) { ... }

    public void PlayTamingEffect() => monsterView.PlayTamingEffect();
}
```

---

## FSM 구조

### MonsterStandaloneFSM

단독 몬스터용. `SpatialGrid`를 통해 적을 탐지한다.

| 상태 | 역할 |
|---|---|
| `MonsterIdleState` | idle 애님, 감지 범위 내 적 탐지 시 DetectEnemy |
| `MonsterChaseState` | move 애님, 가장 가까운 적 추적 이동 |
| `MonsterAttackState` | attack 애님, 적을 향해 이동하며 공격 |
| `MonsterDeadState` | 이동 정지, dead 애님 |

### MonsterLeaderFSM

MonsterSquad 리더용. `ObstacleGrid` 기반 장애물 우회를 지원한다.

| 상태 | 역할 |
|---|---|
| `MonsterWanderState` | 랜덤 배회, 감지 범위 내 적 탐지 시 DetectEnemy |
| `MonsterChaseState` | 장애물 우회 추적 이동 |
| `MonsterAttackState` | 적을 향해 이동하며 공격 |
| `MonsterDeadState` | 이동 정지, dead 애님 |

**공통 트리거 (`MonsterTrigger`):**

```csharp
public enum MonsterTrigger { DetectEnemy, LoseEnemy, InAttackRange, OutOfAttackRange, Die }
```

### Attack 상태 이동

Attack 상태에서도 가장 가까운 적을 향해 이동한다. 정지하지 않고 근접 유지를 시도한다.

---

## MonsterView (MonoBehaviour) : CharacterView

FSM States가 직접 호출하는 애님 API를 CharacterView로부터 상속받는다.
피격·사망·테이밍 VFX 연출 메서드를 추가로 제공한다.

```csharp
public class MonsterView : CharacterView
{
    public void PlayHitEffect() { ... }
    public void PlayDeathEffect() { ... }
    public void PlayTamingEffect() { ... }
}
```

---

## EntitySpawner (pure C#)

Monster와 SquadMember 양쪽 스폰/디스폰을 담당한다. `OnMonsterSpawned` / `OnMonsterDespawned` 이벤트로 GameController가 CombatSystem에 자동 등록할 수 있도록 한다.

```csharp
public class EntitySpawner
{
    public event Action<Monster> OnMonsterSpawned;
    public event Action<Monster> OnMonsterDespawned;

    public Monster SpawnMonster(MonsterData data, Vector2 position) { ... }
    public SquadMember SpawnSquadMember(MonsterData data, Vector2 position) { ... }
    public void DespawnMonster(Monster monster) { ... }

    /// <summary>GameController.Update()에서 호출. 몬스터 FSM 업데이트 처리.</summary>
    public void Update(float deltaTime) { ... }
}
```
