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
    private readonly List<Monster>      activeMonsters = new();
    private readonly List<MonsterSquad> activeSquads   = new();

    public IReadOnlyList<Monster>      ActiveMonsters => activeMonsters;
    public IReadOnlyList<MonsterSquad> ActiveSquads   => activeSquads;

    public event Action<Monster>      OnMonsterSpawned;
    public event Action<Monster>      OnMonsterDespawned;
    public event Action<MonsterSquad> OnSquadSpawned;
    public event Action<MonsterSquad> OnSquadDespawned;

    public EntitySpawner(SpatialGrid<IUnit> unitGrid)
    {
        this.unitGrid = unitGrid;
    }

    public Monster SpawnMonster(MonsterData data, Vector2 position)
    {
        var go = Facade.Pool.Spawn(data.prefab);
        go.transform.position = position;
        var view = go.GetComponent<MonsterView>();
        var monster = new Monster(view, data, unitGrid);
        activeMonsters.Add(monster);
        OnMonsterSpawned?.Invoke(monster);
        return monster;
    }

    public SquadMember SpawnSquadMember(MonsterData data, Vector2 position)
    {
        var go = Facade.Pool.Spawn(data.squadPrefab);
        go.transform.position = position;
        var view = go.GetComponent<SquadMemberView>();
        return new SquadMember(view, data);
    }

    public void DespawnMonster(Monster monster)
    {
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
    public MonsterSquad SpawnMonsterSquad(MonsterData data, Vector2 position, int count)
    {
        count = Mathf.Clamp(count, 1, 12);
        var squad = new MonsterSquad(data, unitGrid);

        for (int i = 0; i < count; i++)
        {
            var offset   = i == 0 ? Vector2.zero : Random.insideUnitCircle * 1.5f;
            var spawnPos = position + offset;
            var role     = i == 0 ? MonsterRole.Leader : MonsterRole.Follower;

            var go      = Facade.Pool.Spawn(data.prefab);
            go.transform.position = spawnPos;
            var view    = go.GetComponent<MonsterView>();
            var monster = new Monster(view, data, unitGrid, role);

            squad.AddMember(monster);
            OnMonsterSpawned?.Invoke(monster); // CombatSystem 등록
        }

        squad.OnMemberDied += DespawnMonster;
        activeSquads.Add(squad);
        OnSquadSpawned?.Invoke(squad);
        return squad;
    }

    /// <summary>스쿼드 전체를 디스폰한다.</summary>
    public void DespawnMonsterSquad(MonsterSquad squad)
    {
        squad.OnMemberDied -= DespawnMonster;

        foreach (var m in squad.Members.ToList())
            DespawnMonster(m);

        activeSquads.Remove(squad);
        OnSquadDespawned?.Invoke(squad);
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
    }
}
