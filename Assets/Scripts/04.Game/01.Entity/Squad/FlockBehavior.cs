using System.Collections.Generic;
using UnityEngine;

public class FlockBehavior
{
    public float AlignmentWeight = 1f;
    public float CohesionWeight = 1f;
    public float SeparationWeight = 2f;
    public float FollowWeight = 2f;
    public float AvoidanceWeight = 2f;
    public float NeighborRadius = 3f;
    public float ArrivalRadius = 2f;           // 리더 도달 판정 반경 — 이내에선 Follow 없음
    public float MinSeparationDistance = 0.8f; // 캐릭터 간 최소 유지 거리

    public Vector2 CalculateDirection(
        SquadMember self,
        IReadOnlyList<SquadMember> neighbors,
        Transform leader,
        ObstacleGrid obstacleGrid)
    {
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

    private Vector2 CalculateAlignment(SquadMember self, List<SquadMember> neighbors)
    {
        // UnitMovement에 MoveDirection이 없어 현재는 zero 반환 — 향후 확장 시 구현
        return Vector2.zero;
    }

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

    private Vector2 CalculateFollow(SquadMember self, Transform leader)
    {
        var toLeader = (Vector2)leader.position - (Vector2)self.Transform.position;

        // 도달 반경 이내에선 Follow 없음 — 떨림 방지
        if (toLeader.sqrMagnitude < ArrivalRadius * ArrivalRadius)
            return Vector2.zero;

        return toLeader.normalized;
    }

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
