using UnityEngine;

/// <summary>
/// 카메라 흔들림 파라미터를 저장하는 ScriptableObject.
/// DefaultDatabase가 Resources 폴더에서 "CameraShakeData" 이름으로 조회한다.
/// 예) Facade.DB.Get&lt;CameraShakeData&gt;("CameraShakeData")
/// </summary>
[CreateAssetMenu(menuName = "Data/CameraShakeData")]
public class CameraShakeData : ScriptableObject
{
    [Header("흔들림 강도 — 카메라가 이동하는 최대 거리 (단위: 월드)")]
    public float intensity = 0.1f;

    [Header("흔들림 지속 시간 — 효과가 지속되는 시간 (초)")]
    public float duration = 0.2f;
}
