using System;
using System.Collections.Generic;
using Base;
using UnityEngine;

public class Squad
{
    private readonly List<SquadMember> members = new();
    private readonly FlockBehavior flock;
    private readonly HashSet<SquadMember> stopped      = new();
    private readonly List<SquadMember>    stopSnapshot = new();
    private Vector2[] memberPosCache = System.Array.Empty<Vector2>();

    public float StopRadius = 0.6f;       // 리더 근처 완전 정지 반경
    public float MemberStopRadius = 0.6f; // 정지 멤버 근처 연쇄 정지 반경

    public IReadOnlyList<SquadMember> Members => members;
    public int Count => members.Count;

    public event Action<SquadMember> OnMemberAdded;
    public event Action<SquadMember> OnMemberRemoved;

    public Squad()
    {
        var flockSettings = Facade.DB.Get<FlockSettingsData>("PlayerSquadFlock");
        flock = new FlockBehavior(flockSettings);

        var squadSettings = Facade.DB.Get<SquadSettingsData>("SquadSettings");
        if (squadSettings != null)
        {
            StopRadius       = squadSettings.stopRadius;
            MemberStopRadius = squadSettings.memberStopRadius;
        }
    }

    public void AddMember(SquadMember member)
    {
        members.Add(member);
        member.OnDied += HandleMemberDied;
        OnMemberAdded?.Invoke(member);
    }

    public void RemoveMember(SquadMember member)
    {
        member.OnDied -= HandleMemberDied;
        members.Remove(member);
        OnMemberRemoved?.Invoke(member);
    }

    private void HandleMemberDied(SquadMember member)
    {
        RemoveMember(member);
    }

    public void Clear()
    {
        for (int i = members.Count - 1; i >= 0; i--)
            RemoveMember(members[i]);
    }

    public void Update(Transform leader, ObstacleGrid obstacleGrid, float deltaTime)
    {
        Vector2 leaderPos = leader.position;

        // 1단계: 리더 근처 멤버 정지 판정
        stopped.Clear();
        foreach (var member in members)
        {
            if (Vector2.Distance((Vector2)member.Transform.position, leaderPos) <= StopRadius)
                stopped.Add(member);
        }

        // 2단계: 정지 멤버 근처 멤버 연쇄 정지 (스냅샷 기반으로 1패스만 수행)
        stopSnapshot.Clear();
        foreach (var s in stopped) stopSnapshot.Add(s);
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

        // 3단계: 멤버 위치 사전 캐싱 (CollectNeighbors Transform 접근 완전 제거)
        if (memberPosCache.Length < members.Count)
            memberPosCache = new Vector2[members.Count];
        for (int i = 0; i < members.Count; i++)
            memberPosCache[i] = members[i].Transform.position;

        var context = new SquadContext(members, memberPosCache, leader, obstacleGrid);

        int frameBucket = Time.frameCount % 5;

        for (int i = 0; i < members.Count; i++)
        {
            var member = members[i];
            member.Combat.Tick(deltaTime);

#if UNITY_EDITOR
            // 기즈모는 10프레임마다 재계산
            if (Time.frameCount % 10 == 0)
                member.SetFlockDebug(flock.ComputeDebugData(member, in context));
#endif

            if (stopped.Contains(member))
            {
                // 정지는 매 프레임 즉시 반영
                member.SetMoveDirection(Vector2.zero);
            }
            else if (i % 5 == frameBucket)
            {
                // 스태거드 업데이트: 멤버를 5버킷으로 나눠 매 프레임 1/5만 재계산
                // → FlockBehavior 비용 80% 절감, 버킷별로 분산되어 시각적 끊김 없음
                var direction = flock.CalculateDirection(member, in context);
                member.SetMoveDirection(direction);
            }
            // else: 이전 프레임 방향(DesiredMoveDirection) 유지

            member.Update();
        }
    }
}
