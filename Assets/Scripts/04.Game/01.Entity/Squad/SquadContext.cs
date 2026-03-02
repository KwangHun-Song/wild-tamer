using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FlockBehavior 계산에 필요한 스쿼드 공유 컨텍스트.
/// Squad와 MonsterSquad가 매 프레임 생성해 FlockBehavior에 전달한다.
/// </summary>
public readonly struct SquadContext
{
    public readonly IEnumerable<IUnit> Members;
    public readonly Transform LeaderTransform;
    public readonly ObstacleGrid ObstacleGrid;

    public SquadContext(IEnumerable<IUnit> members, Transform leaderTransform, ObstacleGrid obstacleGrid)
    {
        Members = members;
        LeaderTransform = leaderTransform;
        ObstacleGrid = obstacleGrid;
    }
}
