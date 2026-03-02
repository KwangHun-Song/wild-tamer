using FiniteStateMachine;

/// <summary>
/// 플레이어 FSM. Idle ↔ Attack → Dead 상태 전이를 관리한다.
/// - StartAttack: 외부 이벤트(OnAttackFired) → ExecuteCommand (트리거 전용)
/// - Attack→Idle: StopAttack 트리거 OR Combat.CanAttack 조건 → 둘 다 처리
/// - Die: Health.OnDeath 이벤트 → ExecuteCommand (트리거 전용)
/// </summary>
public class PlayerFSM : StateMachine<Player, PlayerTrigger>
{
    private readonly PlayerIdleState idle = new();
    private readonly PlayerAttackState attack = new();
    private readonly PlayerDeadState dead = new();

    protected override State<Player, PlayerTrigger> InitialState => idle;

    protected override State<Player, PlayerTrigger>[] States
        => new State<Player, PlayerTrigger>[] { idle, attack, dead };

    protected override StateTransition<Player, PlayerTrigger>[] Transitions => new[]
    {
        StateTransition<Player, PlayerTrigger>.Generate(idle,   attack, PlayerTrigger.StartAttack),
        StateTransition<Player, PlayerTrigger>.Generate(attack, idle,   PlayerTrigger.StopAttack, s => s.Owner.Combat.CanAttack),
        StateTransition<Player, PlayerTrigger>.Generate(idle,   dead,   PlayerTrigger.Die, s => !s.Owner.Health.IsAlive),
        StateTransition<Player, PlayerTrigger>.Generate(attack, dead,   PlayerTrigger.Die, s => !s.Owner.Health.IsAlive),
    };

    public PlayerFSM(Player owner) : base(owner) { }
}
