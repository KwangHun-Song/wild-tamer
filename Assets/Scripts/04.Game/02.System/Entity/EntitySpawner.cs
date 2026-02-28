using System;
using System.Collections.Generic;
using Base;
using UnityEngine;

/// <summary>
/// Monster와 SquadMember의 스폰/디스폰을 담당한다.
/// OnMonsterSpawned / OnMonsterDespawned 이벤트로 GameController가 CombatSystem에 자동 등록한다.
/// </summary>
public class EntitySpawner
{
    private readonly SpatialGrid<IUnit> unitGrid;
    private readonly List<Monster> activeMonsters = new();

    public IReadOnlyList<Monster> ActiveMonsters => activeMonsters;

    public event Action<Monster> OnMonsterSpawned;
    public event Action<Monster> OnMonsterDespawned;

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
        OnMonsterDespawned?.Invoke(monster);
        activeMonsters.Remove(monster);
        Facade.Pool.Despawn(monster.Transform.gameObject);
    }

    /// <summary>GameController.Update()에서 호출. AI 업데이트를 각 몬스터에 위임한다.</summary>
    public void Update(float deltaTime)
    {
        foreach (var monster in activeMonsters)
        {
            monster.Combat.Tick(deltaTime);
            monster.Update();
        }
    }
}
