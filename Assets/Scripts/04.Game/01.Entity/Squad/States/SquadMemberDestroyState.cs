using FiniteStateMachine;

public class SquadMemberDestroyState : State<SquadMember, SquadMemberTrigger>
{
    public override void OnEnter()
    {
        Owner.NotifyDied();
    }
}
