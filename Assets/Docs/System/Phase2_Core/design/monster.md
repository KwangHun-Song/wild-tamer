# 2.4 몬스터

> 상위 문서: [Phase 2 설계](../design.md)

야생 몬스터 개체(Monster), AI 상태 기계(MonsterAI), 기획 데이터(MonsterData), 스폰 관리(EntitySpawner)를 포함한다.

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

MonsterAI를 내부에 소유하며 AI 업데이트를 위임한다. TamingSystem 등 외부에서 View 연출이 필요할 때는 공개 메서드(`PlayTamingEffect`)를 경유한다.

```csharp
public class Monster : Character
{
    public override UnitTeam Team => UnitTeam.Enemy;
    public MonsterData Data { get; }

    private readonly MonsterAI ai;
    private readonly MonsterView monsterView;

    public event Action<Vector2> OnMoveRequested;

    public Monster(MonsterView view, MonsterData data)
        : base(view, CreateCombat(data))
    {
        Data = data;
        monsterView = view;

        view.Health.Initialize(data.maxHp);
        view.Movement.MoveSpeed = data.moveSpeed;
        view.Health.OnDamaged += _ => monsterView.PlayHitEffect();
        view.Health.OnDeath   += monsterView.PlayDeathEffect;
        view.Subscribe(this);

        ai = new MonsterAI(this);
        ai.SetUp();
    }

    public void Update() => ai.Update();

    public void Move(Vector2 direction) => OnMoveRequested?.Invoke(direction);

    /// <summary>외부(TamingSystem 등)에서 View 연출을 트리거할 때 사용한다. View 직접 접근 금지.</summary>
    public void PlayTamingEffect() => monsterView.PlayTamingEffect();

    private static UnitCombat CreateCombat(MonsterData d)
        => new(d.attackDamage, d.attackRange, d.detectionRange, d.attackCooldown);
}
```

---

## MonsterView (MonoBehaviour) : CharacterView

Monster 이벤트를 구독하여 Movement를 구동하고, 피격/사망/테이밍 연출을 처리한다.

```csharp
public class MonsterView : CharacterView
{
    public void Subscribe(Monster monster)
    {
        monster.OnMoveRequested += direction => Movement.Move(direction);
    }

    public void PlayHitEffect() { ... }
    public void PlayDeathEffect() { ... }
    public void PlayTamingEffect() { ... }
}
```

---

## MonsterAI (pure C#)

FSM(`StateMachine<Monster, MonsterTrigger>`) 기반 AI. Idle → Chase → Attack 상태를 전이한다.

각 State는 탐지 로직이 필요하다. `MonsterIdleState`와 `MonsterChaseState`는 Owner의 `Combat.DetectionRange`를 기준으로 **CombatSystem의 SpatialGrid**를 조회하여 적을 탐지한다. SpatialGrid 참조는 MonsterAI 생성 시 주입받는다.

```csharp
public enum MonsterTrigger
{
    DetectEnemy, LoseEnemy, InAttackRange, OutOfAttackRange
}

public class MonsterAI : StateMachine<Monster, MonsterTrigger>
{
    public SpatialGrid<IUnit> UnitGrid { get; }    // 탐지에 사용할 공유 SpatialGrid

    private readonly MonsterIdleState idle     = new();
    private readonly MonsterChaseState chase   = new();
    private readonly MonsterAttackState attack = new();

    protected override State<Monster, MonsterTrigger> InitialState => idle;
    protected override State<Monster, MonsterTrigger>[] States
        => new State<Monster, MonsterTrigger>[] { idle, chase, attack };
    protected override StateTransition<Monster, MonsterTrigger>[] Transitions => new[]
    {
        StateTransition<Monster, MonsterTrigger>.Generate(idle,   chase,  MonsterTrigger.DetectEnemy),
        StateTransition<Monster, MonsterTrigger>.Generate(chase,  idle,   MonsterTrigger.LoseEnemy),
        StateTransition<Monster, MonsterTrigger>.Generate(chase,  attack, MonsterTrigger.InAttackRange),
        StateTransition<Monster, MonsterTrigger>.Generate(attack, chase,  MonsterTrigger.OutOfAttackRange),
    };

    public MonsterAI(Monster owner, SpatialGrid<IUnit> unitGrid) : base(owner)
    {
        UnitGrid = unitGrid;
    }
}
```

각 State는 `StateMachine.Owner`(Monster)와 `(StateMachine as MonsterAI).UnitGrid`를 통해 탐지를 수행한다.

```csharp
// MonsterIdleState 예시
public class MonsterIdleState : State<Monster, MonsterTrigger>
{
    public override void OnEnter() { /* 대기 애니메이션 */ }

    // TryTransition에서 조건 체크 — 탐지 범위 내 적 존재 시 DetectEnemy 트리거
    // StateMachine.TryTransition()이 매 프레임 호출하므로 여기서 조건 평가
}
```

### 상태 전이 요약

| 출발 | 도착 | 트리거 | 조건 |
|------|------|--------|------|
| Idle | Chase | DetectEnemy | DetectionRange 내 Player 팀 유닛 존재 |
| Chase | Idle | LoseEnemy | DetectionRange 밖으로 모든 적 이탈 |
| Chase | Attack | InAttackRange | AttackRange 내 적 존재 |
| Attack | Chase | OutOfAttackRange | AttackRange 밖으로 적 이탈 |

---

## EntitySpawner (pure C#)

Monster와 SquadMember 양쪽 스폰/디스폰을 담당한다. `OnMonsterSpawned` / `OnMonsterDespawned` 이벤트로 GameController가 CombatSystem에 자동 등록할 수 있도록 한다.

```csharp
public class EntitySpawner
{
    private readonly List<Monster> activeMonsters = new();

    public IReadOnlyList<Monster> ActiveMonsters => activeMonsters;

    public event Action<Monster> OnMonsterSpawned;
    public event Action<Monster> OnMonsterDespawned;

    public Monster SpawnMonster(MonsterData data, Vector2 position)
    {
        var go      = Facade.Pool.Spawn(data.prefab, position);
        var view    = go.GetComponent<MonsterView>();
        var monster = new Monster(view, data);
        activeMonsters.Add(monster);
        OnMonsterSpawned?.Invoke(monster);
        return monster;
    }

    public SquadMember SpawnSquadMember(MonsterData data, Vector2 position)
    {
        var go   = Facade.Pool.Spawn(data.squadPrefab, position);
        var view = go.GetComponent<SquadMemberView>();
        return new SquadMember(view, data);
    }

    public void DespawnMonster(Monster monster)
    {
        OnMonsterDespawned?.Invoke(monster);
        activeMonsters.Remove(monster);
        Facade.Pool.Despawn(monster.Transform.gameObject);
    }

    /// <summary>GameController.Update()에서 호출. AI 업데이트 및 스폰 주기 처리.</summary>
    public void Update(float deltaTime) { ... }
}
```
