using UnityEngine;
using Base;

/// <summary>보스 패턴 클래스들이 공유하는 정적 유틸리티.</summary>
internal static class BossPatternUtils
{
    /// <summary>보스 UnitGrid에서 가장 가까운 적(Enemy팀) 유닛을 반환한다.</summary>
    internal static IUnit FindNearestEnemy(BossMonster boss)
    {
        var pos = (Vector2)boss.Transform.position;
        IUnit nearest = null;
        float minDist = float.MaxValue;
        foreach (var u in boss.UnitGrid.Query(pos, boss.Combat.DetectionRange))
        {
            if (u.Team == boss.Team || !u.IsAlive) continue;
            float d = Vector2.Distance(pos, (Vector2)u.Transform.position);
            if (d < minDist) { minDist = d; nearest = u; }
        }
        return nearest;
    }

    /// <summary>
    /// 직선 영역(origin + dir * range, 폭 width) 내 적 유닛에 데미지를 적용한다.
    /// P3 CrossZone, P4 XZone에서 공용 사용.
    /// </summary>
    internal static void DamageLineArea(Vector2 origin, Vector2 dir, BossPatternData data,
                                        BossMonster boss, SpatialGrid<IUnit> unitGrid, Notifier notifier)
    {
        float halfWidth   = data.width * 0.5f;
        float queryRadius = data.range + halfWidth;

        foreach (var u in unitGrid.Query(origin, queryRadius))
        {
            if (u.Team == boss.Team || !u.IsAlive) continue;

            var   toUnit = (Vector2)u.Transform.position - origin;
            float along  = Vector2.Dot(toUnit, dir);
            if (along < 0f || along > data.range) continue;

            var perp = toUnit - dir * along;
            if (perp.magnitude <= halfWidth)
                DamageProcessor.ProcessDamage(boss, u, data.damage, notifier);
        }
    }
}
