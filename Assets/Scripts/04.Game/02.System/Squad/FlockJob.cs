using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

/// <summary>
/// FlockBehavior의 Separation + Cohesion + Follow 계산을 워커 스레드에서 병렬로 수행한다.
/// Avoidance(ObstacleGrid)는 관리 코드 의존으로 FlockJobRunner.Complete()에서 메인 스레드 후처리.
/// </summary>
[BurstCompile]
public struct FlockJob : IJobParallelFor
{
    // ---- 입력 (ReadOnly) ----
    [ReadOnly] public NativeArray<Vector2> Positions;
    [ReadOnly] public NativeArray<bool>    StoppedFlags;
    [ReadOnly] public Vector2 LeaderPos;

    [ReadOnly] public float NeighborRadius;
    [ReadOnly] public float MinSeparationDistance;
    [ReadOnly] public float ArrivalRadius;
    [ReadOnly] public float SeparationWeight;
    [ReadOnly] public float CohesionWeight;
    [ReadOnly] public float FollowWeight;

    // ---- 출력 ----
    [WriteOnly] public NativeArray<Vector2> OutDirections;

    public void Execute(int index)
    {
        if (StoppedFlags[index])
        {
            OutDirections[index] = Vector2.zero;
            return;
        }

        Vector2 selfPos = Positions[index];
        float sqrRadius = NeighborRadius * NeighborRadius;
        float sqrMinSep = MinSeparationDistance * MinSeparationDistance;

        Vector2 separation  = Vector2.zero;
        Vector2 cohesionSum = Vector2.zero;
        int     cohesionCnt = 0;

        // 이웃 순회 — Burst SIMD 벡터화로 200마리 브루트포스 충분
        int count = Positions.Length;
        for (int i = 0; i < count; i++)
        {
            if (i == index) continue;

            Vector2 np  = Positions[i];
            float   dx  = selfPos.x - np.x;
            float   dy  = selfPos.y - np.y;
            float   sqr = dx * dx + dy * dy;

            if (sqr > sqrRadius) continue;

            // Separation — 역제곱 법칙 (sqrt 없음)
            if (sqr > 0f && sqr < sqrMinSep)
            {
                float scale = (sqrMinSep - sqr) / (sqr * sqrMinSep);
                separation.x += dx * scale;
                separation.y += dy * scale;
            }

            // Cohesion — 최소 거리 밖 이웃만 포함
            if (sqr >= sqrMinSep)
            {
                cohesionSum.x += np.x;
                cohesionSum.y += np.y;
                cohesionCnt++;
            }
        }

        // Cohesion 정규화
        Vector2 cohesion = Vector2.zero;
        if (cohesionCnt > 0)
        {
            float cx  = cohesionSum.x / cohesionCnt - selfPos.x;
            float cy  = cohesionSum.y / cohesionCnt - selfPos.y;
            float mag = cx * cx + cy * cy;
            if (mag > 1f)
            {
                float inv = 1f / Mathf.Sqrt(mag);
                cx *= inv; cy *= inv;
            }
            cohesion = new Vector2(cx, cy);
        }

        // Follow — ArrivalRadius 이내 감속
        float lx   = LeaderPos.x - selfPos.x;
        float ly   = LeaderPos.y - selfPos.y;
        float lSqr = lx * lx + ly * ly;
        float arrSqr = ArrivalRadius * ArrivalRadius;
        Vector2 follow = Vector2.zero;
        if (lSqr >= arrSqr)
        {
            float dist = Mathf.Sqrt(lSqr);
            if (dist > ArrivalRadius * 2f)
            {
                follow = new Vector2(lx, ly);
            }
            else
            {
                float t = (dist - ArrivalRadius) / ArrivalRadius;
                follow = new Vector2(lx * t, ly * t);
            }
        }

        // 합산
        Vector2 combined = new Vector2(
            separation.x * SeparationWeight + cohesion.x * CohesionWeight + follow.x * FollowWeight,
            separation.y * SeparationWeight + cohesion.y * CohesionWeight + follow.y * FollowWeight
        );

        // 임계값 클램핑
        if (Mathf.Abs(combined.x) < 0.2f) combined.x = 0f;
        if (Mathf.Abs(combined.y) < 0.2f) combined.y = 0f;

        // 정규화
        float cmag = combined.x * combined.x + combined.y * combined.y;
        if (cmag > 1f)
        {
            float inv = 1f / Mathf.Sqrt(cmag);
            combined.x *= inv;
            combined.y *= inv;
        }

        OutDirections[index] = combined;
    }
}
