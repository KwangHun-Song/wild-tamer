using System.Collections.Generic;
using FiniteStateMachine;
using UnityEngine;

/// <summary>
/// 스탠드얼론 몬스터 FSM. Idle → Chase → Attack 상태 전이를 관리한다.
/// - DetectEnemy / LoseEnemy / InAttackRange / OutOfAttackRange: 트리거 + 조건 둘 다 처리
/// - Die: Health.OnDeath 이벤트 → ExecuteCommand (트리거 전용)
/// </summary>
public class MonsterStandaloneFSM : StateMachine<Monster, MonsterTrigger>
{
    public SpatialGrid<IUnit>   UnitGrid { get; }
    private readonly List<IUnit> queryBuffer = new();

    private readonly MonsterCreateState create = new();
    private readonly MonsterIdleState idle = new();
    private readonly MonsterChaseState chase = new();
    private readonly MonsterAttackState attack = new();
    private readonly MonsterDeadState dead = new();
    private readonly MonsterDestroyState destroy = new();

    protected override State<Monster, MonsterTrigger> InitialState => create;

    protected override State<Monster, MonsterTrigger>[] States
        => new State<Monster, MonsterTrigger>[] { create, idle, chase, attack, dead, destroy };

    protected override StateTransition<Monster, MonsterTrigger>[] Transitions => new[]
    {
        StateTransition<Monster, MonsterTrigger>.Generate(create, idle,   MonsterTrigger.Created),
        StateTransition<Monster, MonsterTrigger>.Generate(idle,   chase,  MonsterTrigger.DetectEnemy,      EnemyInDetectionRange),
        StateTransition<Monster, MonsterTrigger>.Generate(chase,  attack, MonsterTrigger.InAttackRange,    EnemyInAttackRange),
        StateTransition<Monster, MonsterTrigger>.Generate(chase,  idle,   MonsterTrigger.LoseEnemy,        s => !EnemyInDetectionRange(s)),
        StateTransition<Monster, MonsterTrigger>.Generate(attack, chase,  MonsterTrigger.OutOfAttackRange, s => !EnemyInAttackRange(s)),
        StateTransition<Monster, MonsterTrigger>.Generate(idle,   dead,   MonsterTrigger.Die, s => !s.Owner.Health.IsAlive),
        StateTransition<Monster, MonsterTrigger>.Generate(chase,  dead,   MonsterTrigger.Die, s => !s.Owner.Health.IsAlive),
        StateTransition<Monster, MonsterTrigger>.Generate(attack, dead,   MonsterTrigger.Die, s => !s.Owner.Health.IsAlive),
        StateTransition<Monster, MonsterTrigger>.Generate(dead,   destroy, MonsterTrigger.Destroy),
    };

    public MonsterStandaloneFSM(Monster owner, SpatialGrid<IUnit> unitGrid) : base(owner)
    {
        UnitGrid = unitGrid;
    }

    private bool EnemyInDetectionRange(State<Monster, MonsterTrigger> s)
    {
        // 피격 어그로 대상이 살아있으면 인식 범위 무관하게 추적 유지
        if (s.Owner.AggroTarget?.IsAlive == true) return true;

        if (UnitGrid == null) return false;
        var pos = (Vector2)s.Owner.Transform.position;
        float range = s.Owner.Combat.DetectionRange;
        queryBuffer.Clear();
        UnitGrid.Query(pos, range, queryBuffer);
        foreach (var u in queryBuffer)
            if (u.Team != s.Owner.Team && u.IsAlive && Vector2.Distance(pos, (Vector2)u.Transform.position) <= range) return true;
        return false;
    }

    private bool EnemyInAttackRange(State<Monster, MonsterTrigger> s)
    {
        if (UnitGrid == null) return false;
        var pos = (Vector2)s.Owner.Transform.position;
        float range = s.Owner.Combat.AttackRange;
        queryBuffer.Clear();
        UnitGrid.Query(pos, range, queryBuffer);
        foreach (var u in queryBuffer)
            if (u.Team != s.Owner.Team && u.IsAlive && Vector2.Distance(pos, (Vector2)u.Transform.position) <= range) return true;
        return false;
    }
}
