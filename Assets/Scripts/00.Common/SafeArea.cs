using UnityEngine;

/// <summary>
/// 디바이스 SafeArea(노치, 홈 인디케이터 등)를 RectTransform 앵커에 반영한다.
///
/// 사용법:
///   Canvas 하위 루트 RectTransform GameObject에 부착.
///   해당 RectTransform의 anchorMin/anchorMax가 SafeArea에 맞춰 조정되어
///   자식 UI가 노치·홈바 영역을 침범하지 않는다.
///
/// 에디터:
///   Device Simulator 해상도 변경을 매 프레임 감지하여 자동 재적용.
///   (빌드에서는 Awake 1회만 실행 — 디바이스 회전 시 재적용 필요 시 OnRectTransformDimensionsChanged 활용)
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect          lastSafeArea   = Rect.zero;
    private Vector2Int    lastScreenSize = Vector2Int.zero;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        Apply();
    }

    /// <summary>화면 회전 등으로 RectTransform 크기가 바뀔 때 자동 재적용.</summary>
    private void OnRectTransformDimensionsChanged()
    {
        Apply();
    }

#if UNITY_EDITOR
    private void Update()
    {
        // Device Simulator에서 해상도/SafeArea 변경을 실시간 반영
        var currentSafeArea  = Screen.safeArea;
        var currentSize      = new Vector2Int(Screen.width, Screen.height);
        if (currentSafeArea != lastSafeArea || currentSize != lastScreenSize)
            Apply();
    }
#endif

    private void Apply()
    {
        var safeArea = Screen.safeArea;

        // 변경 없으면 스킵
        var screenSize = new Vector2Int(Screen.width, Screen.height);
        if (safeArea == lastSafeArea && screenSize == lastScreenSize) return;

        lastSafeArea   = safeArea;
        lastScreenSize = screenSize;

        if (screenSize.x == 0 || screenSize.y == 0) return;

        // 스크린 픽셀 좌표 → 정규화 앵커 (0~1)
        var anchorMin = new Vector2(
            safeArea.xMin / screenSize.x,
            safeArea.yMin / screenSize.y);
        var anchorMax = new Vector2(
            safeArea.xMax / screenSize.x,
            safeArea.yMax / screenSize.y);

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
    }
}
