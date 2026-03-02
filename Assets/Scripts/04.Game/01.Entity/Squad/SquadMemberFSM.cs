using FiniteStateMachine;
using UnityEngine;

/// <summary>
/// 스쿼드 멤버 FSM. Idle ↔ Attack → Dead 상태 전이를 관리한다.
/// - Idle→Attack: StartAttack 트리거 OR 공격 범위 내 적 존재 조건 → 둘 다 처리
/// - Attack→Idle: StopAttack 트리거 OR 공격 범위 내 적 없음 조건 → 둘 다 처리
/// - Die: Health.OnDeath 이벤트 → ExecuteCommand (트리거 전용)
/// </summary>
public class SquadMemberFSM : StateMachine<SquadMember, SquadMemberTrigger>
{
    public SpatialGrid<IUnit> UnitGrid { get; }

    private readonly SquadMemberIdleState idle = new();
    private readonly SquadMemberAttackState attack = new();
    private readonly SquadMemberDeadState dead = new();
    private readonly SquadMemberDestroyState destroy = new();

    protected override State<SquadMember, SquadMemberTrigger> InitialState => idle;

    protected override State<SquadMember, SquadMemberTrigger>[] States
        => new State<SquadMember, SquadMemberTrigger>[] { idle, attack, dead, destroy };

    protected override StateTransition<SquadMember, SquadMemberTrigger>[] Transitions => new[]
    {
        StateTransition<SquadMember, SquadMemberTrigger>.Generate(idle,   attack,  SquadMemberTrigger.StartAttack, EnemyInAttackRange),
        StateTransition<SquadMember, SquadMemberTrigger>.Generate(attack, idle,    SquadMemberTrigger.StopAttack,  s => !EnemyInAttackRange(s)),
        StateTransition<SquadMember, SquadMemberTrigger>.Generate(idle,   dead,    SquadMemberTrigger.Die, s => !s.Owner.Health.IsAlive),
        StateTransition<SquadMember, SquadMemberTrigger>.Generate(attack, dead,    SquadMemberTrigger.Die, s => !s.Owner.Health.IsAlive),
        StateTransition<SquadMember, SquadMemberTrigger>.Generate(dead,   destroy, SquadMemberTrigger.Destroy),
    };

    public SquadMemberFSM(SquadMember owner, SpatialGrid<IUnit> unitGrid) : base(owner)
    {
        UnitGrid = unitGrid;
    }

    private bool EnemyInAttackRange(State<SquadMember, SquadMemberTrigger> s)
    {
        if (UnitGrid == null) return false;
        var pos = (Vector2)s.Owner.Transform.position;
        float range = s.Owner.Combat.AttackRange;
        foreach (var u in UnitGrid.Query(pos, range))
            if (u.Team != s.Owner.Team && u.IsAlive && Vector2.Distance(pos, (Vector2)u.Transform.position) <= range) return true;
        return false;
    }
}
