using System;

[Serializable]
public class GameSaveData
{
    public float   playerPosX;
    public float   playerPosY;
    public int     playerHp;

    public SquadMemberSaveData[] squadMembers;

    public float   bossElapsedTime;
    public float   bossRespawnTimer;

    public FogSaveData fog;
}

[Serializable]
public class SquadMemberSaveData
{
    public string monsterId;
    public float  offsetX;
    public float  offsetY;
    public int    currentHp;
}

[Serializable]
public class FogSaveData
{
    public int   width;
    public int   height;
    public int[] exploredIndices;
}
