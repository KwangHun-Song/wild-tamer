using FiniteStateMachine;
using UnityEngine;

public class MonsterChaseState : State<Monster, MonsterTrigger>
{
    public override void OnEnter() { /* TODO: chase animation */ }

    public override void OnExit()
    {
        Owner.Move(Vector2.zero);
    }
}
