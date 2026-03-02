using UnityEngine;

/// <summary>
/// 몬스터 스쿼드 스폰 설정을 저장하는 ScriptableObject.
/// Facade.DB.Get&lt;SpawnSettingsData&gt;("SpawnSettings")으로 조회한다.
/// </summary>
[CreateAssetMenu(menuName = "Data/SpawnSettings")]
public class SpawnSettingsData : ScriptableObject
{
    [Header("스쿼드 수 제한")]
    public int minSquadCount = 3;
    public int maxSquadCount = 8;

    [Header("스쿼드당 멤버 수")]
    public int minMembersPerSquad = 1;
    public int maxMembersPerSquad = 12;

    [Header("스폰/디스폰")]
    [Tooltip("카메라 경계 밖 추가 여유 거리")]
    public float spawnMargin = 3f;
    [Tooltip("플레이어 기준 디스폰 반경")]
    public float despawnDistance = 35f;
    [Tooltip("스폰 시도 주기 (초)")]
    public float spawnInterval = 8f;
}
