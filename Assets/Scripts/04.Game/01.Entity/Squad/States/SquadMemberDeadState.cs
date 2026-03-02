using FiniteStateMachine;
using UnityEngine;

public class SquadMemberDeadState : State<SquadMember, SquadMemberTrigger>
{
    public override void OnEnter()
    {
        Owner.View.Movement.Move(Vector2.zero);
        Owner.View.PlayDeathSequence();
    }
}
