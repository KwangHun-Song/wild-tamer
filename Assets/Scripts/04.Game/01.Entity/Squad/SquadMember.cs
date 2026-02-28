using System;
using UnityEngine;

public class SquadMember : Character
{
    public override UnitTeam Team => UnitTeam.Player;
    public MonsterData Data { get; }
    public event Action<Vector2> OnMoveRequested;

    public SquadMember(SquadMemberView view, MonsterData data) : base(view, CreateCombat(data))
    {
        Data = data;
        view.Subscribe(this);
        view.Health.Initialize(data.maxHp);
        view.Movement.MoveSpeed = data.moveSpeed;
    }

    public void Move(Vector2 direction)
    {
        OnMoveRequested?.Invoke(direction);
    }

    private static UnitCombat CreateCombat(MonsterData d)
    {
        return new UnitCombat(d.attackDamage, d.attackRange, d.detectionRange, d.attackCooldown);
    }
}
