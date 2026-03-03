# 플로팅 가상 조이스틱 — 설계 문서

## 1. 개요

모바일 환경에서 화면 임의 지점을 터치·드래그하여 캐릭터를 조작하는
**플로팅(Floating) 가상 조이스틱** 시스템.

### 동작 요약

| 단계 | 입력 | 결과 |
|------|------|------|
| TouchDown | 화면 임의 위치 터치 | 해당 위치에 OuterRing + Knob UI 표시 |
| Drag | 손가락 이동 | Knob이 OuterRing 반경 내에서 이동, 방향 벡터 갱신 |
| TouchUp / Cancel | 손가락 뗌 | UI 숨김, 방향 벡터 → (0, 0) |

---

## 2. 아키텍처 결정

### 2-1. PlayerInput 수정 방향

기존 `PlayerInput`은 `Input.GetAxisRaw`만 읽는 단순 MonoBehaviour.
변경 최소화 원칙에 따라 **같은 클래스에 조이스틱 레퍼런스를 추가**한다.

```
PlayerInput.Update()
  kb  = (GetAxisRaw("Horizontal"), GetAxisRaw("Vertical"))
  joy = virtualJoystick?.Direction ?? Vector2.zero
  MoveDirection = (kb + joy).normalized
```

- 키보드와 조이스틱을 **동시에 합산** → 에디터에서 키보드, 실기기에서 터치, 둘 다 동시 사용 가능
- `virtualJoystick`이 null이면 기존 동작과 완전히 동일

### 2-2. VirtualJoystick 분리

UI 처리와 입력 계산은 `VirtualJoystick` 컴포넌트가 담당하고,
`PlayerInput`은 `Direction` 프로퍼티만 읽는다.
이로써 조이스틱 로직은 `PlayerInput`과 완전히 분리된다.

---

## 3. 컴포넌트 구조 (씬 계층)

```
PlayPage Canvas
└── VirtualJoystickRoot          ← RectTransform (CanvasGroup)
    ├── OuterRing                ← Image (원형, 반투명)
    └── Knob                     ← Image (원형, 불투명)
```

- `VirtualJoystickRoot`는 터치 시작 위치로 매 터치마다 이동
- `CanvasGroup.alpha`로 표시/숨김 제어 (SetActive 대신 → GC 없음)
- `Knob`은 `OuterRing`의 **자식이 아님** — `VirtualJoystickRoot` 기준 `anchoredPosition`으로 이동
  > 자식으로 두면 OuterRing 이미지 스케일 변경 시 Knob도 같이 변형되어 분리 배치가 더 안정적

---

## 4. VirtualJoystick 클래스 설계

```csharp
public class VirtualJoystick : MonoBehaviour
{
    [SerializeField] RectTransform joystickRoot;   // 터치 위치로 이동하는 루트
    [SerializeField] RectTransform knob;           // 노브 RectTransform
    [SerializeField] CanvasGroup   canvasGroup;    // alpha 제어
    [SerializeField] float outerRadius = 80f;      // 외부 링 반경 (px, Canvas pixels)
    [SerializeField] float deadzone   = 10f;       // 데드존 반경 (px)
    [SerializeField] float opacity    = 0.7f;      // 활성 시 알파

    public Vector2 Direction { get; private set; }

    // 내부 상태
    private int     activeFingerId   = -1;
    private Vector2 startScreenPos;
}
```

### 입력 흐름

```
Update()
  #if UNITY_EDITOR || UNITY_STANDALONE
    HandleMouseInput()
  #else
    HandleTouchInput()
  #endif

HandleTouchInput():
  foreach Touch t in Input.touches
    Began  → if activeFingerId == -1 && !IsPointerOverUI(t.fingerId) → BeginJoystick(t.position)
    Moved  → if t.fingerId == activeFingerId → UpdateJoystick(t.position)
    Ended/Canceled → if t.fingerId == activeFingerId → EndJoystick()

HandleMouseInput():
  GetMouseButtonDown(0) → if !IsPointerOverUI(-1) → BeginJoystick(mousePos)
  GetMouseButton(0)     → if active → UpdateJoystick(mousePos)
  GetMouseButtonUp(0)   → EndJoystick()
```

### 핵심 메서드

```csharp
void BeginJoystick(Vector2 screenPos)
  activeFingerId       = fingerId
  startScreenPos       = screenPos
  joystickRoot.position = ScreenToCanvasPosition(screenPos)
  knob.anchoredPosition = Vector2.zero
  canvasGroup.alpha    = opacity

void UpdateJoystick(Vector2 screenPos)
  Vector2 delta   = screenPos - startScreenPos
  if delta.magnitude < deadzone:
      Direction             = Vector2.zero
      knob.anchoredPosition = Vector2.zero
      return
  Vector2 clamped       = Vector2.ClampMagnitude(delta, outerRadius)
  knob.anchoredPosition = clamped               // UI 노브 위치 (px 단위)
  Direction             = clamped / outerRadius  // 방향 벡터 [-1, 1]

void EndJoystick()
  activeFingerId        = -1
  Direction             = Vector2.zero
  knob.anchoredPosition = Vector2.zero
  canvasGroup.alpha     = 0
```

### 좌표 변환

Canvas가 **Screen Space - Camera** 모드이므로 스크린 좌표 → Canvas 로컬 좌표 변환 필요.

```csharp
private Vector2 ScreenToCanvasPosition(Vector2 screenPos)
{
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        canvas.GetComponent<RectTransform>(),
        screenPos,
        canvas.worldCamera,
        out Vector2 localPoint);
    return canvas.transform.TransformPoint(localPoint);  // world position for joystickRoot.position
}
```

---

## 5. UI 터치 충돌 방지

Setting 버튼 등 UI 버튼 위에서 시작된 터치는 조이스틱으로 처리하지 않는다.

```csharp
private bool IsPointerOverUI(int fingerId)
{
    if (fingerId < 0)
        return EventSystem.current.IsPointerOverGameObject();  // mouse
    return EventSystem.current.IsPointerOverGameObject(fingerId);
}
```

---

## 6. 멀티터치 처리

- `activeFingerId`로 단일 터치만 추적
- 조이스틱 활성 중 추가 터치는 무시 (전투 탭 등 다른 용도로 확장 가능)
- 첫 터치 종료 후 다음 터치가 오면 새로 시작

---

## 7. VirtualJoystickSettings (ScriptableObject) — 선택적

조정 빈도가 높은 수치는 ScriptableObject로 추출해 에디터에서 런타임 조정 가능.
우선 Inspector 직렬화로 시작하고, 튜닝 필요 시 추출한다.

| 필드 | 기본값 | 설명 |
|------|--------|------|
| outerRadius | 80 | 외부 링 반경 (UI px) |
| deadzone | 10 | 최소 드래그 거리 |
| opacity | 0.7 | 활성 시 투명도 |

---

## 8. 이식성 / 확장성

- `VirtualJoystick`은 `PlayerInput`과 완전히 분리 → 다른 씬이나 캐릭터에도 재사용 가능
- 공격 버튼, 스킬 버튼 추가 시 `VirtualJoystick` 옆에 독립 UI 추가만 하면 됨
- `Direction`을 `IPlayerInput` 인터페이스로 추상화하면 키보드/조이스틱 구현체 교체 가능 (현재는 단순 합산으로 충분)

---

## 9. 검증 기준

| 항목 | 기준 |
|------|------|
| 에디터 마우스 | 클릭·드래그 시 캐릭터 이동, 방향 정확 |
| 모바일 터치 | 화면 임의 위치 터치 → UI 즉시 표시 |
| 데드존 | 10px 이내 드래그 시 캐릭터 정지 |
| UI 버튼 충돌 | Setting 버튼 탭 시 조이스틱 미작동 |
| 키보드 병행 | 에디터에서 키보드와 마우스 조이스틱 동시 동작 |
| 릴리즈 | 손가락 떼면 즉시 정지 + UI 숨김 |
