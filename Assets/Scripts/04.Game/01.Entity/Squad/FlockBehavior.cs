using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 군집 행동(Flocking) 방향을 계산한다.
/// Alignment · Cohesion · Separation · Follow · Avoidance 다섯 힘의 합산으로 이동 방향을 결정한다.
/// </summary>
public class FlockBehavior
{
    public float AlignmentWeight      = 1f;
    public float CohesionWeight       = 1f;
    public float SeparationWeight     = 1.5f;
    public float FollowWeight         = 2f;
    public float AvoidanceWeight      = 2f;
    public float NeighborRadius       = 3f;
    public float ArrivalRadius        = 1f;           // Follow 감속 반경 — 이내에서 거리에 비례해 Follow 약화
    public float MinSeparationDistance = 0.8f;        // 캐릭터 간 최소 유지 거리

    private readonly List<IUnit>   neighborsCache   = new();
    /// <summary>이웃 위치를 사전 캐싱한다. CollectNeighbors()에서 Transform.position을 1회만 읽어 저장.</summary>
    private readonly List<Vector2> neighborPosCache = new();

    // 장애물 회피 방향 — 매 호출마다 new[] 생성하지 않도록 static 캐싱
    private static readonly Vector2[] AvoidDirections =
        { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

    public FlockBehavior() { }

    /// <summary>ScriptableObject 설정으로 가중치와 반경을 초기화한다. null이면 기본값을 유지한다.</summary>
    public FlockBehavior(FlockSettingsData settings)
    {
        if (settings == null) return;
        AlignmentWeight       = settings.alignmentWeight;
        CohesionWeight        = settings.cohesionWeight;
        SeparationWeight      = settings.separationWeight;
        FollowWeight          = settings.followWeight;
        AvoidanceWeight       = settings.avoidanceWeight;
        NeighborRadius        = settings.neighborRadius;
        ArrivalRadius         = settings.arrivalRadius;
        MinSeparationDistance = settings.minSeparationDistance;
    }

    /// <summary>
    /// 각 힘을 가중합산하고 정규화한 최종 이동 방향을 반환한다.
    /// </summary>
    public Vector2 CalculateDirection(IUnit self, in SquadContext context)
    {
        CollectNeighbors(self, context);

        var selfPos = (Vector2)self.Transform.position;

        var combined = CalculateSeparation(selfPos) * SeparationWeight
                     + CalculateCohesion(selfPos)   * CohesionWeight
                     // Alignment 미구현 (항상 zero) — 구현 시 여기에 추가
                     + CalculateFollow(self, context.LeaderTransform)   * FollowWeight
                     + CalculateAvoidance(self, context.ObstacleGrid)   * AvoidanceWeight;

        // x, y값을 오프셋 미만이면 0으로 치환
        combined.x = Mathf.Abs(combined.x) < 0.2f ? 0f : combined.x;
        combined.y = Mathf.Abs(combined.y) < 0.2f ? 0f : combined.y;

        if (combined == Vector2.zero) return Vector2.zero;
        if (combined.sqrMagnitude > 1f) return combined.normalized;
        return combined;
    }

    /// <summary>
    /// NeighborRadius 이내 이웃을 수집하고 위치를 neighborPosCache에 사전 저장한다.
    /// sqrMagnitude 비교로 sqrt를 회피한다.
    /// </summary>
    private void CollectNeighbors(IUnit self, in SquadContext context)
    {
        neighborsCache.Clear();
        neighborPosCache.Clear();

        var selfPos       = (Vector2)self.Transform.position;
        float sqrRadius   = NeighborRadius * NeighborRadius;

        foreach (var neighbor in context.Members)
        {
            if (neighbor == self) continue;
            var neighborPos = (Vector2)neighbor.Transform.position; // Transform 1회만 읽음
            if ((neighborPos - selfPos).sqrMagnitude <= sqrRadius)
            {
                neighborsCache.Add(neighbor);
                neighborPosCache.Add(neighborPos);
            }
        }
    }

    /// <summary>
    /// 이웃과 최소 거리(MinSeparationDistance)를 유지하려는 힘.
    /// sqrMagnitude 사전 필터링 후 sqrt를 1회만 실행 (기존 2회 → 1회).
    /// </summary>
    private Vector2 CalculateSeparation(Vector2 selfPos)
    {
        var separationSum = Vector2.zero;
        float sqrMinSep   = MinSeparationDistance * MinSeparationDistance;

        for (int i = 0; i < neighborPosCache.Count; i++)
        {
            var diff      = selfPos - neighborPosCache[i];
            float sqrDist = diff.sqrMagnitude;

            if (sqrDist > 0f && sqrDist < sqrMinSep)
            {
                float distance = Mathf.Sqrt(sqrDist);                                           // sqrt 1회
                separationSum += (diff / distance) * ((MinSeparationDistance - distance) / MinSeparationDistance);
            }
        }

        if (separationSum == Vector2.zero) return Vector2.zero;
        if (separationSum.sqrMagnitude > 1f) return separationSum.normalized;
        return separationSum;
    }

    /// <summary>
    /// 이웃의 무게중심 방향으로 모이려는 힘.
    /// neighborPosCache를 사용해 Transform 재접근 없이 처리한다.
    /// </summary>
    private Vector2 CalculateCohesion(Vector2 selfPos)
    {
        if (neighborPosCache.Count == 0) return Vector2.zero;

        var centerSum   = Vector2.zero;
        int count       = 0;
        float sqrMinSep = MinSeparationDistance * MinSeparationDistance;

        for (int i = 0; i < neighborPosCache.Count; i++)
        {
            var np = neighborPosCache[i];
            // 최소 유지 거리 이내는 응집 계산에서 제외 — sqrMagnitude로 sqrt 회피
            if ((np - selfPos).sqrMagnitude < sqrMinSep) continue;
            centerSum += np;
            count++;
        }

        if (count == 0) return Vector2.zero;

        var toCenter = centerSum / count - selfPos;
        if (toCenter == Vector2.zero) return Vector2.zero;
        if (toCenter.sqrMagnitude > 1f) return toCenter.normalized;
        return toCenter;
    }

    /// <summary>
    /// 리더(플레이어)를 따라가려는 힘.
    /// ArrivalRadius 이내에서는 거리에 비례해 힘을 감소시켜 Separation과 자연스러운 평형점을 형성한다.
    /// </summary>
    private Vector2 CalculateFollow(IUnit self, Transform leader)
    {
        var toLeader = (Vector2)leader.position - (Vector2)self.Transform.position;
        float dist   = toLeader.magnitude;

        if (dist < ArrivalRadius) return Vector2.zero;
        if (dist > ArrivalRadius * 2f) return toLeader;

        return Vector2.Lerp(Vector2.zero, toLeader, (dist - ArrivalRadius) / ArrivalRadius);
    }

    /// <summary>
    /// 4방향 인접 셀을 검사해 장애물을 피하려는 힘.
    /// </summary>
    private Vector2 CalculateAvoidance(IUnit self, ObstacleGrid obstacleGrid)
    {
        Vector2 selfPos  = self.Transform.position;
        var avoidance    = Vector2.zero;

        foreach (var dir in AvoidDirections)
        {
            if (!obstacleGrid.IsWalkable(selfPos + dir))
                avoidance -= dir;
        }

        if (avoidance == Vector2.zero) return Vector2.zero;
        return avoidance.normalized;
    }

#if UNITY_EDITOR
    public struct FlockDebugData
    {
        public Vector2 Cohesion;
        public Vector2 Separation;
        public Vector2 Follow;
        public Vector2 Avoidance;
        public Vector2 Combined;
    }

    /// <summary>
    /// 기즈모 시각화용 디버그 데이터를 계산한다. (에디터 전용)
    /// CollectNeighbors()를 공유해 CalculateDirection과 중복 계산을 방지한다.
    /// </summary>
    public FlockDebugData ComputeDebugData(IUnit self, in SquadContext context)
    {
        CollectNeighbors(self, context);

        var selfPos    = (Vector2)self.Transform.position;
        var cohesion   = CalculateCohesion(selfPos)   * CohesionWeight;
        var separation = CalculateSeparation(selfPos)  * SeparationWeight;
        var follow     = CalculateFollow(self, context.LeaderTransform)  * FollowWeight;
        var avoidance  = CalculateAvoidance(self, context.ObstacleGrid)  * AvoidanceWeight;
        var combined   = cohesion + separation + follow + avoidance;

        combined.x = Mathf.Abs(combined.x) < 0.1f ? 0f : combined.x;
        combined.y = Mathf.Abs(combined.y) < 0.1f ? 0f : combined.y;

        return new FlockDebugData
        {
            Cohesion   = cohesion,
            Separation = separation,
            Follow     = follow,
            Avoidance  = avoidance,
            Combined   = combined == Vector2.zero ? Vector2.zero : combined.normalized,
        };
    }
#endif
}
