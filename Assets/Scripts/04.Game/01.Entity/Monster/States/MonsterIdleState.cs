using FiniteStateMachine;

public class MonsterIdleState : State<Monster, MonsterTrigger>
{
    public override void OnEnter()
    {
        Owner.View.PlayIdleAnimation();
    }

    // DetectEnemy: FSM Transition이 탐지 범위 내 적 조건으로 자동 처리 — OnUpdate 불필요
}
