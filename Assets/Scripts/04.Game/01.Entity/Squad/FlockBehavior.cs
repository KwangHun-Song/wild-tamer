using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 군집 행동(Flocking) 방향을 계산한다.
/// Alignment · Cohesion · Separation · Follow · Avoidance 다섯 힘의 합산으로 이동 방향을 결정한다.
/// </summary>
public class FlockBehavior
{
    public float AlignmentWeight = 1f;
    public float CohesionWeight = 1f;
    public float SeparationWeight = 1.5f;
    public float FollowWeight = 2f;
    public float AvoidanceWeight = 2f;
    public float NeighborRadius = 3f;
    public float ArrivalRadius = 1f;           // Follow 감속 반경 — 이내에서 거리에 비례해 Follow 약화
    public float MinSeparationDistance = 0.8f; // 캐릭터 간 최소 유지 거리

    private readonly List<IUnit> neighborsCache = new();

    /// <summary>
    /// 각 힘을 가중합산하고 정규화한 최종 이동 방향을 반환한다.
    /// </summary>
    public Vector2 CalculateDirection(
        IUnit self,
        IEnumerable<IUnit> neighbors,
        Transform leader,
        ObstacleGrid obstacleGrid)
    {
        // NeighborRadius 이내의 이웃만 수집
        neighborsCache.Clear();
        var selfPos2D = (Vector2)self.Transform.position;
        foreach (var neighbor in neighbors)
        {
            if (neighbor == self) continue;
            float dist = Vector2.Distance(selfPos2D, (Vector2)neighbor.Transform.position);
            if (dist <= NeighborRadius)
                neighborsCache.Add(neighbor);
        }

        var alignment = CalculateAlignment(self, neighborsCache) * AlignmentWeight;
        var cohesion = CalculateCohesion(self, neighborsCache) * CohesionWeight;
        var separation = CalculateSeparation(self, neighborsCache) * SeparationWeight;
        var follow = CalculateFollow(self, leader) * FollowWeight;
        var avoidance = CalculateAvoidance(self, obstacleGrid) * AvoidanceWeight;

        var combined = alignment + cohesion + separation + follow + avoidance;

        // x, y값을 각각 오프셋 미만이면 0으로 치환
        var offsetMin = 0.2F;
        combined.x = Mathf.Abs(combined.x) < offsetMin ? 0F : combined.x;
        combined.y = Mathf.Abs(combined.y) < offsetMin ? 0F : combined.y;

        if (combined == Vector2.zero)
            return Vector2.zero;

        if (combined.magnitude > 1F)
            return combined.normalized;

        return combined;
    }

    /// <summary>
    /// 이웃의 평균 이동 방향에 맞추려는 힘. (현재 미구현 — MoveDirection 노출 시 확장)
    /// </summary>
    private Vector2 CalculateAlignment(IUnit self, List<IUnit> neighbors)
    {
        return Vector2.zero;
    }

    /// <summary>
    /// 이웃의 무게중심 방향으로 모이려는 힘.
    /// </summary>
    private Vector2 CalculateCohesion(IUnit self, List<IUnit> neighbors)
    {
        if (neighbors.Count == 0)
            return Vector2.zero;

        Vector2 selfPos = self.Transform.position;
        var centerSum = Vector2.zero;
        int count = 0;

        foreach (var neighbor in neighbors)
        {
            // 최소 유지 거리 이내의 이웃은 이미 충분히 가까우므로 응집 계산에서 제외한다.
            float dist = Vector2.Distance(selfPos, (Vector2)neighbor.Transform.position);
            if (dist < MinSeparationDistance) continue;

            centerSum += (Vector2)neighbor.Transform.position;
            count++;
        }

        if (count == 0)
            return Vector2.zero;

        var center = centerSum / count;
        var toCenter = center - selfPos;

        if (toCenter == Vector2.zero)
            return Vector2.zero;

        if (toCenter.magnitude > 1F)
            return toCenter.normalized;

        return toCenter;
    }

    /// <summary>
    /// 이웃과 최소 거리(MinSeparationDistance)를 유지하려는 힘.
    /// 거리가 가까울수록 반발력이 강해지는 선형 모델.
    /// </summary>
    private Vector2 CalculateSeparation(IUnit self, List<IUnit> neighbors)
    {
        if (neighbors.Count == 0)
            return Vector2.zero;

        Vector2 selfPos = self.Transform.position;
        var separationSum = Vector2.zero;

        foreach (var neighbor in neighbors)
        {
            var diff = selfPos - (Vector2)neighbor.Transform.position;
            float distance = diff.magnitude;

            // 최소 거리 이내일 때만 선형 반발력 적용 (distance=0에서 최대, MinSeparationDistance에서 0)
            if (distance > 0f && distance < MinSeparationDistance)
                separationSum += diff.normalized * (MinSeparationDistance - distance) / MinSeparationDistance;
        }

        if (separationSum == Vector2.zero)
            return Vector2.zero;

        if (separationSum.magnitude > 1F)
            return separationSum.normalized;

        return separationSum;
    }

    /// <summary>
    /// 리더(플레이어)를 따라가려는 힘.
    /// ArrivalRadius 이내에서는 거리에 비례해 힘을 감소시켜 Separation과 자연스러운 평형점을 형성한다.
    /// (단절 방식은 ArrivalRadius 경계에서 진동을 유발하므로 선형 감소 방식을 사용)
    /// </summary>
    private Vector2 CalculateFollow(IUnit self, Transform leader)
    {
        var toLeader = (Vector2)leader.position - (Vector2)self.Transform.position;
        float dist = toLeader.magnitude;

        // 충분히 가까워졌으면 더 이상 리더에 가까이 가지 않는다.
        if (dist < ArrivalRadius)
            return Vector2.zero;

        // 멀면 충분한 힘으로 리더를 따라가려고 한다.
        if (dist > ArrivalRadius * 2)
            return toLeader;

        // 중간 거리에서는 보간 사용
        return Vector2.Lerp(Vector2.zero, toLeader, (dist - ArrivalRadius) / ArrivalRadius);
    }

    /// <summary>
    /// 4방향 인접 셀을 검사해 장애물을 피하려는 힘.
    /// </summary>
    private Vector2 CalculateAvoidance(IUnit self, ObstacleGrid obstacleGrid)
    {
        Vector2 selfPos = self.Transform.position;
        var avoidance = Vector2.zero;

        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        foreach (var dir in directions)
        {
            if (!obstacleGrid.IsWalkable(selfPos + dir))
                avoidance -= dir;
        }

        if (avoidance == Vector2.zero)
            return Vector2.zero;

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
    /// </summary>
    public FlockDebugData ComputeDebugData(
        IUnit self,
        IEnumerable<IUnit> neighbors,
        Transform leader,
        ObstacleGrid obstacleGrid)
    {
        neighborsCache.Clear();
        var selfPos2D = (Vector2)self.Transform.position;
        foreach (var neighbor in neighbors)
        {
            if (neighbor == self) continue;
            float dist = Vector2.Distance(selfPos2D, (Vector2)neighbor.Transform.position);
            if (dist <= NeighborRadius)
                neighborsCache.Add(neighbor);
        }

        var cohesion = CalculateCohesion(self, neighborsCache) * CohesionWeight;
        var separation = CalculateSeparation(self, neighborsCache) * SeparationWeight;
        var follow = CalculateFollow(self, leader) * FollowWeight;
        var avoidance = CalculateAvoidance(self, obstacleGrid) * AvoidanceWeight;
        var alignment = CalculateAlignment(self, neighborsCache) * AlignmentWeight;
        var combined = alignment + cohesion + separation + follow + avoidance;

        combined.x = Mathf.Abs(combined.x) < 0.1F ? 0F : combined.x;
        combined.y = Mathf.Abs(combined.y) < 0.1F ? 0F : combined.y;

        return new FlockDebugData
        {
            Cohesion = cohesion,
            Separation = separation,
            Follow = follow,
            Avoidance = avoidance,
            Combined = combined == Vector2.zero ? Vector2.zero : combined.normalized,
        };
    }
#endif
}
