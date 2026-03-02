using UnityEngine;

public class Player : Character
{
    public override UnitTeam Team => UnitTeam.Player;

    public Vector2 InputDirection { get; private set; }

    private readonly PlayerFSM fsm;

    public Player(PlayerView view, UnitCombat combat, int maxHp) : base(view, combat)
    {
        Health.Initialize(maxHp);
        Health.OnDeath += OnHealthDeath;
        fsm = new PlayerFSM(this);
        fsm.SetUp();
        OnAttackFired += () => fsm.ExecuteCommand(PlayerTrigger.StartAttack);
    }

    private void OnHealthDeath()
    {
        fsm.ExecuteCommand(PlayerTrigger.Die);
    }

    /// <summary>GameController가 매 프레임 호출. 장애물 보정이 끝난 방향을 전달한다.</summary>
    public void SetInput(Vector2 direction)
    {
        InputDirection = direction;
    }

    /// <summary>GameController.Update()에서 매 프레임 호출하여 FSM을 구동한다.</summary>
    public void Update()
    {
        fsm.Update();
#if UNITY_EDITOR
        View.SetGizmoLabel(fsm.CurrentState?.GetType().Name ?? "None");
#endif
    }
}
