using UnityEngine;
using Base;

/// <summary>P1 — 추적 장판. Warning 동안 플레이어를 추적하다 고정 후 폭발.</summary>
public class TrackingZonePattern : IBossPattern
{
    public void OnWarningTick(BossMonster boss, BossPatternData data, ref Vector2 lockedTarget)
    {
        var nearest = BossPatternUtils.FindNearestEnemy(boss);
        if (nearest != null)
            lockedTarget = nearest.Transform.position;

        boss.BossView.UpdateIndicatorPosition(BossPatternType.TrackingZone, lockedTarget);
    }

    public void Activate(BossMonster boss, BossPatternData data, Vector2 lockedTarget,
                         SpatialGrid<IUnit> unitGrid, Notifier notifier, BossMonsterView view)
    {
        foreach (var u in unitGrid.Query(lockedTarget, data.range))
        {
            if (u.Team == boss.Team || !u.IsAlive) continue;
            if (Vector2.Distance(lockedTarget, (Vector2)u.Transform.position) <= data.range)
                DamageProcessor.ProcessDamage(boss, u, data.damage, notifier);
        }
    }
}
