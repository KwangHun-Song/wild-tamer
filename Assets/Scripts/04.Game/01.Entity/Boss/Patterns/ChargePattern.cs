using System.Collections.Generic;
using UnityEngine;
using Base;

/// <summary>
/// P2 — 돌진 공격. Warning 동안 방향을 고정하고 Active에서 고속 돌진 후 경로상 유닛 피해.
/// 돌진 완료는 BossMonsterView 코루틴 콜백으로 BossPatternCastState에 통보된다.
/// </summary>
public class ChargePattern : IBossPattern
{
    public void OnWarningTick(BossMonster boss, BossPatternData data, ref Vector2 lockedTarget)
    {
        var nearest = BossPatternUtils.FindNearestEnemy(boss);
        if (nearest != null)
        {
            var dir = ((Vector2)nearest.Transform.position - (Vector2)boss.Transform.position).normalized;
            lockedTarget = dir; // P2에서 lockedTarget은 방향 벡터로 사용
        }

        boss.BossView.UpdateChargeIndicator(lockedTarget, data.chargeDistance, data.chargeWidth);
    }

    public void Activate(BossMonster boss, BossPatternData data, Vector2 lockedTarget,
                         SpatialGrid<IUnit> unitGrid, Notifier notifier, BossMonsterView view)
    {
        // lockedTarget은 방향 벡터. 돌진 이동은 View 코루틴이 처리하고 완료 시 콜백 호출.
        view.StartCharge(lockedTarget, data, boss, unitGrid, (hitUnits) =>
        {
            foreach (var u in hitUnits)
            {
                if (u.Team == boss.Team || !u.IsAlive) continue;
                DamageProcessor.ProcessDamage(boss, u, data.damage, notifier);
            }
        });
    }
}
