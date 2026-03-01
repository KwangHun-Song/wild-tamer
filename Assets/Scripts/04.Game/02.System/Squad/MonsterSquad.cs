using System;
using System.Collections.Generic;
using System.Linq;
using Base;
using UnityEngine;

/// <summary>
/// 같은 종류 몬스터들로 구성된 스쿼드.
/// 첫 번째 멤버가 리더가 되며, 리더가 사망하면 다음 생존 팔로워가 자동 승계한다.
/// 팔로워는 FlockBehavior로 리더를 추종한다.
/// </summary>
public class MonsterSquad
{
    private readonly List<Monster> members = new();
    private readonly Dictionary<Monster, Action> deathHandlers = new();
    private Monster leader;
    private readonly SpatialGrid<IUnit> unitGrid;
    private readonly FlockBehavior flock;

    public float StopRadius = 0.6f; // 리더 근처 팔로워 정지 반경

    public Monster Leader => leader;
    public IReadOnlyList<Monster> Members => members;
    public bool IsEmpty => members.Count == 0;
    public MonsterData Data { get; }

    public event Action<Monster> OnMemberDied;
    public event Action OnSquadEmpty;

    public MonsterSquad(MonsterData data, SpatialGrid<IUnit> unitGrid)
    {
        Data = data;
        this.unitGrid = unitGrid;
        flock = new FlockBehavior(data.flockSettings);

        var squadSettings = Facade.DB.Get<SquadSettingsData>("SquadSettings");
        if (squadSettings != null)
            StopRadius = squadSettings.stopRadius;
    }

    public void AddMember(Monster monster)
    {
        members.Add(monster);
        void handler() => HandleMemberDeath(monster);
        deathHandlers[monster] = handler;
        monster.Health.OnDeath += handler;
        if (leader == null) PromoteLeader(monster);
    }

    /// <summary>GameController.Update()에서 MonsterSquadSpawner를 통해 매 프레임 호출된다.</summary>
    public void Update(ObstacleGrid obstacleGrid, float deltaTime)
    {
        if (leader == null || !leader.IsAlive) return;

        var leaderTf = leader.Transform;
        Vector2 leaderPos = leaderTf.position;

        // 리더 AI 업데이트 (MonsterLeaderAI 내부에서 Move 호출)
        leader.Combat.Tick(deltaTime);
        leader.Update();

        // 팔로워: FlockBehavior로 리더 추종 — ToList()로 한 번만 구체화해 O(N²) 방지
        var aliveMembers = members.Where(m => m.IsAlive).ToList();

        foreach (var follower in members)
        {
            if (follower == leader || !follower.IsAlive) continue;

            follower.Combat.Tick(deltaTime);

            // 리더 근처이면 정지
            if (Vector2.Distance((Vector2)follower.Transform.position, leaderPos) <= StopRadius)
            {
                follower.Move(Vector2.zero);
                continue;
            }

            var dir = flock.CalculateDirection(follower, aliveMembers, leaderTf, obstacleGrid);
            var followerPos = (Vector2)follower.Transform.position;
            var resolved = obstacleGrid == null ? dir : new Vector2(
                obstacleGrid.IsWalkable(new Vector2(followerPos.x + dir.x * 0.5f, followerPos.y)) ? dir.x : 0f,
                obstacleGrid.IsWalkable(new Vector2(followerPos.x, followerPos.y + dir.y * 0.5f)) ? dir.y : 0f
            );
            follower.Move(resolved);
        }
    }

    private void HandleMemberDeath(Monster dead)
    {
        if (deathHandlers.TryGetValue(dead, out var handler))
        {
            dead.Health.OnDeath -= handler;
            deathHandlers.Remove(dead);
        }

        members.Remove(dead);
        OnMemberDied?.Invoke(dead);

        if (dead == leader)
        {
            leader = null;
            var next = members.FirstOrDefault(m => m.IsAlive);
            if (next != null)
                PromoteLeader(next);
        }

        if (members.Count == 0)
            OnSquadEmpty?.Invoke();
    }

    private void PromoteLeader(Monster monster)
    {
        leader = monster;
        monster.PromoteToLeader(unitGrid);
    }
}
