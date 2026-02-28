using FiniteStateMachine;
using UnityEngine;

public class MonsterAttackState : State<Monster, MonsterTrigger>
{
    public override void OnEnter() { /* TODO: attack animation */ }

    public override void OnExit()
    {
        Owner.Move(Vector2.zero);
    }
}
