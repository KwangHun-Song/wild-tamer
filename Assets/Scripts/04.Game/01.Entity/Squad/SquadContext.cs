using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FlockBehavior 계산에 필요한 스쿼드 공유 컨텍스트.
/// Squad와 MonsterSquad가 매 프레임 생성해 FlockBehavior에 전달한다.
/// MemberPositions 배열에 위치를 사전 캐싱해 CollectNeighbors의 Transform 접근을 제거한다.
/// </summary>
public readonly struct SquadContext
{
    public readonly IReadOnlyList<IUnit> Members;
    /// <summary>Members[i].Transform.position을 미리 읽어 저장한 배열. 길이 >= Members.Count.</summary>
    public readonly Vector2[] MemberPositions;
    public readonly Transform LeaderTransform;
    public readonly ObstacleGrid ObstacleGrid;

    public SquadContext(IReadOnlyList<IUnit> members, Vector2[] memberPositions, Transform leaderTransform, ObstacleGrid obstacleGrid)
    {
        Members = members;
        MemberPositions = memberPositions;
        LeaderTransform = leaderTransform;
        ObstacleGrid = obstacleGrid;
    }
}
