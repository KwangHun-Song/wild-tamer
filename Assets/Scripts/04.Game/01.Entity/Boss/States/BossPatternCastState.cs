using FiniteStateMachine;
using UnityEngine;

/// <summary>
/// 보스 패턴 시전 상태. Warning(인디케이터 표시) → Active(피해 판정) 두 단계로 진행.
/// SetPattern()은 BossChaseState가 PatternReady 발화 직전에 호출한다.
/// </summary>
public class BossPatternCastState : State<BossMonster, BossTrigger>
{
    private BossPatternData pattern;
    private IBossPattern    handler;
    private Vector2         lockedTarget;
    private float           phaseTimer;
    private bool            isWarning;
    private bool            chargeComplete;

    public void SetPattern(BossPatternData selected, IBossPattern h)
    {
        pattern        = selected;
        handler        = h;
        isWarning      = true;
        chargeComplete = false;
    }

    public override void OnEnter()
    {
        Owner.View.Movement.Move(Vector2.zero);
        lockedTarget = Owner.Transform.position;
        phaseTimer   = pattern.warningDuration;
        Owner.BossView.ShowIndicator(pattern.type, lockedTarget, pattern);
    }

    public override void OnUpdate()
    {
        phaseTimer -= Time.deltaTime;

        if (!Owner.IsAlive)
        {
            StateMachine.ExecuteCommand(BossTrigger.Die);
            return;
        }

        if (isWarning)
        {
            handler?.OnWarningTick(Owner, pattern, ref lockedTarget);
            if (phaseTimer <= 0f)
            {
                isWarning  = false;
                phaseTimer = pattern.activeDuration;
                ActivatePattern();
            }
        }
        else
        {
            if (phaseTimer <= 0f || chargeComplete)
            {
                Owner.BossView.HideIndicator(pattern.type);
                StateMachine.ExecuteCommand(BossTrigger.PatternComplete);
            }
        }
    }

    public override void OnExit() => Owner.BossView.HideAllIndicators();

    private void ActivatePattern()
    {
        Owner.BossView.FlashIndicator(pattern.type);
        handler?.Activate(Owner, pattern, lockedTarget, Owner.UnitGrid, Owner.Notifier, Owner.BossView);
        if (pattern.type == BossPatternType.Charge)
            Owner.BossView.RegisterChargeComplete(() => chargeComplete = true);
    }
}
