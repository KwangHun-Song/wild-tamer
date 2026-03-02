using FiniteStateMachine;
using UnityEngine;

public class SquadMemberIdleState : State<SquadMember, SquadMemberTrigger>
{
    private bool wasMoving;

    public override void OnEnter()
    {
        var dir = Owner.DesiredMoveDirection;
        wasMoving = dir.magnitude > 0.01f;
        if (wasMoving) Owner.View.PlayMoveAnimation();
        else Owner.View.PlayIdleAnimation();
    }

    public override void OnUpdate()
    {
        // StartAttack: FSM Transition이 공격 범위 내 적 탐지 조건으로 자동 처리
        var dir = Owner.DesiredMoveDirection;
        bool moving = dir.magnitude > 0.01f;

        if (moving)
        {
            Owner.View.Movement.Move(dir);
            Owner.View.UpdateFacing(dir);
            // IsPlayingMoveAnimation() 체크: 공격 애니 등 외부 트리거 후 걷기 애니가 끊겼을 때 재활성화
            if (!Owner.View.IsPlayingMoveAnimation())
                Owner.View.PlayMoveAnimation();
            wasMoving = true;
        }
        else
        {
            if (wasMoving)
            {
                Owner.View.Movement.Move(Vector2.zero);
                Owner.View.PlayIdleAnimation();
                wasMoving = false;
            }
        }
    }
}
