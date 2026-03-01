using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 카메라 외곽에서 몬스터 스쿼드를 주기적으로 스폰하고,
/// 플레이어와 너무 멀어진 스쿼드를 자동 디스폰한다.
/// </summary>
public class MonsterSquadSpawner
{
    public int   MinSquadCount      = 3;
    public int   MaxSquadCount      = 8;
    public int   MinMembersPerSquad = 1;
    public int   MaxMembersPerSquad = 12;
    public float SpawnMargin        = 3f;   // 카메라 경계 밖 추가 여유 거리
    public float DespawnDistance    = 35f;  // 플레이어 기준 디스폰 반경
    public float SpawnInterval      = 8f;   // 스폰 시도 주기(초)

    private readonly EntitySpawner      entitySpawner;
    private readonly ObstacleGrid       obstacleGrid;
    private readonly Transform          playerTransform;
    private readonly Camera             camera;
    private readonly MonsterData[]      spawnTable;

    private float spawnTimer;

    public MonsterSquadSpawner(
        EntitySpawner entitySpawner,
        ObstacleGrid obstacleGrid,
        Transform playerTransform,
        Camera camera,
        MonsterData[] spawnTable)
    {
        this.entitySpawner   = entitySpawner;
        this.obstacleGrid    = obstacleGrid;
        this.playerTransform = playerTransform;
        this.camera          = camera;
        this.spawnTable      = spawnTable;
        spawnTimer           = SpawnInterval;
    }

    /// <summary>GameController.Update()에서 매 프레임 호출한다.</summary>
    public void Update(float deltaTime)
    {
        // 1. 스쿼드 AI 및 FlockBehavior 업데이트
        foreach (var squad in entitySpawner.ActiveSquads)
            squad.Update(obstacleGrid, deltaTime);

        // 2. 원거리 스쿼드 자동 디스폰
        TryDespawnFarSquads();

        // 3. 스폰 주기 체크
        spawnTimer -= deltaTime;
        if (spawnTimer <= 0f)
        {
            TrySpawnSquad();
            spawnTimer = SpawnInterval;
        }
    }

    private void TrySpawnSquad()
    {
        if (entitySpawner.ActiveSquads.Count >= MaxSquadCount) return;
        if (spawnTable == null || spawnTable.Length == 0) return;

        var pos   = FindSpawnPositionOutsideCamera();
        var data  = spawnTable[Random.Range(0, spawnTable.Length)];
        var count = Random.Range(MinMembersPerSquad, MaxMembersPerSquad + 1);
        entitySpawner.SpawnMonsterSquad(data, pos, count);
    }

    private void TryDespawnFarSquads()
    {
        Vector2 playerPos = playerTransform.position;

        foreach (var squad in entitySpawner.ActiveSquads.ToList())
        {
            if (squad.Leader == null) continue;
            float dist = Vector2.Distance(playerPos, (Vector2)squad.Leader.Transform.position);
            if (dist > DespawnDistance)
                entitySpawner.DespawnMonsterSquad(squad);
        }
    }

    private Vector2 FindSpawnPositionOutsideCamera()
    {
        var   camPos = (Vector2)camera.transform.position;
        float halfH  = camera.orthographicSize + SpawnMargin;
        float halfW  = halfH * camera.aspect   + SpawnMargin;

        return Random.Range(0, 4) switch
        {
            0 => new Vector2(Random.Range(camPos.x - halfW, camPos.x + halfW), camPos.y + halfH), // 위
            1 => new Vector2(Random.Range(camPos.x - halfW, camPos.x + halfW), camPos.y - halfH), // 아래
            2 => new Vector2(camPos.x - halfW, Random.Range(camPos.y - halfH, camPos.y + halfH)), // 왼쪽
            _ => new Vector2(camPos.x + halfW, Random.Range(camPos.y - halfH, camPos.y + halfH)), // 오른쪽
        };
    }
}
