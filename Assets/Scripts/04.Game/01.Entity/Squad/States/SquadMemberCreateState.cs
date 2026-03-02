using FiniteStateMachine;

public class SquadMemberCreateState : State<SquadMember, SquadMemberTrigger>
{
    public override void OnEnter()
    {
        Owner.View.PlayCreateAnimation(Owner.SignalCreated);
    }
}
