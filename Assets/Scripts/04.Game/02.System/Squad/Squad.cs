using System;
using System.Collections.Generic;
using Base;
using UnityEngine;

public class Squad : IDisposable
{
    private readonly List<SquadMember> members     = new();
    private readonly FlockJobRunner    jobRunner;
    private readonly HashSet<SquadMember> stopped      = new();
    private readonly List<SquadMember>    stopSnapshot = new();

    public float StopRadius       = 0.6f;
    public float MemberStopRadius = 0.6f;

    public IReadOnlyList<SquadMember> Members => members;
    public int Count => members.Count;

    public event Action<SquadMember> OnMemberAdded;
    public event Action<SquadMember> OnMemberRemoved;

#if UNITY_EDITOR
    // 에디터 기즈모 전용 — 빌드에서는 완전 제거
    private readonly FlockBehavior flock;
    private Vector2[] memberPosCache = Array.Empty<Vector2>();
#endif

    public Squad()
    {
        var flockSettings = Facade.DB.Get<FlockSettingsData>("PlayerSquadFlock");
        jobRunner = new FlockJobRunner(flockSettings);

#if UNITY_EDITOR
        flock = new FlockBehavior(flockSettings);
#endif

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
        if (members.Count == 0) return;

        Vector2 leaderPos = leader.position;

        // 1단계: 리더 근처 멤버 정지 판정
        stopped.Clear();
        foreach (var member in members)
        {
            if (Vector2.Distance((Vector2)member.Transform.position, leaderPos) <= StopRadius)
                stopped.Add(member);
        }

        // 2단계: 정지 멤버 근처 연쇄 정지 (스냅샷 기반 1패스)
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

        // 3단계: FlockJob 스케줄 — 워커 스레드에서 Separation + Cohesion + Follow 병렬 계산
        var jobHandle = jobRunner.Schedule(members, stopped, leaderPos);

        // 4단계: Job 실행 중 Combat.Tick 병행 처리 (메인 스레드 유휴 시간 활용)
        foreach (var member in members)
            member.Combat.Tick(deltaTime);

        // 5단계: Job 완료 대기 → 방향 적용 → Avoidance 후처리 (메인 스레드)
        jobRunner.Complete(jobHandle, members, obstacleGrid);

        // 6단계: 에디터 기즈모 (10프레임마다, 빌드에서 완전 제거)
#if UNITY_EDITOR
        if (Time.frameCount % 10 == 0)
        {
            if (memberPosCache.Length < members.Count)
                memberPosCache = new Vector2[members.Count];
            for (int i = 0; i < members.Count; i++)
                memberPosCache[i] = members[i].Transform.position;

            var debugContext = new SquadContext(members, memberPosCache, leader, obstacleGrid);
            foreach (var member in members)
                member.SetFlockDebug(flock.ComputeDebugData(member, in debugContext));
        }
#endif

        // 7단계: FSM 구동
        foreach (var member in members)
            member.Update();
    }

    /// <summary>NativeArray 해제. Squad 소멸 시 반드시 호출해야 한다.</summary>
    public void Dispose()
    {
        jobRunner?.Dispose();
    }
}
