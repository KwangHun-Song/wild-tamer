using UnityEngine;

/// <summary>
/// 카메라 흔들림 파라미터를 저장하는 ScriptableObject.
/// DefaultDatabase가 Resources 폴더에서 "CameraShakeData" 이름으로 조회한다.
/// 예) Facade.DB.Get&lt;CameraShakeData&gt;("CameraShakeData")
/// </summary>
[CreateAssetMenu(menuName = "Data/CameraShakeData")]
public class CameraShakeData : ScriptableObject
{
    [Header("일반 피격 흔들림")]
    public float intensity = 0.1f;
    public float duration  = 0.2f;

    [Header("보스 패턴 피격 흔들림 (더 강하게)")]
    public float bossIntensity = 0.35f;
    public float bossDuration  = 0.45f;
}
