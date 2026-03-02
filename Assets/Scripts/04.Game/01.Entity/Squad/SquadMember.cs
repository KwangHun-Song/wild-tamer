using System;
using UnityEngine;

public class SquadMember : Character
{
    public override UnitTeam Team => UnitTeam.Player;
    public override float Radius => Data.radius;
    public MonsterData Data { get; }

    public Vector2 DesiredMoveDirection { get; private set; }

    public event Action<SquadMember> OnDied;

    private readonly SquadMemberFSM fsm;

    public SquadMember(SquadMemberView view, MonsterData data, SpatialGrid<IUnit> unitGrid) : base(view, CreateCombat(data))
    {
        Data = data;
        Health.Initialize(data.maxHp);
        View.Movement.MoveSpeed = data.squadMoveSpeed;
        fsm = new SquadMemberFSM(this, unitGrid);
        fsm.SetUp();
        Health.OnDeath   += OnHealthDeath;
        OnAttackFired    += View.PlayAttackAnimation;
#if UNITY_EDITOR
        view.Subscribe(this);
#endif
    }

    private void OnHealthDeath()
    {
        fsm.ExecuteCommand(SquadMemberTrigger.Die);
    }

    /// <summary>CreateState에서 연출 완료 후 호출. Idle 전환을 트리거한다.</summary>
    public void SignalCreated()
    {
        fsm.ExecuteCommand(SquadMemberTrigger.Created);
    }

    /// <summary>DeadState에서 DeathSequence 완료 후 호출. DestroyState로 전환을 트리거한다.</summary>
    public void RequestDestroy()
    {
        fsm.ExecuteCommand(SquadMemberTrigger.Destroy);
    }

    /// <summary>SquadMemberDestroyState에서 호출. OnDied 이벤트를 발생시켜 Despawn을 트리거한다.</summary>
    public void NotifyDied()
    {
        OnDied?.Invoke(this);
    }

    public void Cleanup()
    {
        Health.OnDeath   -= OnHealthDeath;
        OnAttackFired    -= View.PlayAttackAnimation;
    }

    /// <summary>Squad.Update()에서 매 프레임 호출. FlockBehavior가 계산한 이동 방향을 전달한다.</summary>
    public void SetMoveDirection(Vector2 direction)
    {
        DesiredMoveDirection = direction;
    }

    /// <summary>Squad.Update()에서 매 프레임 호출하여 FSM을 구동한다.</summary>
    public void Update()
    {
        fsm.Update();
#if UNITY_EDITOR
        View.SetGizmoLabel(fsm.CurrentState?.GetType().Name ?? "None");
#endif
    }

    private static UnitCombat CreateCombat(MonsterData d)
    {
        return new UnitCombat(d.attackDamage, d.attackRange, d.detectionRange, d.attackCooldown);
    }

#if UNITY_EDITOR
    public FlockBehavior.FlockDebugData FlockDebug { get; private set; }

    public void SetFlockDebug(FlockBehavior.FlockDebugData data)
    {
        FlockDebug = data;
    }
#endif
}
