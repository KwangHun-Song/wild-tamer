using FiniteStateMachine;
using UnityEngine;

public class PlayerAttackState : State<Player, PlayerTrigger>
{
    public override void OnEnter()
    {
        Owner.View.PlayAttackAnimation();
    }

    public override void OnUpdate()
    {
        // StopAttack: FSM Transition이 Combat.CanAttack 조건으로 자동 처리
        var dir = Owner.InputDirection;
        if (dir.magnitude > 0.01f)
            Owner.View.Movement.Move(dir);
        // Facing은 CombatSystem이 공격 대상 방향으로 업데이트하므로 입력 방향으로 덮어쓰지 않는다
    }
}
