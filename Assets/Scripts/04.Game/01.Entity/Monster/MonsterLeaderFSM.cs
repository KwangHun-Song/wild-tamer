using FiniteStateMachine;
using UnityEngine;

/// <summary>
/// 몬스터 스쿼드 리더 FSM. Wander ↔ Chase → Attack 상태 전이를 관리한다.
/// - DetectEnemy / LoseEnemy / InAttackRange / OutOfAttackRange: 트리거 + 조건 둘 다 처리
/// - Die: Health.OnDeath 이벤트 → ExecuteCommand (트리거 전용)
/// </summary>
public class MonsterLeaderFSM : StateMachine<Monster, MonsterTrigger>
{
    public SpatialGrid<IUnit> UnitGrid { get; }
    public ObstacleGrid ObstacleGrid { get; }

    private readonly MonsterCreateState create = new();
    private readonly MonsterWanderState wander = new();
    private readonly MonsterChaseState chase = new();
    private readonly MonsterAttackState attack = new();
    private readonly MonsterDeadState dead = new();
    private readonly MonsterDestroyState destroy = new();

    private readonly State<Monster, MonsterTrigger> initialState;

    protected override State<Monster, MonsterTrigger> InitialState => initialState;

    protected override State<Monster, MonsterTrigger>[] States
        => new State<Monster, MonsterTrigger>[] { create, wander, chase, attack, dead, destroy };

    protected override StateTransition<Monster, MonsterTrigger>[] Transitions => new[]
    {
        StateTransition<Monster, MonsterTrigger>.Generate(create, wander, MonsterTrigger.Created),
        StateTransition<Monster, MonsterTrigger>.Generate(wander, chase,  MonsterTrigger.DetectEnemy,      EnemyInDetectionRange),
        StateTransition<Monster, MonsterTrigger>.Generate(chase,  attack, MonsterTrigger.InAttackRange,    EnemyInAttackRange),
        StateTransition<Monster, MonsterTrigger>.Generate(chase,  wander, MonsterTrigger.LoseEnemy,        s => !EnemyInDetectionRange(s)),
        StateTransition<Monster, MonsterTrigger>.Generate(attack, chase,  MonsterTrigger.OutOfAttackRange, s => !EnemyInAttackRange(s)),
        StateTransition<Monster, MonsterTrigger>.Generate(wander, dead,   MonsterTrigger.Die, s => !s.Owner.Health.IsAlive),
        StateTransition<Monster, MonsterTrigger>.Generate(chase,  dead,   MonsterTrigger.Die, s => !s.Owner.Health.IsAlive),
        StateTransition<Monster, MonsterTrigger>.Generate(attack, dead,   MonsterTrigger.Die, s => !s.Owner.Health.IsAlive),
        StateTransition<Monster, MonsterTrigger>.Generate(dead,   destroy, MonsterTrigger.Destroy),
    };

    public MonsterLeaderFSM(Monster owner, SpatialGrid<IUnit> unitGrid, ObstacleGrid obstacleGrid = null, bool skipCreate = false) : base(owner)
    {
        UnitGrid = unitGrid;
        ObstacleGrid = obstacleGrid;
        initialState = skipCreate ? (State<Monster, MonsterTrigger>)wander : create;
    }

    private bool EnemyInDetectionRange(State<Monster, MonsterTrigger> s)
    {
        // 피격 어그로 대상이 살아있으면 인식 범위 무관하게 추적 유지
        if (s.Owner.AggroTarget?.IsAlive == true) return true;

        if (UnitGrid == null) return false;
        var pos = (Vector2)s.Owner.Transform.position;
        float range = s.Owner.Combat.DetectionRange;
        foreach (var u in UnitGrid.Query(pos, range))
            if (u.Team != s.Owner.Team && u.IsAlive && Vector2.Distance(pos, (Vector2)u.Transform.position) <= range) return true;
        return false;
    }

    private bool EnemyInAttackRange(State<Monster, MonsterTrigger> s)
    {
        if (UnitGrid == null) return false;
        var pos = (Vector2)s.Owner.Transform.position;
        float range = s.Owner.Combat.AttackRange;
        foreach (var u in UnitGrid.Query(pos, range))
            if (u.Team != s.Owner.Team && u.IsAlive && Vector2.Distance(pos, (Vector2)u.Transform.position) <= range) return true;
        return false;
    }
}
