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
        List<SquadMember> copy = members.ToList();
        foreach (SquadMember member in copy)
        {
            RemoveMember(member);
        }
    }

    public void Update(Transform leader, ObstacleGrid obstacleGrid, float deltaTime)
    {
        foreach (SquadMember member in members)
        {
            Vector2 direction = flock.CalculateDirection(member, members, leader, obstacleGrid);
            member.Move(direction);
        }
    }
}
