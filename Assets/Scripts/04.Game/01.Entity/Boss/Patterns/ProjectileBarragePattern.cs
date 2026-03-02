using UnityEngine;
using Base;

/// <summary>P6 — 투사체 난사. Warning 동안 가장 가까운 적을 추적하고, Active에서 해당 방향으로 투사체 연사.</summary>
public class ProjectileBarragePattern : IBossPattern
{
    public void OnWarningTick(BossMonster boss, BossPatternData data, ref Vector2 lockedTarget)
    {
        var nearest = BossPatternUtils.FindNearestEnemy(boss);
        if (nearest != null)
            lockedTarget = nearest.Transform.position;

        boss.BossView.UpdateIndicatorPosition(BossPatternType.ProjectileBarrage, lockedTarget);
    }

    public void Activate(BossMonster boss, BossPatternData data, Vector2 lockedTarget,
                         SpatialGrid<IUnit> unitGrid, Notifier notifier, BossMonsterView view)
    {
        var dir = (lockedTarget - (Vector2)boss.Transform.position).normalized;
        view.FireProjectiles(boss, dir, data, notifier);
    }
}
