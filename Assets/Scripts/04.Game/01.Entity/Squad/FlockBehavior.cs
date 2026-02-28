using System.Collections.Generic;
using UnityEngine;

public class FlockBehavior
{
    public float AlignmentWeight = 1f;
    public float CohesionWeight = 1f;
    public float SeparationWeight = 1.5f;
    public float FollowWeight = 2f;
    public float AvoidanceWeight = 2f;
    public float NeighborRadius = 3f;

    public Vector2 CalculateDirection(
        SquadMember self,
        IReadOnlyList<SquadMember> neighbors,
        Transform leader,
        ObstacleGrid obstacleGrid)
    {
        List<SquadMember> others = new List<SquadMember>();
        foreach (SquadMember neighbor in neighbors)
        {
            if (neighbor != self)
            {
                others.Add(neighbor);
            }
        }

        Vector2 alignment = CalculateAlignment(self, others) * AlignmentWeight;
        Vector2 cohesion = CalculateCohesion(self, others) * CohesionWeight;
        Vector2 separation = CalculateSeparation(self, others) * SeparationWeight;
        Vector2 follow = CalculateFollow(self, leader) * FollowWeight;
        Vector2 avoidance = CalculateAvoidance(self, obstacleGrid) * AvoidanceWeight;

        Vector2 combined = alignment + cohesion + separation + follow + avoidance;

        if (combined == Vector2.zero)
        {
            return Vector2.zero;
        }

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
        {
            return Vector2.zero;
        }

        Vector2 centerSum = Vector2.zero;
        foreach (SquadMember neighbor in neighbors)
        {
            centerSum += (Vector2)neighbor.Transform.position;
        }

        Vector2 center = centerSum / neighbors.Count;
        Vector2 toCenter = center - (Vector2)self.Transform.position;

        if (toCenter == Vector2.zero)
        {
            return Vector2.zero;
        }

        return toCenter.normalized;
    }

    private Vector2 CalculateSeparation(SquadMember self, List<SquadMember> neighbors)
    {
        if (neighbors.Count == 0)
        {
            return Vector2.zero;
        }

        Vector2 selfPos = self.Transform.position;
        Vector2 separationSum = Vector2.zero;

        foreach (SquadMember neighbor in neighbors)
        {
            Vector2 diff = selfPos - (Vector2)neighbor.Transform.position;
            float distance = diff.magnitude;

            if (distance > 0f)
            {
                separationSum += diff / (distance * distance);
            }
        }

        if (separationSum == Vector2.zero)
        {
            return Vector2.zero;
        }

        return separationSum.normalized;
    }

    private Vector2 CalculateFollow(SquadMember self, Transform leader)
    {
        Vector2 toLeader = (Vector2)leader.position - (Vector2)self.Transform.position;

        if (toLeader == Vector2.zero)
        {
            return Vector2.zero;
        }

        return toLeader.normalized;
    }

    private Vector2 CalculateAvoidance(SquadMember self, ObstacleGrid obstacleGrid)
    {
        Vector2 selfPos = self.Transform.position;
        Vector2 avoidance = Vector2.zero;

        Vector2[] directions = new Vector2[]
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right
        };

        foreach (Vector2 dir in directions)
        {
            Vector2 checkPos = selfPos + dir;
            if (!obstacleGrid.IsWalkable(checkPos))
            {
                avoidance -= dir;
            }
        }

        if (avoidance == Vector2.zero)
        {
            return Vector2.zero;
        }

        return avoidance.normalized;
    }
}
