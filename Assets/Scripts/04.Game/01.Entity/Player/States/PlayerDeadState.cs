using FiniteStateMachine;
using UnityEngine;

public class PlayerDeadState : State<Player, PlayerTrigger>
{
    public override void OnEnter()
    {
        Owner.View.Movement.Move(Vector2.zero);
        Owner.View.PlayDeadAnimation();
    }
}
