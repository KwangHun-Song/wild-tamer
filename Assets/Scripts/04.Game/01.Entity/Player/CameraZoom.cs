using Base;
using UnityEngine;

/// <summary>
/// 마우스 휠로 카메라 orthographicSize를 부드럽게 조절한다.
/// CameraZoomData ScriptableObject에서 최소·최대·스텝·속도를 읽는다.
/// QuarterViewCamera와 같은 카메라 GameObject에 추가한다.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraZoom : MonoBehaviour
{
    private Camera cam;
    private float targetSize;
    private float minSize;
    private float maxSize;
    private float zoomStep;
    private float smoothSpeed;

    private void Awake()
    {
        cam = GetComponent<Camera>();

        var data = Facade.DB.Get<CameraZoomData>("CameraZoomData");
        minSize    = data?.minSize    ?? 3f;
        maxSize    = data?.maxSize    ?? 12f;
        zoomStep   = data?.zoomStep   ?? 1f;
        smoothSpeed = data?.smoothSpeed ?? 10f;

        targetSize = data?.defaultSize ?? cam.orthographicSize;
        cam.orthographicSize = targetSize;
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
            targetSize = Mathf.Max(targetSize - zoomStep, minSize);
        else if (scroll < 0f)
            targetSize = Mathf.Min(targetSize + zoomStep, maxSize);

        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, smoothSpeed * Time.deltaTime);
    }
}
