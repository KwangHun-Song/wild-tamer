using FiniteStateMachine;

/// <summary>보스 사망 상태. 진입 시 사망 연출 재생.</summary>
public class BossDeadState : State<BossMonster, BossTrigger>
{
    public override void OnEnter() => Owner.BossView.PlayDeathSequence();
}
