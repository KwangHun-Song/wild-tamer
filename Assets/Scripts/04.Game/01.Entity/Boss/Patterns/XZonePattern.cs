using UnityEngine;
using Base;

/// <summary>P4 — X자 장판. Warning 동안 보스 위치 중심 고정, Active에서 4방향(대각선) 직선 범위 피해.</summary>
public class XZonePattern : IBossPattern
{
    private static readonly float Inv = 1f / Mathf.Sqrt(2f);

    private static readonly Vector2[] Directions =
    {
        new Vector2( Inv,  Inv),
        new Vector2(-Inv,  Inv),
        new Vector2( Inv, -Inv),
        new Vector2(-Inv, -Inv),
    };

    public void OnWarningTick(BossMonster boss, BossPatternData data, ref Vector2 lockedTarget)
    {
        lockedTarget = boss.Transform.position;
        boss.BossView.UpdateIndicatorPosition(BossPatternType.XZone, lockedTarget);
    }

    public void Activate(BossMonster boss, BossPatternData data, Vector2 lockedTarget,
                         SpatialGrid<IUnit> unitGrid, Notifier notifier, BossMonsterView view)
    {
        var origin = (Vector2)boss.Transform.position;
        foreach (var dir in Directions)
            BossPatternUtils.DamageLineArea(origin, dir, data, boss, unitGrid, notifier);
    }
}
