using System;
using System.Collections.Generic;
using System.Linq;
using Base;
using UnityEngine;

/// <summary>
/// Monster, SquadMember, MonsterSquad의 스폰/디스폰을 담당한다.
/// OnMonsterSpawned / OnMonsterDespawned 이벤트로 GameController가 CombatSystem에 자동 등록한다.
/// </summary>
public class EntitySpawner
{
    private readonly SpatialGrid<IUnit> unitGrid;
    private readonly Transform unitRoot;
    private readonly List<Monster> activeMonsters = new();
    private readonly List<MonsterSquad> activeSquads = new();
    private BossMonster activeBoss;

    public IReadOnlyList<Monster> ActiveMonsters => activeMonsters;
    public IReadOnlyList<MonsterSquad> ActiveSquads => activeSquads;

    public event Action<Monster> OnMonsterSpawned;
    public event Action<Monster> OnMonsterDespawned;
    public event Action<MonsterSquad> OnSquadSpawned;
    public event Action<MonsterSquad> OnSquadDespawned;

    public EntitySpawner(SpatialGrid<IUnit> unitGrid, Transform unitRoot = null)
    {
        this.unitGrid = unitGrid;
        this.unitRoot = unitRoot;
    }

    public Monster SpawnMonster(MonsterData data, Vector2 position)
    {
        var go = Facade.Pool.Spawn(data.prefab);
        go.transform.SetParent(unitRoot, worldPositionStays: false);
        go.transform.position = position;
        var view = go.GetComponent<MonsterView>();
        var monster = new Monster(view, data, unitGrid);
        monster.OnReadyToDespawn += DespawnMonster;
        activeMonsters.Add(monster);
        OnMonsterSpawned?.Invoke(monster);
        return monster;
    }

    public SquadMember SpawnSquadMember(MonsterData data, Vector2 position)
    {
        var go = Facade.Pool.Spawn(data.squadPrefab);
        go.transform.SetParent(unitRoot, worldPositionStays: false);
        go.transform.position = position;
        var view = go.GetComponent<SquadMemberView>();
        var member = new SquadMember(view, data, unitGrid);
        member.OnDied += DespawnSquadMember;
        return member;
    }

    public void DespawnSquadMember(SquadMember member)
    {
        member.OnDied -= DespawnSquadMember;
        member.Cleanup();
        Facade.Pool.Despawn(member.Transform.gameObject);
    }

    public void DespawnMonster(Monster monster)
    {
        monster.OnReadyToDespawn -= DespawnMonster;
        monster.Cleanup();
        OnMonsterDespawned?.Invoke(monster);
        activeMonsters.Remove(monster);
        Facade.Pool.Despawn(monster.Transform.gameObject);
    }

    /// <summary>
    /// 몬스터 스쿼드를 스폰한다. count는 1~12로 클램프된다.
    /// 스쿼드 몬스터는 activeMonsters에 추가하지 않는다 — Update/Tick은 MonsterSquad.Update()가 담당.
    /// CombatSystem 등록은 OnMonsterSpawned 이벤트로 처리한다.
    /// </summary>
    public MonsterSquad SpawnMonsterSquad(MonsterData data, Vector2 position, int count, ObstacleGrid obstacleGrid = null)
    {
        count = Mathf.Clamp(count, 1, 12);
        var squad = new MonsterSquad(data, unitGrid);

        for (int i = 0; i < count; i++)
        {
            var spawnPos = i == 0 ? position : FindWalkableOffset(position, obstacleGrid);
            var role = i == 0 ? MonsterRole.Leader : MonsterRole.Follower;

            var go = Facade.Pool.Spawn(data.prefab);
            go.transform.SetParent(unitRoot, worldPositionStays: false);
            go.transform.position = spawnPos;
            var view = go.GetComponent<MonsterView>();
            var monster = new Monster(view, data, unitGrid, role, obstacleGrid);

            monster.OnReadyToDespawn += DespawnMonster;
            squad.AddMember(monster);
            OnMonsterSpawned?.Invoke(monster); // CombatSystem 등록
        }

        activeSquads.Add(squad);
        OnSquadSpawned?.Invoke(squad);
        return squad;
    }

    private static Vector2 FindWalkableOffset(Vector2 origin, ObstacleGrid grid)
    {
        if (grid == null) return origin + (Vector2)UnityEngine.Random.insideUnitCircle * 1.5f;
        for (int attempt = 0; attempt < 10; attempt++)
        {
            var candidate = origin + (Vector2)UnityEngine.Random.insideUnitCircle * 1.5f;
            if (grid.IsWalkable(candidate)) return candidate;
        }
        return origin;
    }

    /// <summary>스쿼드 전체를 강제 디스폰한다. 살아있는 멤버는 연출 없이 즉시 반환된다.</summary>
    public void DespawnMonsterSquad(MonsterSquad squad)
    {
        foreach (var m in squad.Members.ToList())
            DespawnMonster(m);

        activeSquads.Remove(squad);
        OnSquadDespawned?.Invoke(squad);
    }

    /// <summary>보스를 활성 보스로 등록하고 OnMonsterSpawned 이벤트를 발생시킨다 (CombatSystem 등록).</summary>
    public void RegisterBoss(BossMonster boss)
    {
        activeBoss = boss;
        OnMonsterSpawned?.Invoke(boss);
    }

    /// <summary>보스를 해제하고 OnMonsterDespawned 이벤트를 발생시킨다 (CombatSystem 해제).</summary>
    public void UnregisterBoss(BossMonster boss)
    {
        if (activeBoss != boss) return;
        activeBoss = null;
        OnMonsterDespawned?.Invoke(boss);
    }

    /// <summary>GameController.Update()에서 호출. 스탠드얼론 몬스터 AI 업데이트를 위임한다.</summary>
    public void Update(float deltaTime)
    {
        // 순회 중 DespawnMonster 호출에 의한 컬렉션 변경을 방지하기 위해 스냅샷 복사
        var snapshot = new List<Monster>(activeMonsters);
        foreach (var monster in snapshot)
        {
            monster.Combat.Tick(deltaTime);
            monster.Update();
        }

        if (activeBoss != null)
        {
            activeBoss.Combat.Tick(deltaTime);
            activeBoss.Update();
        }
    }
}
