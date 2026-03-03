using UnityEngine;

/// <summary>
/// 카메라 줌 파라미터를 저장하는 ScriptableObject.
/// DefaultDatabase가 Resources 폴더에서 "CameraZoomData" 이름으로 조회한다.
/// 예) Facade.DB.Get&lt;CameraZoomData&gt;("CameraZoomData")
/// </summary>
[CreateAssetMenu(menuName = "Data/CameraZoomData")]
public class CameraZoomData : ScriptableObject
{
    [Header("기본 줌 크기 (orthographicSize)")]
    public float defaultSize = 6f;

    [Header("최소 줌 — 가장 가까이 볼 수 있는 크기")]
    public float minSize = 3f;

    [Header("최대 줌 — 가장 멀리 볼 수 있는 크기")]
    public float maxSize = 12f;

    [Header("휠 1회당 줌 변화량")]
    public float zoomStep = 1f;

    [Header("줌 보간 속도 (Lerp)")]
    public float smoothSpeed = 10f;
}
