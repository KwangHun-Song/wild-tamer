using Base;
using FiniteStateMachine;
using UnityEngine;

public class PlayerIdleState : State<Player, PlayerTrigger>
{
    public override void OnEnter()
    {
        var dir = Owner.InputDirection;
        if (dir.magnitude > 0.01f) Owner.View.PlayMoveAnimation();
        else Owner.View.PlayIdleAnimation();
    }

    public override void OnUpdate()
    {
        var dir = Owner.InputDirection;
        bool moving = dir.magnitude > 0.01f;

        if (moving)
        {
            Owner.View.Movement.Move(dir);
            Owner.View.UpdateFacing(dir);
            if (!Owner.View.IsPlayingMoveAnimation())
            {
                Facade.Logger?.Log($"[Player] Idle → MOVE ({dir:F2})", LogLevel.Info, DebugColor.Cyan);
                Owner.View.PlayMoveAnimation();
            }
        }
        else
        {
            if (Owner.View.IsPlayingMoveAnimation())
            {
                Owner.View.Movement.Move(Vector2.zero);
                Facade.Logger?.Log("[Player] Idle → IDLE (정지)", LogLevel.Info, DebugColor.Yellow);
                Owner.View.PlayIdleAnimation();
            }
        }
    }
}
