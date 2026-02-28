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

    // 씬 참조
    private readonly PlayerInput playerInput;
    private readonly ObstacleGrid obstacleGrid;

    // 공유 자원
    private readonly SpatialGrid<IUnit> unitGrid;
    public Notifier Notifier { get; } = new();
    public GamePhase Phase { get; private set; } = GamePhase.Play;

    public GameController(
        PlayerView playerView,
        PlayerInput playerInput,
        ObstacleGrid obstacleGrid)
    {
        this.playerInput  = playerInput;
        this.obstacleGrid = obstacleGrid;

        // 공유 SpatialGrid 생성 (CombatSystem, EntitySpawner, MonsterAI가 공유)
        unitGrid = new SpatialGrid<IUnit>(2f);

        // 개체 생성
        var playerCombat = new UnitCombat(10, 1.5f, 5f, 1f);
        Player = new Player(playerView, playerCombat, 100);
        Squad  = new Squad();

        // 시스템 생성
        combatSystem  = new CombatSystem(unitGrid, Notifier);
        entitySpawner = new EntitySpawner(unitGrid);
        tamingSystem  = new TamingSystem(Squad, entitySpawner, Notifier);

        // CombatSystem 유닛 등록
        combatSystem.RegisterUnit(Player);

        // Squad ↔ CombatSystem 자동 등록
        Squad.OnMemberAdded   += combatSystem.RegisterUnit;
        Squad.OnMemberRemoved += combatSystem.UnregisterUnit;

        // EntitySpawner ↔ CombatSystem 자동 등록
        entitySpawner.OnMonsterSpawned   += combatSystem.RegisterUnit;
        entitySpawner.OnMonsterDespawned += combatSystem.UnregisterUnit;
    }

    /// <summary>GameLoop(MonoBehaviour)에서 매 프레임 호출한다.</summary>
    public void Update()
    {
        if (Phase != GamePhase.Play) return;

        var dt = Time.deltaTime;

        // 1. 입력 → Player
        Player.Move(playerInput.MoveDirection);
        Player.Combat.Tick(dt);

        // 2. 부대 이동
        Squad.Update(Player.Transform, obstacleGrid, dt);

        // 3. 몬스터 AI (Combat.Tick 포함)
        entitySpawner.Update(dt);

        // 4. 전투
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
            var pos    = snapshot.PlayerPosition + memberSnap.PositionOffset;
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
        Squad.OnMemberAdded   -= combatSystem.RegisterUnit;
        Squad.OnMemberRemoved -= combatSystem.UnregisterUnit;
        entitySpawner.OnMonsterSpawned   -= combatSystem.RegisterUnit;
        entitySpawner.OnMonsterDespawned -= combatSystem.UnregisterUnit;
    }

    public void SetPhase(GamePhase phase) => Phase = phase;
}
