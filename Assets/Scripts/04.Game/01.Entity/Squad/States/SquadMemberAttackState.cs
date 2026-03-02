using FiniteStateMachine;
using UnityEngine;

public class SquadMemberAttackState : State<SquadMember, SquadMemberTrigger>
{
    public override void OnEnter()
    {
        Owner.View.PlayAttackAnimation();
    }

    public override void OnUpdate()
    {
        // StopAttack: FSM Transition이 공격 범위 내 적 없음 조건으로 자동 처리

        // 공격 중에도 flock 방향으로 이동 가능
        var dir = Owner.DesiredMoveDirection;
        if (dir.magnitude > 0.01f)
        {
            Owner.View.Movement.Move(dir);
            Owner.View.UpdateFacing(dir);
        }

        if (Owner.Combat.CanAttack)
            Owner.Combat.ResetCooldown();
    }
}
