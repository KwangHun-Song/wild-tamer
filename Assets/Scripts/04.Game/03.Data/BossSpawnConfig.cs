using UnityEngine;

/// <summary>
/// 보스 스폰 타이밍 설정. InPlayState에서 BossSpawnSystem에 주입된다.
/// </summary>
[CreateAssetMenu(menuName = "Data/BossSpawnConfig", fileName = "BossSpawnConfig")]
public class BossSpawnConfig : ScriptableObject
{
    [Header("스폰 타이밍")]
    [Tooltip("게임 시작 후 첫 보스 등장까지 대기 시간 (초)")]
    public float spawnTime    = 180f;

    [Tooltip("보스 처치 후 다음 보스 등장까지 대기 시간 (초)")]
    public float respawnDelay = 240f;

    [Tooltip("경고 UI 표시 시간 (초)")]
    public float warnDuration = 2.5f;

    [Header("스폰 위치")]
    [Tooltip("플레이어로부터 스폰 거리 (타일)")]
    public float spawnOffset  = 15f;
}
