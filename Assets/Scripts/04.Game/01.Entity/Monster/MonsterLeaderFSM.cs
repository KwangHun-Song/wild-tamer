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

    private readonly MonsterWanderState wander = new();
    private readonly MonsterChaseState chase = new();
    private readonly MonsterAttackState attack = new();
    private readonly MonsterDeadState dead = new();

    protected override State<Monster, MonsterTrigger> InitialState => wander;

    protected override State<Monster, MonsterTrigger>[] States
        => new State<Monster, MonsterTrigger>[] { wander, chase, attack, dead };

    protected override StateTransition<Monster, MonsterTrigger>[] Transitions => new[]
    {
        StateTransition<Monster, MonsterTrigger>.Generate(wander, chase,  MonsterTrigger.DetectEnemy,      EnemyInDetectionRange),
        StateTransition<Monster, MonsterTrigger>.Generate(chase,  attack, MonsterTrigger.InAttackRange,    EnemyInAttackRange),
        StateTransition<Monster, MonsterTrigger>.Generate(chase,  wander, MonsterTrigger.LoseEnemy,        s => !EnemyInDetectionRange(s)),
        StateTransition<Monster, MonsterTrigger>.Generate(attack, chase,  MonsterTrigger.OutOfAttackRange, s => !EnemyInAttackRange(s)),
        StateTransition<Monster, MonsterTrigger>.Generate(wander, dead,   MonsterTrigger.Die, s => !s.Owner.Health.IsAlive),
        StateTransition<Monster, MonsterTrigger>.Generate(chase,  dead,   MonsterTrigger.Die, s => !s.Owner.Health.IsAlive),
        StateTransition<Monster, MonsterTrigger>.Generate(attack, dead,   MonsterTrigger.Die, s => !s.Owner.Health.IsAlive),
    };

    public MonsterLeaderFSM(Monster owner, SpatialGrid<IUnit> unitGrid, ObstacleGrid obstacleGrid = null) : base(owner)
    {
        UnitGrid = unitGrid;
        ObstacleGrid = obstacleGrid;
    }

    private bool EnemyInDetectionRange(State<Monster, MonsterTrigger> s)
    {
        if (UnitGrid == null) return false;
        var pos = (Vector2)s.Owner.Transform.position;
        foreach (var u in UnitGrid.Query(pos, s.Owner.Combat.DetectionRange))
            if (u.Team != s.Owner.Team && u.IsAlive) return true;
        return false;
    }

    private bool EnemyInAttackRange(State<Monster, MonsterTrigger> s)
    {
        if (UnitGrid == null) return false;
        var pos = (Vector2)s.Owner.Transform.position;
        foreach (var u in UnitGrid.Query(pos, s.Owner.Combat.AttackRange))
            if (u.Team != s.Owner.Team && u.IsAlive) return true;
        return false;
    }
}
