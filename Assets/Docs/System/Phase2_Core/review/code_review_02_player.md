# 2.1 플레이어 이동 및 입력 - 코드 리뷰

## 리뷰 대상 파일

| 파일 | 역할 | MonoBehaviour |
|------|------|:---:|
| `PlayerInput.cs` | 입력 읽기, 방향 벡터 노출 | O |
| `Player.cs` | Presenter (pure C#), 이동 이벤트 발행 | X |
| `PlayerView.cs` | View, 이벤트 구독 후 UnitMovement 구동 | O |
| `QuarterViewCamera.cs` | 2D 쿼터뷰 카메라 추적 | O |

### 참조한 설계 문서

- `Assets/Docs/System/Phase2_Core/design/player.md`
- `Assets/Docs/System/Phase2_Core/design/entity_common.md`
- `Assets/Docs/System/Phase2_Core/design.md`

---

## 설계 일관성

구현이 설계 문서와 일치하는지 항목별로 대조한다.

| 항목 | 설계 문서 | 실제 구현 | 판단 |
|------|-----------|-----------|------|
| PlayerInput: MonoBehaviour | O | O | 일치 |
| PlayerInput: `Vector2 MoveDirection { get; private set; }` | O | O | 일치 |
| PlayerInput: `Update()`에서 `Input.GetAxisRaw` 정규화 | O | O | 일치 |
| Player: pure C# Character 상속 | O | O | 일치 |
| Player: `UnitTeam.Player` | O | O | 일치 |
| Player: `event Action<Vector2> OnMoveRequested` | O | O | 일치 |
| Player: 생성자에서 `view.Subscribe(this)` | O | O | 일치 |
| Player: `Move(Vector2)` -> 이벤트 발행 | O | O | 일치 |
| PlayerView: MonoBehaviour CharacterView 상속 | O | O | 일치 |
| PlayerView: `Subscribe(Player)` 이벤트 구독 | O | O | 일치 |
| PlayerView: `Update()` 없음 | O (원칙) | O | 일치 |
| QuarterViewCamera: `[SerializeField]` target, offset, smoothSpeed | O | O | 일치 |
| QuarterViewCamera: `LateUpdate()` Lerp 추적 | O | O | 일치 |
| QuarterViewCamera: smoothSpeed 기본값 | 미지정 | `= 5f` | 구현이 개선된 형태 |

설계 문서의 클래스 명세와 구현이 **완전히 일치**한다. smoothSpeed 기본값 추가는 인스펙터 편의를 위한 합리적인 개선이다.

---

## 긍정적인 점

### 1. MVP 패턴의 정확한 구현

Player(Presenter)가 View에 직접 명령하지 않고 `OnMoveRequested` 이벤트를 통해서만 통신한다. PlayerView는 `Subscribe()`에서 이벤트를 구독하고 `Movement.Move()`를 호출하는 구조로, Presenter→View 단방향 이벤트 흐름이 설계 의도대로 정확히 구현되어 있다.

```csharp
// Player.cs:15 — Presenter는 이벤트만 발행
public void Move(Vector2 direction) => OnMoveRequested?.Invoke(direction);

// PlayerView.cs:4-5 — View는 이벤트를 구독하여 Movement 구동
player.OnMoveRequested += direction => Movement.Move(direction);
```

### 2. 입력 경로 격리

PlayerInput은 `MoveDirection` 프로퍼티만 노출하고 어떤 시스템에도 직접 명령하지 않는다. GameController가 이 값을 읽어 `Player.Move()`를 호출하는 구조가 설계대로 지켜졌다. 입력 소스를 교체하더라도 PlayerInput만 수정하면 되는 깨끗한 분리다.

### 3. View에 Update() 부재

PlayerView에 `Update()` 메서드가 없다. 모든 이동 명령은 Presenter 이벤트 경유로만 도달하므로, View가 독자적으로 로직을 실행하는 일이 없다. 이는 MVP 원칙과 설계 문서의 명시적 요구사항을 모두 충족한다.

### 4. 코드 간결성

4개 파일 모두 단일 책임에 충실하며, 불필요한 코드가 없다. PlayerInput 14줄, Player 16줄, PlayerView 7줄, QuarterViewCamera 14줄로 각 클래스가 하나의 역할만 수행한다.

---

## 이슈

### 1. PlayerView 이벤트 구독 해제 누락 (중요도: 높음)

**파일**: `PlayerView.cs:3-6`

```csharp
public void Subscribe(Player player)
{
    player.OnMoveRequested += direction => Movement.Move(direction);
}
```

익명 람다로 이벤트를 구독하고 있어 구독 해제가 불가능하다. Player(pure C#)가 PlayerView(MonoBehaviour)보다 오래 생존하는 경우, PlayerView가 `Destroy`된 후에도 Player가 해당 델리게이트를 유지하게 된다. 이는 다음 문제를 유발한다:

- **NullReferenceException**: 파괴된 MonoBehaviour의 `Movement` 접근 시 예외 발생
- **메모리 누수**: GC가 파괴된 PlayerView 관련 객체를 수집하지 못함

현재 Player와 PlayerView가 1:1로 생성/소멸되는 구조에서는 실질적 문제가 발생하지 않을 수 있으나, 풀링이나 씬 전환 등 생명주기가 달라지는 상황에서 버그로 이어진다.

**제안**: 람다 대신 명명된 메서드를 사용하고 `Unsubscribe` 메서드를 추가한다.

```csharp
public class PlayerView : CharacterView
{
    private Player subscribedPlayer;

    public void Subscribe(Player player)
    {
        subscribedPlayer = player;
        player.OnMoveRequested += OnMoveRequested;
    }

    public void Unsubscribe()
    {
        if (subscribedPlayer != null)
        {
            subscribedPlayer.OnMoveRequested -= OnMoveRequested;
            subscribedPlayer = null;
        }
    }

    private void OnMoveRequested(Vector2 direction) => Movement.Move(direction);

    private void OnDestroy() => Unsubscribe();
}
```

> **[수정 완료]** 커밋 `4550d55`에서 반영됨. 람다 대신 명명된 메서드, `subscribedPlayer` 필드, `Unsubscribe()` 및 `OnDestroy()` 구현됨.

---

### 2. QuarterViewCamera target null 체크 부재 (중요도: 높음)

**파일**: `QuarterViewCamera.cs:11`

```csharp
private void LateUpdate()
{
    var desired = target.position + offset;
    transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
}
```

`target`이 인스펙터에서 할당되지 않았거나, 런타임에 대상 오브젝트가 파괴된 경우 `NullReferenceException`이 매 프레임 발생한다. `LateUpdate()`에서 발생하는 예외는 콘솔을 빠르게 채우고 성능에도 악영향을 미친다.

**제안**: null 체크를 추가한다.

```csharp
private void LateUpdate()
{
    if (target == null) return;

    var desired = target.position + offset;
    transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
}
```

> **[수정 완료]** 커밋 `4550d55`에서 반영됨. `LateUpdate()` 시작부에 `if (target == null) return;` null 체크 추가됨.

---

### 3. 유닛 테스트 부재 (중요도: 중간)

Player 관련 클래스에 대한 유닛 테스트가 존재하지 않는다. Player는 pure C# 클래스이므로 EditMode 테스트가 가능하다. 특히 다음 항목은 테스트로 검증할 수 있다:

- `Player.Move()` 호출 시 `OnMoveRequested` 이벤트 발행 여부
- `Player.Team`이 `UnitTeam.Player`인지 확인
- 생성자에서 `view.Subscribe(this)`가 정상 호출되는지

**제안**: `Player`의 이벤트 발행 로직을 검증하는 EditMode 테스트를 추가한다.

---

### 4. QuarterViewCamera Lerp t값 클램핑 미적용 (중요도: 낮음)

**파일**: `QuarterViewCamera.cs:12`

```csharp
transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
```

`Vector3.Lerp`의 세 번째 파라미터(`t`)는 `[0, 1]` 범위에서 의미가 있다. `smoothSpeed = 5f` 기본값에서 프레임레이트가 5fps 이하로 떨어지면 `t >= 1.0`이 되어 스무딩 효과가 사라진다. Unity의 `Vector3.Lerp`는 내부적으로 `t`를 클램핑하므로 오버슈트는 발생하지 않으나, 의도된 부드러운 추적 동작이 무효화된다.

일반적인 게임 환경(30fps 이상)에서는 문제가 없으며, 이는 널리 사용되는 패턴이므로 현 단계에서 수정 우선순위는 낮다.

**참고**: 프레임 독립적인 정확한 스무딩이 필요한 경우 지수 감쇠 방식을 사용할 수 있다.

```csharp
transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
```

---

### 5. 네임스페이스 미사용 (중요도: 낮음)

4개 파일 모두 네임스페이스를 사용하지 않고 글로벌 스코프에 클래스를 선언하고 있다. 코딩 컨벤션 문서(`csharp_coding_convention.md`)에서 네임스페이스 규칙은 "논의가 필요한 사항"으로 분류되어 있어 현재 위반은 아니다. 다만 프로젝트 규모가 커지면 이름 충돌 가능성이 높아지므로 기록해 둔다.

---

## 종합 평가

| 항목 | 등급 | 설명 |
|------|------|------|
| 설계 일관성 | **A** | 설계 문서의 클래스 명세, 데이터 흐름, MVP 패턴이 구현에 정확히 반영됨 |
| 코딩 컨벤션 준수 | **A** | Allman 스타일, 명명 규칙, 접근 제한자 명시 등 모든 항목 충족 |
| 캡슐화 | **B+** | MVP 분리는 우수하나 이벤트 구독 해제 경로가 없어 생명주기 분리가 불완전 |
| 에러 처리 | **B** | QuarterViewCamera의 target null 체크 부재. 런타임 예외 가능성 존재 |
| 테스트 존재 | **C** | Player 관련 유닛 테스트가 전무. pure C# 클래스임에도 테스트 미작성 |

### 우선 보강이 필요한 2가지

1. **PlayerView 이벤트 구독 해제 메커니즘 추가** — 람다 대신 명명된 메서드를 사용하고 `OnDestroy`에서 구독을 해제하여, View 파괴 후 이벤트 호출에 의한 NullReferenceException과 메모리 누수를 방지한다.
2. **QuarterViewCamera target null 체크 추가** — `LateUpdate()`에서 target이 null인 경우 즉시 반환하여, 대상 미할당 또는 런타임 파괴 시 매 프레임 예외 발생을 방지한다.
