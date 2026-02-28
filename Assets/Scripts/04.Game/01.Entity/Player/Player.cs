using System;
using UnityEngine;

public class Player : Character
{
    public override UnitTeam Team => UnitTeam.Player;

    public event Action<Vector2> OnMoveRequested;

    public Player(PlayerView view, UnitCombat combat, int maxHp) : base(view, combat)
    {
        Health.Initialize(maxHp);
        view.Subscribe(this);
    }

    public void Move(Vector2 direction) => OnMoveRequested?.Invoke(direction);
}
