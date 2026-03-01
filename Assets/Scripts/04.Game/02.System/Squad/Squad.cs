using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Squad
{
    private readonly List<SquadMember> members = new();
    private readonly FlockBehavior flock;

    public IReadOnlyList<SquadMember> Members => members;
    public int Count => members.Count;

    public event Action<SquadMember> OnMemberAdded;
    public event Action<SquadMember> OnMemberRemoved;

    public Squad()
    {
        flock = new FlockBehavior();
    }

    public void AddMember(SquadMember member)
    {
        members.Add(member);
        OnMemberAdded?.Invoke(member);
    }

    public void RemoveMember(SquadMember member)
    {
        members.Remove(member);
        OnMemberRemoved?.Invoke(member);
    }

    public void Clear()
    {
        var copy = members.ToList();
        foreach (SquadMember member in copy)
        {
            RemoveMember(member);
        }
    }

    public void Update(Transform leader, ObstacleGrid obstacleGrid, float deltaTime)
    {
        Vector2 leaderPos = leader.position;
        foreach (SquadMember member in members)
        {
            member.Combat.Tick(deltaTime);
            var direction = flock.CalculateDirection(member, members, leader, obstacleGrid);

            // 리더와의 거리 비례로 속도 감소 — ArrivalRadius 이내에서 감속해 오버슈트 방지
            float dist = Vector2.Distance((Vector2)member.Transform.position, leaderPos);
            float speedScale = Mathf.Clamp01(dist / flock.ArrivalRadius);
            member.Move(direction * speedScale);
        }
    }
}
