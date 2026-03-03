using System;
using Base;
using UnityEngine;

/// <summary>
/// 게임 상태의 저장·로드·삭제 진입점.
/// Facade.Data(PlayerPrefs + JSON)를 통해 GameSaveData를 직렬화한다.
/// Load()는 1회용 Resume 세이브이므로 로드 즉시 삭제한다.
/// </summary>
public static class GameSaveManager
{
    private const string SaveKey = "game_save";

    /// <summary>세팅팝업 저장 버튼 클릭 시 발생. InPlayState가 구독하여 TrySave()를 호출한다.</summary>
    public static event Action OnSaveRequested;

    public static void RequestSave() => OnSaveRequested?.Invoke();

    public static bool HasSave() => Facade.Data.HasKey(SaveKey);

    public static void Save(GameSaveData data)
    {
        Facade.Data.Save(SaveKey, data);
        Debug.Log("[GameSaveManager] 게임 상태 저장 완료.");
    }

    /// <summary>로드 후 즉시 삭제. 저장 데이터가 없으면 null 반환.</summary>
    public static GameSaveData Load()
    {
        var data = Facade.Data.Load<GameSaveData>(SaveKey, null);
        Facade.Data.Delete(SaveKey);
        if (data != null)
            Debug.Log("[GameSaveManager] 저장 데이터 로드 완료.");
        return data;
    }

    public static void Delete()
    {
        Facade.Data.Delete(SaveKey);
        Debug.Log("[GameSaveManager] 저장 데이터 삭제.");
    }
}
