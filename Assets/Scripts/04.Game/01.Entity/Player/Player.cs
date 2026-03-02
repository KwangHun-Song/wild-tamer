using System;
using UnityEngine;

public class Player : Character
{
    public override UnitTeam Team => UnitTeam.Player;
    public override float Radius { get; }

    public Vector2 InputDirection { get; private set; }

    public event Action OnGameOver;

    private readonly PlayerFSM fsm;

    public Player(PlayerView view, UnitCombat combat, int maxHp, float radius = 0.3f) : base(view, combat)
    {
        Radius = radius;
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

    /// <summary>CreateState에서 연출 완료 후 호출. Idle 전환을 트리거한다.</summary>
    public void SignalCreated()
    {
        fsm.ExecuteCommand(PlayerTrigger.Created);
    }

    /// <summary>DeadState에서 DeathSequence 완료 후 호출. DestroyState로 전환을 트리거한다.</summary>
    public void RequestDestroy()
    {
        fsm.ExecuteCommand(PlayerTrigger.Destroy);
    }

    /// <summary>PlayerDestroyState에서 호출. 게임오버 이벤트를 발생시킨다.</summary>
    public void FireGameOver()
    {
        OnGameOver?.Invoke();
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
