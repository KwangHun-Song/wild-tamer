using System;
using System.Collections.Generic;
using Base;
using UnityEngine;

/// <summary>
/// 유저 진행 데이터 컨테이너.
/// Facade.Data를 통해 직접 저장·로드한다.
/// </summary>
[Serializable]
public class UserData
{
    private const string SaveKey = "user_data";

    /// <summary>한 번이라도 테이밍(또는 초기 지급)된 몬스터의 ScriptableObject 에셋명 목록.</summary>
    public List<string> tamedMonsterIds = new();

    public static UserData Load()
    {
        return Facade.Data.Load<UserData>(SaveKey, null) ?? new UserData();
    }

    /// <summary>중복 id는 무시하고 저장한다.</summary>
    public static void AddTamedMonster(string monsterAssetName)
    {
        var data = Load();
        if (data.tamedMonsterIds.Contains(monsterAssetName)) return;
        data.tamedMonsterIds.Add(monsterAssetName);
        Facade.Data.Save(SaveKey, data);
        Debug.Log($"[UserData] 테이밍 기록 추가: {monsterAssetName}");
    }
}
