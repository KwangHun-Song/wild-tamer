# 플로팅 가상 조이스틱 — 구현 계획

## 전제 조건

- 설계 문서: `Assets/Docs/System/Input/design.md` 숙지
- 수정 대상 씬: PlayPage (Canvas, PlayerInput 컴포넌트 포함)
- 기존 `PlayerInput.cs`의 `MoveDirection` 인터페이스는 그대로 유지

---

## Step 1: VirtualJoystick.cs 작성

**파일**: `Assets/Scripts/04.Game/01.Entity/Player/VirtualJoystick.cs`

```csharp
using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour
{
    [SerializeField] private RectTransform joystickRoot;
    [SerializeField] private RectTransform knob;
    [SerializeField] private CanvasGroup   canvasGroup;
    [SerializeField] private Canvas        canvas;

    [SerializeField] private float outerRadius = 80f;
    [SerializeField] private float deadzone    = 10f;
    [SerializeField] private float opacity     = 0.7f;

    public Vector2 Direction { get; private set; }

    private int     activeFingerId = -1;
    private Vector2 startScreenPos;

    private void Awake()
    {
        canvasGroup.alpha = 0f;
    }

    private void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#else
        HandleTouchInput();
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
                    if (activeFingerId == -1 && !IsPointerOverUI(touch.fingerId))
                        BeginJoystick(touch.fingerId, touch.position);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (touch.fingerId == activeFingerId)
                        UpdateJoystick(touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (touch.fingerId == activeFingerId)
                        EndJoystick();
                    break;
            }
        }
    }

    // ── Mouse (Editor / Standalone) ────────────────────────────────────────

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0) && activeFingerId == -1 && !IsPointerOverUI(-1))
            BeginJoystick(-1, Input.mousePosition);
        else if (Input.GetMouseButton(0) && activeFingerId == -1 && activeFingerId == -1)
        {
            /* already handled by BeginJoystick on same frame */
        }

        if (activeFingerId == -1) return;

        if (Input.GetMouseButton(0))
            UpdateJoystick(Input.mousePosition);
        else if (Input.GetMouseButtonUp(0))
            EndJoystick();
    }

    // ── Core logic ─────────────────────────────────────────────────────────

    private void BeginJoystick(int fingerId, Vector2 screenPos)
    {
        activeFingerId        = fingerId;
        startScreenPos        = screenPos;
        joystickRoot.position = ScreenToWorldPosition(screenPos);
        knob.anchoredPosition = Vector2.zero;
        canvasGroup.alpha     = opacity;
    }

    private void UpdateJoystick(Vector2 screenPos)
    {
        // 스크린 픽셀 델타 → Canvas 픽셀 단위로 변환
        Vector2 delta = ScreenDeltaToCanvas(screenPos - startScreenPos);

        if (delta.magnitude < deadzone)
        {
            Direction             = Vector2.zero;
            knob.anchoredPosition = Vector2.zero;
            return;
        }

        Vector2 clamped       = Vector2.ClampMagnitude(delta, outerRadius);
        knob.anchoredPosition = clamped;
        Direction             = clamped / outerRadius;
    }

    private void EndJoystick()
    {
        activeFingerId        = -1;
        Direction             = Vector2.zero;
        knob.anchoredPosition = Vector2.zero;
        canvasGroup.alpha     = 0f;
    }

    // ── Coordinate helpers ─────────────────────────────────────────────────

    /// <summary>스크린 좌표를 Canvas 월드 위치로 변환 (joystickRoot.position 용).</summary>
    private Vector3 ScreenToWorldPosition(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            screenPos,
            canvas.worldCamera,
            out Vector3 worldPoint);
        return worldPoint;
    }

    /// <summary>스크린 픽셀 델타를 Canvas 픽셀 단위 델타로 스케일 변환.</summary>
    private Vector2 ScreenDeltaToCanvas(Vector2 screenDelta)
    {
        // Canvas scaler가 화면 해상도에 따라 스케일을 조정하므로
        // 스크린 좌표를 그대로 사용하면 고해상도에서 반경이 실제보다 작게 느껴진다.
        // canvasScaleFactor로 나누어 Canvas 픽셀 단위로 정규화.
        float scaleFactor = canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        return screenDelta / scaleFactor;
    }

    private static bool IsPointerOverUI(int fingerId)
    {
        return fingerId < 0
            ? EventSystem.current.IsPointerOverGameObject()
            : EventSystem.current.IsPointerOverGameObject(fingerId);
    }
}
```

**구현 후 체크**:
- [ ] `joystickRoot`, `knob`, `canvasGroup`, `canvas` 레퍼런스 Inspector에서 할당
- [ ] Awake에서 alpha = 0 초기화 확인

---

## Step 2: PlayerInput.cs 수정

**파일**: `Assets/Scripts/04.Game/01.Entity/Player/PlayerInput.cs`

```csharp
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [SerializeField] private VirtualJoystick virtualJoystick;  // optional, 없으면 키보드만

    public Vector2 MoveDirection { get; private set; }

    private void Update()
    {
        var kb  = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        var joy = virtualJoystick != null ? virtualJoystick.Direction : Vector2.zero;
        MoveDirection = (kb + joy).normalized;
    }
}
```

**변경 포인트**:
- `virtualJoystick` 필드 추가 (nullable — null 시 기존 동작 100% 유지)
- 키보드 + 조이스틱 합산 후 normalized → 대각선 과속 방지

---

## Step 3: 프리팹 / 씬 작업 (Unity 에디터)

### 3-1. JoystickRoot GameObject 생성

PlayPage Canvas 하위에 빈 GameObject 생성:
```
Canvas
└── JoystickRoot          ← 이름: "VirtualJoystickRoot"
    ├── OuterRing          ← Image (원형 Sprite)
    └── Knob               ← Image (원형 Sprite, OuterRing의 형제 — 자식 아님)
```

> Knob을 OuterRing의 형제로 두는 이유: OuterRing과 독립적인 anchoredPosition 사용

### 3-2. VirtualJoystickRoot 설정

| 컴포넌트 | 설정 |
|----------|------|
| RectTransform | Anchor: Middle/Center, Pivot: (0.5, 0.5), Size: (160, 160) |
| CanvasGroup | Alpha: 0, Interactable: false, BlocksRaycasts: false |
| VirtualJoystick | 스크립트 추가 |

> `BlocksRaycasts: false` 필수 — 조이스틱 UI가 터치 이벤트를 가로채지 않아야 함

### 3-3. OuterRing 설정

| 필드 | 값 |
|------|-----|
| RectTransform | Size: (160, 160), Anchor: Middle/Center |
| Image | 원형 Sprite, Color: (1,1,1,0.4) — 반투명 흰색 |
| Raycast Target | **Off** (터치 이벤트 통과) |

### 3-4. Knob 설정

| 필드 | 값 |
|------|-----|
| RectTransform | Size: (80, 80), Anchor: Middle/Center, anchoredPosition: (0,0) |
| Image | 원형 Sprite, Color: (1,1,1,0.8) |
| Raycast Target | **Off** |

### 3-5. VirtualJoystick 필드 연결 (Inspector)

`VirtualJoystickRoot` 의 `VirtualJoystick` 컴포넌트:
- `Joystick Root` → VirtualJoystickRoot (자기 자신의 RectTransform)
- `Knob` → Knob RectTransform
- `Canvas Group` → VirtualJoystickRoot의 CanvasGroup
- `Canvas` → PlayPage Canvas
- `Outer Radius` → 80
- `Deadzone` → 10
- `Opacity` → 0.7

### 3-6. PlayerInput 필드 연결

`InPlayState`(또는 PlayerInput이 붙은 오브젝트)의 `PlayerInput` 컴포넌트:
- `Virtual Joystick` → VirtualJoystickRoot의 VirtualJoystick

### 3-7. 렌더 순서

JoystickRoot는 Canvas 자식 중 **가장 아래(마지막)** 배치 → 다른 UI 위에 렌더링

---

## Step 4: 검증

### 에디터 (마우스)
1. Play Mode 진입
2. 화면 빈 공간 클릭 → OuterRing + Knob UI가 클릭 지점에 표시되는지 확인
3. 클릭 유지하며 드래그 → 캐릭터 이동, Knob이 OuterRing 경계 내에서 이동하는지 확인
4. 마우스 버튼 해제 → UI 사라지고 캐릭터 정지 확인
5. Setting 버튼 클릭 → 조이스틱 미작동 확인

### 이동 방향 정확도
- 오른쪽 드래그 → MoveDirection.x ≈ 1, y ≈ 0
- 위쪽 드래그 → MoveDirection.y ≈ 1, x ≈ 0
- 대각선 드래그 → magnitude ≈ 1 (normalized 확인)

### 데드존
- 10px 미만 드래그 → MoveDirection == (0, 0)

### 키보드 병행 (에디터)
- 키보드 W + 마우스 오른쪽 드래그 → 합산 방향으로 이동 (우상향)

---

## 파일 요약

| 파일 | 유형 | 변경 |
|------|------|------|
| `Assets/Scripts/04.Game/01.Entity/Player/VirtualJoystick.cs` | 신규 | |
| `Assets/Scripts/04.Game/01.Entity/Player/PlayerInput.cs` | 수정 | VirtualJoystick 필드 추가 |
| PlayPage 씬 (또는 프리팹) | Unity 에디터 | JoystickRoot UI 추가, 레퍼런스 연결 |

---

## 주의사항

### Canvas scaleFactor
Canvas의 `scaleFactor`가 1이 아닌 경우(CanvasScaler가 해상도에 따라 스케일) 스크린 픽셀 델타를 그대로 쓰면 고해상도 기기에서 조이스틱 반경이 실제보다 작게 느껴진다. `ScreenDeltaToCanvas()`에서 `canvas.scaleFactor`로 나누어 보정한다.

### Screen Space - Camera Canvas
PlayPage Canvas는 Screen Space - Camera 모드이므로 `ScreenPointToWorldPointInRectangle` 사용. Screen Space - Overlay라면 `canvas.worldCamera`를 null로 전달.

### GC 주의
`Input.touches`는 매 프레임 배열을 반환하지 않고 내부 캐시를 사용하므로 foreach 열거는 GC-free. `Input.touchCount` + 인덱스 루프와 동일하게 안전.
