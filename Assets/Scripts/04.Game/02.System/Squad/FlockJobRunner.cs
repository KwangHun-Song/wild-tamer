using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

/// <summary>
/// FlockJob의 NativeArray 수명을 관리하고 Schedule / Complete를 담당한다.
/// Squad가 소유하며 Squad.Dispose() 시 반드시 Dispose()를 호출해야 한다.
/// </summary>
public class FlockJobRunner : IDisposable
{
    private NativeArray<Vector2> positions;
    private NativeArray<bool>    stoppedFlags;
    private NativeArray<Vector2> outDirections;
    private int capacity;

    private readonly FlockSettingsData settings;

    public FlockJobRunner(FlockSettingsData settings)
    {
        this.settings = settings;
        Reallocate(16);
    }

    private void Reallocate(int size)
    {
        if (positions.IsCreated)      positions.Dispose();
        if (stoppedFlags.IsCreated)   stoppedFlags.Dispose();
        if (outDirections.IsCreated)  outDirections.Dispose();

        positions     = new NativeArray<Vector2>(size, Allocator.Persistent);
        stoppedFlags  = new NativeArray<bool>   (size, Allocator.Persistent);
        outDirections = new NativeArray<Vector2>(size, Allocator.Persistent);
        capacity      = size;
    }

    /// <summary>
    /// Phase 1+2: 위치/정지 데이터를 NativeArray에 복사한 뒤 Job을 스케줄한다.
    /// 반환된 JobHandle.Complete()를 호출하기 전에 results를 읽으면 안 된다.
    /// </summary>
    public JobHandle Schedule(
        IReadOnlyList<SquadMember> members,
        HashSet<SquadMember>       stopped,
        Vector2                    leaderPos)
    {
        int count = members.Count;
        if (count == 0) return default;

        if (count > capacity)
            Reallocate(count * 2);

        // 메인 스레드: Transform 읽기 + 정지 여부 복사
        for (int i = 0; i < count; i++)
        {
            positions[i]    = members[i].Transform.position;
            stoppedFlags[i] = stopped.Contains(members[i]);
        }

        var job = new FlockJob
        {
            Positions             = positions,
            StoppedFlags          = stoppedFlags,
            LeaderPos             = leaderPos,
            NeighborRadius        = settings != null ? settings.neighborRadius        : 3f,
            MinSeparationDistance = settings != null ? settings.minSeparationDistance : 0.8f,
            ArrivalRadius         = settings != null ? settings.arrivalRadius         : 1f,
            SeparationWeight      = settings != null ? settings.separationWeight      : 1.5f,
            CohesionWeight        = settings != null ? settings.cohesionWeight        : 1f,
            FollowWeight          = settings != null ? settings.followWeight          : 2f,
            OutDirections         = outDirections,
        };

        // innerloopBatchCount: 8 (멤버 수에 따라 4~16 튜닝)
        return job.Schedule(count, 8);
    }

    /// <summary>
    /// Phase 3: Job 완료 대기 → 결과를 멤버에 적용 → Avoidance 후처리 (메인 스레드).
    /// </summary>
    public void Complete(
        JobHandle                  handle,
        IReadOnlyList<SquadMember> members,
        ObstacleGrid               obstacleGrid)
    {
        handle.Complete();

        int count = members.Count;
        for (int i = 0; i < count; i++)
        {
            var dir = outDirections[i];

            // Avoidance 후처리 — ObstacleGrid는 관리 코드이므로 메인 스레드에서 처리
            if (obstacleGrid != null && dir != Vector2.zero)
            {
                Vector2 pos = members[i].Transform.position;
                dir = new Vector2(
                    obstacleGrid.IsWalkable(new Vector2(pos.x + Mathf.Sign(dir.x) * 0.5f, pos.y)) ? dir.x : 0f,
                    obstacleGrid.IsWalkable(new Vector2(pos.x, pos.y + Mathf.Sign(dir.y) * 0.5f)) ? dir.y : 0f
                );
            }

            members[i].SetMoveDirection(dir);
        }
    }

    public void Dispose()
    {
        if (positions.IsCreated)     positions.Dispose();
        if (stoppedFlags.IsCreated)  stoppedFlags.Dispose();
        if (outDirections.IsCreated) outDirections.Dispose();
    }
}
