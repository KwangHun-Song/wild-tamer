using FiniteStateMachine;

public class MonsterDestroyState : State<Monster, MonsterTrigger>
{
    public override void OnEnter()
    {
        Owner.NotifyDestroyReady();
    }
}
