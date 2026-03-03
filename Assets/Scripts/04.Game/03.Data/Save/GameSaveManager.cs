using Base;
using UnityEngine;

/// <summary>
/// 게임 상태의 저장·로드·삭제 진입점.
/// Facade.DataStore(PlayerPrefs + JSON)를 통해 GameSaveData를 직렬화한다.
/// Load()는 1회용 Resume 세이브이므로 로드 즉시 삭제한다.
/// </summary>
public static class GameSaveManager
{
    private const string SaveKey = "game_save";

    public static bool HasSave() => Facade.DataStore.HasKey(SaveKey);

    public static void Save(GameSaveData data)
    {
        Facade.DataStore.Save(SaveKey, data);
        Debug.Log("[GameSaveManager] 게임 상태 저장 완료.");
    }

    /// <summary>로드 후 즉시 삭제. 저장 데이터가 없으면 null 반환.</summary>
    public static GameSaveData Load()
    {
        var data = Facade.DataStore.Load<GameSaveData>(SaveKey, null);
        Facade.DataStore.Delete(SaveKey);
        if (data != null)
            Debug.Log("[GameSaveManager] 저장 데이터 로드 완료.");
        return data;
    }

    public static void Delete()
    {
        Facade.DataStore.Delete(SaveKey);
        Debug.Log("[GameSaveManager] 저장 데이터 삭제.");
    }
}
