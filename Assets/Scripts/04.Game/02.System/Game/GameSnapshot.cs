using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 상태를 스냅샷으로 저장한다. 플레이어, 부대원, 몬스터, 안개 상태를 기록한다.
/// </summary>
public class GameSnapshot
{
    public Vector2 PlayerPosition { get; }
    public List<SquadMemberSnapshot> SquadMembers { get; }
    public List<MonsterSnapshot> Monsters { get; }
    public FogState[,] FogGrid { get; }

    public GameSnapshot(
        Vector2 playerPosition,
        List<SquadMemberSnapshot> squadMembers,
        List<MonsterSnapshot> monsters,
        FogState[,] fogGrid)
    {
        PlayerPosition = playerPosition;
        SquadMembers = squadMembers;
        Monsters = monsters;
        FogGrid = fogGrid;
    }
}

/// <summary>
/// 부대원 개인의 스냅샷. 몬스터 데이터, 플레이어 기준 상대 위치, 현재 HP를 저장한다.
/// </summary>
public class SquadMemberSnapshot
{
    public MonsterData Data { get; }
    public Vector2 PositionOffset { get; }
    public int CurrentHp { get; }

    public SquadMemberSnapshot(SquadMember member, Vector2 playerPos)
    {
        Data = member.Data;
        PositionOffset = (Vector2)member.Transform.position - playerPos;
        CurrentHp = member.Health.CurrentHp;
    }
}

/// <summary>
/// 몬스터 개인의 스냅샷. 몬스터 데이터, 절대 위치, 현재 HP를 저장한다.
/// </summary>
public class MonsterSnapshot
{
    public MonsterData Data { get; }
    public Vector2 Position { get; }
    public int CurrentHp { get; }

    public MonsterSnapshot(Monster monster)
    {
        Data = monster.Data;
        Position = (Vector2)monster.Transform.position;
        CurrentHp = monster.Health.CurrentHp;
    }
}
