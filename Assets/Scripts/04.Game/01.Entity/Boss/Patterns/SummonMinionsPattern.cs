using UnityEngine;
using Base;

/// <summary>P7 — 졸개 소환. Active 시 보스 주변 랜덤 위치에 일반 몬스터를 summonCount마리 소환.</summary>
public class SummonMinionsPattern : IBossPattern
{
    private readonly EntitySpawner entitySpawner;
    private readonly ObstacleGrid  obstacleGrid;

    public SummonMinionsPattern(EntitySpawner entitySpawner, ObstacleGrid obstacleGrid)
    {
        this.entitySpawner = entitySpawner;
        this.obstacleGrid  = obstacleGrid;
    }

    public void Activate(BossMonster boss, BossPatternData data, Vector2 lockedTarget,
                         SpatialGrid<IUnit> unitGrid, Notifier notifier, BossMonsterView view)
    {
        var origin  = (Vector2)boss.Transform.position;
        int spawned = 0;

        for (int attempt = 0; attempt < 20 && spawned < data.summonCount; attempt++)
        {
            var offset = Random.insideUnitCircle.normalized * data.summonRadius;
            var pos    = origin + offset;
            if (!obstacleGrid.IsWalkable(pos)) continue;
            entitySpawner.SpawnMonster(data.summonData, pos);
            spawned++;
        }
    }
}
