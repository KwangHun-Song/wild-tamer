using FiniteStateMachine;

public class MonsterCreateState : State<Monster, MonsterTrigger>
{
    public override void OnEnter()
    {
        Owner.View.PlayCreateAnimation(Owner.SignalCreated);
    }
}
