using FiniteStateMachine;

public class MonsterDeadState : State<Monster, MonsterTrigger>
{
    public override void OnEnter()
    {
        Owner.View.Movement.Move(UnityEngine.Vector2.zero);
        Owner.View.PlayDeadAnimation();
    }
}
