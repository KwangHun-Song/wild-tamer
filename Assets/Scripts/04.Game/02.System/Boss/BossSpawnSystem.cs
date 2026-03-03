using System;
using UnityEngine;
using Base;

/// <summary>
/// 보스 등장 타이머·경고 연출·스폰·사망 처리를 담당한다.
/// GameController가 소유하며 매 프레임 Update(deltaTime)를 호출한다.
/// </summary>
public class BossSpawnSystem
{
    private readonly BossMonsterData[]  bossPool;
    private readonly EntitySpawner      entitySpawner;
    private readonly BossWarningView    warningView;
    private readonly BossHpBarView      hpBarView;
    private readonly BossTimerView      timerView;
    private readonly SpatialGrid<IUnit> unitGrid;
    private readonly ObstacleGrid       obstacleGrid;
    private readonly Transform          playerTransform;
    private readonly Notifier           notifier;

    private float       elapsedTime;
    private float       respawnTimer;
    private BossMonster activeBoss;

    public event Action<BossMonster> OnBossSpawned;
    public event Action<BossMonster> OnBossDied;

    private const float SpawnTime    = 180f;
    private const float RespawnDelay = 240f;
    private const float WarnDuration = 2.5f;
    private const float SpawnOffset  = 15f;

    public BossSpawnSystem(BossMonsterData[]  bossPool,
                           EntitySpawner      entitySpawner,
                           BossWarningView    warningView,
                           BossHpBarView      hpBarView,
                           BossTimerView      timerView,
                           SpatialGrid<IUnit> unitGrid,
                           ObstacleGrid       obstacleGrid,
                           Transform          playerTransform,
                           Notifier           notifier)
    {
        this.bossPool        = bossPool;
        this.entitySpawner   = entitySpawner;
        this.warningView     = warningView;
        this.hpBarView       = hpBarView;
        this.timerView       = timerView;
        this.unitGrid        = unitGrid;
        this.obstacleGrid    = obstacleGrid;
        this.playerTransform = playerTransform;
        this.notifier        = notifier;
        respawnTimer         = -1f;
    }

    public void Update(float deltaTime)
    {
        if (activeBoss != null || bossPool == null || bossPool.Length == 0) return;

        elapsedTime  += deltaTime;
        respawnTimer -= deltaTime;

        if (elapsedTime < SpawnTime)
            timerView?.SetTime(SpawnTime - elapsedTime);

        if (elapsedTime >= SpawnTime && respawnTimer <= 0f)
            StartSpawnSequence();
    }

    private void StartSpawnSequence()
    {
        respawnTimer = float.MaxValue;
        timerView?.Hide();
        var data = bossPool[UnityEngine.Random.Range(0, bossPool.Length)];

        if (warningView != null)
            warningView.Show(data.displayName, data.icon, WarnDuration, () => SpawnBoss(data));
        else
            SpawnBoss(data);
    }

    private void SpawnBoss(BossMonsterData data)
    {
        var spawnPos = FindSpawnPosition();
        var go       = UnityEngine.Object.Instantiate(data.viewPrefab, spawnPos, Quaternion.identity);
        var view     = go.GetComponent<BossMonsterView>();

        activeBoss = new BossMonster(view, data, unitGrid, obstacleGrid, entitySpawner, notifier);
        activeBoss.Health.OnDeath += () => OnBossDefeated(activeBoss);

        entitySpawner.RegisterBoss(activeBoss);
        OnBossSpawned?.Invoke(activeBoss);
        hpBarView?.Bind(activeBoss);
    }

    private void OnBossDefeated(BossMonster boss)
    {
        entitySpawner.UnregisterBoss(boss);
        OnBossDied?.Invoke(boss);
        activeBoss   = null;
        respawnTimer = RespawnDelay;
    }

    private Vector2 FindSpawnPosition()
    {
        var center = (Vector2)playerTransform.position;
        for (int i = 0; i < 20; i++)
        {
            var dir = UnityEngine.Random.insideUnitCircle.normalized;
            var pos = center + dir * SpawnOffset;
            if (obstacleGrid.IsWalkable(pos)) return pos;
        }
        return center + Vector2.right * SpawnOffset;
    }
}
