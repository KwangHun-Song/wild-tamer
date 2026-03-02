using FiniteStateMachine;

public class PlayerDestroyState : State<Player, PlayerTrigger>
{
    public override void OnEnter()
    {
        Owner.FireGameOver();
    }
}
