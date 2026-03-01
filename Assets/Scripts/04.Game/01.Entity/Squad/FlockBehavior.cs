using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 군집 행동(Flocking) 방향을 계산한다.
/// Alignment · Cohesion · Separation · Follow · Avoidance 다섯 힘의 합산으로 이동 방향을 결정한다.
/// </summary>
public class FlockBehavior
{
    public float AlignmentWeight = 1f;
    public float CohesionWeight = 1f;
    public float SeparationWeight = 2f;
    public float FollowWeight = 2f;
    public float AvoidanceWeight = 2f;
    public float NeighborRadius = 3f;
    public float ArrivalRadius = 1f;           // 리더 도달 판정 반경 — 이내에선 Follow 없음
    public float MinSeparationDistance = 0.5f; // 캐릭터 간 최소 유지 거리

    /// <summary>
    /// 각 힘을 가중합산하고 정규화한 최종 이동 방향을 반환한다.
    /// </summary>
    public Vector2 CalculateDirection(
        SquadMember self,
        IReadOnlyList<SquadMember> neighbors,
        Transform leader,
        ObstacleGrid obstacleGrid)
    {
        // NeighborRadius 이내의 이웃만 수집
        var others = new List<SquadMember>();
        var selfPos2D = (Vector2)self.Transform.position;
        foreach (var neighbor in neighbors)
        {
            if (neighbor == self) continue;
            float dist = Vector2.Distance(selfPos2D, (Vector2)neighbor.Transform.position);
            if (dist <= NeighborRadius)
                others.Add(neighbor);
        }

        var alignment  = CalculateAlignment(self, others) * AlignmentWeight;
        var cohesion   = CalculateCohesion(self, others) * CohesionWeight;
        var separation = CalculateSeparation(self, others) * SeparationWeight;
        var follow     = CalculateFollow(self, leader) * FollowWeight;
        var avoidance  = CalculateAvoidance(self, obstacleGrid) * AvoidanceWeight;

        var combined = alignment + cohesion + separation + follow + avoidance;

        if (combined == Vector2.zero)
            return Vector2.zero;

        return combined.normalized;
    }

    /// <summary>
    /// 이웃의 평균 이동 방향에 맞추려는 힘. (현재 미구현 — MoveDirection 노출 시 확장)
    /// </summary>
    private Vector2 CalculateAlignment(SquadMember self, List<SquadMember> neighbors)
    {
        return Vector2.zero;
    }

    /// <summary>
    /// 이웃의 무게중심 방향으로 모이려는 힘.
    /// </summary>
    private Vector2 CalculateCohesion(SquadMember self, List<SquadMember> neighbors)
    {
        if (neighbors.Count == 0)
            return Vector2.zero;

        var centerSum = Vector2.zero;
        foreach (var neighbor in neighbors)
            centerSum += (Vector2)neighbor.Transform.position;

        var center   = centerSum / neighbors.Count;
        var toCenter = center - (Vector2)self.Transform.position;

        if (toCenter == Vector2.zero)
            return Vector2.zero;

        return toCenter.normalized;
    }

    /// <summary>
    /// 이웃과 최소 거리(MinSeparationDistance)를 유지하려는 힘.
    /// 거리가 가까울수록 반발력이 강해지는 선형 모델.
    /// </summary>
    private Vector2 CalculateSeparation(SquadMember self, List<SquadMember> neighbors)
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

        return separationSum.normalized;
    }

    /// <summary>
    /// 리더(플레이어)를 따라가려는 힘.
    /// ArrivalRadius 이내에 도달하면 힘을 제거해 목적지 근처 떨림을 방지한다.
    /// </summary>
    private Vector2 CalculateFollow(SquadMember self, Transform leader)
    {
        var toLeader = (Vector2)leader.position - (Vector2)self.Transform.position;

        if (toLeader.sqrMagnitude < ArrivalRadius * ArrivalRadius)
            return Vector2.zero;

        return toLeader.normalized;
    }

    /// <summary>
    /// 4방향 인접 셀을 검사해 장애물을 피하려는 힘.
    /// </summary>
    private Vector2 CalculateAvoidance(SquadMember self, ObstacleGrid obstacleGrid)
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
}
