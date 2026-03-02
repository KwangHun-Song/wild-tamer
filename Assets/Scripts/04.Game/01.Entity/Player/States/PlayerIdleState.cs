using FiniteStateMachine;
using UnityEngine;

public class PlayerIdleState : State<Player, PlayerTrigger>
{
    private bool wasMoving;

    public override void OnEnter()
    {
        var dir = Owner.InputDirection;
        wasMoving = dir.magnitude > 0.01f;
        if (wasMoving) Owner.View.PlayMoveAnimation();
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
            if (!wasMoving)
            {
                Owner.View.PlayMoveAnimation();
                wasMoving = true;
            }
        }
        else
        {
            if (wasMoving)
            {
                Owner.View.Movement.Move(Vector2.zero);
                Owner.View.PlayIdleAnimation();
                wasMoving = false;
            }
        }
    }
}
