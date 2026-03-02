using FiniteStateMachine;

public class PlayerCreateState : State<Player, PlayerTrigger>
{
    public override void OnEnter()
    {
        Owner.View.PlayCreateAnimation(Owner.SignalCreated);
    }
}
