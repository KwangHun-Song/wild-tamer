using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Squad
{
    private readonly List<SquadMember> members = new();
    private readonly FlockBehavior flock;

    public float StopRadius = 0.5f;       // 리더 근처 완전 정지 반경
    public float MemberStopRadius = 0.5f; // 정지 멤버 근처 연쇄 정지 반경

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

        // 1단계: 리더 근처 멤버 정지 판정
        var stopped = new List<SquadMember>();
        foreach (var member in members)
        {
            if (Vector2.Distance((Vector2)member.Transform.position, leaderPos) <= StopRadius)
                stopped.Add(member);
        }

        // 2단계: 정지 멤버 근처 멤버 연쇄 정지 (스냅샷 기반으로 1패스만 수행)
        var stopSnapshot = new List<SquadMember>(stopped);
        foreach (var member in members)
        {
            if (stopped.Contains(member)) continue;
            foreach (var s in stopSnapshot)
            {
                if (Vector2.Distance((Vector2)member.Transform.position, (Vector2)s.Transform.position) <= MemberStopRadius)
                {
                    stopped.Add(member);
                    break;
                }
            }
        }

        // 3단계: 이동
        foreach (var member in members)
        {
            member.Combat.Tick(deltaTime);

            if (stopped.Contains(member))
            {
                member.Move(Vector2.zero);
                continue;
            }

            var direction = flock.CalculateDirection(member, members, leader, obstacleGrid);
            member.Move(direction);
        }
    }
}
