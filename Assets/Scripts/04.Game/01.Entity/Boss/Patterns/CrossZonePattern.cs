using UnityEngine;
using Base;

/// <summary>P3 — 십자 장판. Warning 동안 보스 위치 중심 고정, Active에서 4방향(상하좌우) 직선 범위 피해.</summary>
public class CrossZonePattern : IBossPattern
{
    private static readonly Vector2[] Directions =
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right,
    };

    public void OnWarningTick(BossMonster boss, BossPatternData data, ref Vector2 lockedTarget)
    {
        lockedTarget = boss.Transform.position;
        boss.BossView.UpdateIndicatorPosition(BossPatternType.CrossZone, lockedTarget);
    }

    public void Activate(BossMonster boss, BossPatternData data, Vector2 lockedTarget,
                         SpatialGrid<IUnit> unitGrid, Notifier notifier, BossMonsterView view)
    {
        var origin = (Vector2)boss.Transform.position;
        foreach (var dir in Directions)
            BossPatternUtils.DamageLineArea(origin, dir, data, boss, unitGrid, notifier);
    }
}
