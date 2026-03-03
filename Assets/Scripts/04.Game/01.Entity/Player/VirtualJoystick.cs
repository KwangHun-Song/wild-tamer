using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 화면 임의 위치를 터치·드래그하여 방향 입력을 생성하는 플로팅 가상 조이스틱.
///
/// 프리팹 구조 전제:
///   Canvas (Screen Space - Overlay, GraphicRaycaster 없음)  ← 루트 == 이 스크립트 소유자
///   └── JoystickRoot (RectTransform + CanvasGroup, Anchor = Center)
///       ├── OuterRing (Image, RaycastTarget = false)
///       └── Knob (Image, RaycastTarget = false)
///
/// 좌표계:
///   - ScreenToCanvasLocal() 이 스크린 픽셀 → Canvas 로컬 좌표로 변환
///   - JoystickRoot.anchor = (0.5, 0.5) 전제 — anchoredPosition 직접 사용
///   - 드래그 델타는 canvas.scaleFactor 로 나누어 해상도 독립성 보장
/// </summary>
public class VirtualJoystick : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas        canvas;
    [SerializeField] private RectTransform joystickRoot;
    [SerializeField] private RectTransform knob;
    [SerializeField] private CanvasGroup   canvasGroup;

    [Header("Settings")]
    [SerializeField] private float outerRadius = 80f;  // 외부 링 반경 (Canvas px)
    [SerializeField] private float deadzone    = 10f;  // 최소 드래그 거리 (Canvas px)
    [SerializeField] private float opacity     = 0.7f; // 활성 시 투명도

    /// <summary>현재 이동 방향 벡터. 크기 [0, 1]. 미활성 시 (0, 0).</summary>
    public Vector2 Direction { get; private set; }

    private bool    isActive       = false;
    private int     activeFingerId = -1;
    private Vector2 startScreenPos;
    private RectTransform canvasRect;

    private void Awake()
    {
        canvasRect        = canvas.GetComponent<RectTransform>();
        canvasGroup.alpha = 0f;
    }

    private void Update()
    {
        HandleTouchInput();
#if UNITY_EDITOR || UNITY_STANDALONE
        // 에디터 / 스탠드얼론: 터치가 없을 때 마우스로 시뮬레이션
        if (Input.touchCount == 0)
            HandleMouseInput();
#endif
    }

    // ── Touch ──────────────────────────────────────────────────────────────

    private void HandleTouchInput()
    {
        foreach (var touch in Input.touches)
        {
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (!isActive && !IsPointerOverUI(touch.fingerId))
                        BeginJoystick(touch.fingerId, touch.position);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (isActive && touch.fingerId == activeFingerId)
                        UpdateJoystick(touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (isActive && touch.fingerId == activeFingerId)
                        EndJoystick();
                    break;
            }
        }
    }

    // ── Mouse (Editor / Standalone) ────────────────────────────────────────

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0) && !isActive && !IsPointerOverUI(-1))
            BeginJoystick(-1, Input.mousePosition);

        if (!isActive) return;

        if (Input.GetMouseButton(0))
            UpdateJoystick(Input.mousePosition);
        else
            EndJoystick();
    }

    // ── Core logic ─────────────────────────────────────────────────────────

    private void BeginJoystick(int fingerId, Vector2 screenPos)
    {
        isActive       = true;
        activeFingerId = fingerId;
        startScreenPos = screenPos;

        // 터치 지점에 조이스틱 루트 배치
        joystickRoot.anchoredPosition = ScreenToCanvasLocal(screenPos);
        knob.anchoredPosition         = Vector2.zero;
        canvasGroup.alpha             = opacity;
    }

    private void UpdateJoystick(Vector2 screenPos)
    {
        // 스크린 픽셀 델타 → Canvas 픽셀 단위 변환 (CanvasScaler 보정)
        float   scale       = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        Vector2 canvasDelta = (screenPos - startScreenPos) / scale;

        if (canvasDelta.magnitude < deadzone)
        {
            Direction             = Vector2.zero;
            knob.anchoredPosition = Vector2.zero;
            return;
        }

        Vector2 clamped       = Vector2.ClampMagnitude(canvasDelta, outerRadius);
        knob.anchoredPosition = clamped;
        Direction             = clamped / outerRadius;
    }

    private void EndJoystick()
    {
        isActive              = false;
        activeFingerId        = -1;
        Direction             = Vector2.zero;
        knob.anchoredPosition = Vector2.zero;
        canvasGroup.alpha     = 0f;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>
    /// 스크린 좌표 → Canvas RectTransform 로컬 좌표 변환.
    /// Overlay Canvas이므로 worldCamera = null.
    /// joystickRoot.anchor = (0.5, 0.5) 전제 시 anchoredPosition에 직접 사용 가능.
    /// </summary>
    private Vector2 ScreenToCanvasLocal(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, null, out Vector2 localPoint);
        return localPoint;
    }

    private static bool IsPointerOverUI(int fingerId)
    {
        if (EventSystem.current == null) return false;
        return fingerId < 0
            ? EventSystem.current.IsPointerOverGameObject()
            : EventSystem.current.IsPointerOverGameObject(fingerId);
    }
}
