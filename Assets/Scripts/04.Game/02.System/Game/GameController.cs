using System.Collections.Generic;
using Base;
using UnityEngine;

/// <summary>
/// 모든 pure C# 시스템을 소유하고 게임 페이즈에 따라 Update와 입력을 게이팅하는 중앙 오케스트레이터.
/// </summary>
public class GameController
{
    // 개체
    public Player Player { get; }
    public Squad Squad { get; }

    // 시스템
    private readonly CombatSystem combatSystem;
    private readonly EntitySpawner entitySpawner;
    private readonly TamingSystem tamingSystem;
    private readonly MonsterSquadSpawner squadSpawner;
    private readonly BossSpawnSystem bossSpawnSystem;

    // 씬 참조
    private readonly PlayerInput playerInput;
    private readonly ObstacleGrid obstacleGrid;

    // 공유 자원
    private readonly SpatialGrid<IUnit> unitGrid;
    public IReadOnlyList<Monster> ActiveMonsters => entitySpawner.ActiveMonsters;
    public Notifier Notifier { get; } = new();
    public GamePhase Phase { get; private set; } = GamePhase.Play;

    public GameController(
        PlayerView playerView,
        PlayerInput playerInput,
        ObstacleGrid obstacleGrid,
        Camera gameCamera = null,
        MonsterData[] monsterSpawnTable = null,
        Transform unitRoot = null,
        BossMonsterData[] bossPool = null,
        BossWarningView bossWarningView = null,
        BossHpBarView bossHpBarView = null)
    {
        this.playerInput = playerInput;
        this.obstacleGrid = obstacleGrid;

        // 공유 SpatialGrid 생성 (CombatSystem, EntitySpawner, MonsterAI가 공유)
        unitGrid = new SpatialGrid<IUnit>(2f);

        // 개체 생성
        var playerData = Facade.DB.Get<PlayerData>("PlayerData");
        var playerCombat = playerData != null
            ? new UnitCombat(playerData.attackDamage, playerData.attackRange, 0f, playerData.attackCooldown)
            : new UnitCombat(10, 1.5f, 0f, 1f);
        Player = new Player(playerView, playerCombat, playerData?.maxHp ?? 100, playerData?.radius ?? 0.3f);
        if (playerData != null)
            playerView.Movement.MoveSpeed = playerData.moveSpeed;
        Squad = new Squad();

        // 시스템 생성
        combatSystem = new CombatSystem(unitGrid, Notifier);
        entitySpawner = new EntitySpawner(unitGrid, unitRoot);
        tamingSystem = new TamingSystem(Squad, entitySpawner, Notifier);

        // CombatSystem 유닛 등록
        combatSystem.RegisterUnit(Player);

        // Squad ↔ CombatSystem 자동 등록
        Squad.OnMemberAdded += combatSystem.RegisterUnit;
        Squad.OnMemberRemoved += combatSystem.UnregisterUnit;

        // EntitySpawner ↔ CombatSystem 자동 등록
        entitySpawner.OnMonsterSpawned += combatSystem.RegisterUnit;
        entitySpawner.OnMonsterDespawned += combatSystem.UnregisterUnit;

        // 몬스터 스쿼드 스포너 (카메라가 없으면 비활성)
        if (gameCamera != null)
        {
            squadSpawner = new MonsterSquadSpawner(
                entitySpawner,
                obstacleGrid,
                Player.Transform,
                gameCamera,
                monsterSpawnTable);
        }

        // 보스 스폰 시스템 (bossPool이 있을 때만 활성)
        if (bossPool != null && bossPool.Length > 0)
        {
            bossSpawnSystem = new BossSpawnSystem(
                bossPool, entitySpawner, bossWarningView, bossHpBarView,
                unitGrid, obstacleGrid, Player.Transform, Notifier);
        }
    }

    /// <summary>GameLoop(MonoBehaviour)에서 매 프레임 호출한다.</summary>
    public void Update()
    {
        if (Phase != GamePhase.Play) return;

        var dt = Time.deltaTime;

        // 1. 입력 → Player (축별 장애물 충돌 체크)
        var rawDir = playerInput.MoveDirection;
        var resolvedDir = Vector2.zero;
        if (rawDir.magnitude > 0.01f)
        {
            var pos = (Vector2)Player.Transform.position;
            resolvedDir = new Vector2(
                obstacleGrid.IsWalkable(new Vector2(pos.x + rawDir.x * 0.5f, pos.y)) ? rawDir.x : 0f,
                obstacleGrid.IsWalkable(new Vector2(pos.x, pos.y + rawDir.y * 0.5f)) ? rawDir.y : 0f
            );
            Player.Combat.Tick(dt);
        }
        Player.SetInput(resolvedDir);
        Player.Update();

        // 2. 부대 이동
        Squad.Update(Player.Transform, obstacleGrid, dt);

        // 3. 스탠드얼론 몬스터 AI (Combat.Tick 포함)
        entitySpawner.Update(dt);

        // 4. 몬스터 스쿼드 스폰/디스폰/AI
        squadSpawner?.Update(dt);

        // 5. 보스 스폰 타이머
        bossSpawnSystem?.Update(dt);

        // 6. 전투
        combatSystem.Update();
    }

    public GameSnapshot CreateSnapshot()
    {
        var playerPos = (Vector2)Player.Transform.position;
        var squadSnaps = new System.Collections.Generic.List<SquadMemberSnapshot>();
        foreach (var m in Squad.Members)
            squadSnaps.Add(new SquadMemberSnapshot(m, playerPos));

        var monsterSnaps = new System.Collections.Generic.List<MonsterSnapshot>();
        foreach (var m in entitySpawner.ActiveMonsters)
            monsterSnaps.Add(new MonsterSnapshot(m));

        return new GameSnapshot(playerPos, squadSnaps, monsterSnaps, null);
    }

    public void RestoreFromSnapshot(GameSnapshot snapshot)
    {
        // 기존 몬스터 정리
        var monsters = new System.Collections.Generic.List<Monster>(entitySpawner.ActiveMonsters);
        foreach (var monster in monsters)
            entitySpawner.DespawnMonster(monster);

        // 플레이어 위치 복원
        Player.SetPosition(snapshot.PlayerPosition);

        // 부대원 복원
        Squad.Clear();
        foreach (var memberSnap in snapshot.SquadMembers)
        {
            var pos = snapshot.PlayerPosition + memberSnap.PositionOffset;
            var member = entitySpawner.SpawnSquadMember(memberSnap.Data, pos);
            Squad.AddMember(member);
        }

        // 몬스터 복원
        foreach (var monsterSnap in snapshot.Monsters)
            entitySpawner.SpawnMonster(monsterSnap.Data, monsterSnap.Position);
    }

    /// <summary>GameLoop.OnDestroy()에서 호출. 이벤트 구독을 해제하여 메모리 누수를 방지한다.</summary>
    public void Cleanup()
    {
        Squad.OnMemberAdded -= combatSystem.RegisterUnit;
        Squad.OnMemberRemoved -= combatSystem.UnregisterUnit;
        entitySpawner.OnMonsterSpawned -= combatSystem.RegisterUnit;
        entitySpawner.OnMonsterDespawned -= combatSystem.UnregisterUnit;
        tamingSystem.Dispose();
    }

    public void SetPhase(GamePhase phase) => Phase = phase;

    /// <summary>치트: 플레이어 주변에 스쿼드 멤버를 즉시 스폰한다.</summary>
    public void CheatSpawnSquadMember(MonsterData data, Vector2 position)
    {
        var member = entitySpawner.SpawnSquadMember(data, position);
        Squad.AddMember(member);
    }

    /// <summary>테스트용: 게임 시작 시 부대원과 몬스터를 초기 배치한다.</summary>
    public void SpawnTestEntities(MonsterData[] squadData, MonsterData[] monsterData, Vector2 origin)
    {
        for (int i = 0; i < squadData.Length; i++)
        {
            var pos = origin + new Vector2((i + 1) * 1.5f, 0f);
            var member = entitySpawner.SpawnSquadMember(squadData[i], pos);
            Squad.AddMember(member);
        }

        for (int i = 0; i < monsterData.Length; i++)
        {
            var pos = origin + new Vector2((i - monsterData.Length / 2f) * 2.5f, 6f);
            entitySpawner.SpawnMonster(monsterData[i], pos);
        }
    }
}
