# 2.1 플레이어 이동 및 입력

> 상위 문서: [Phase 2 설계](../design.md)

플레이어 캐릭터의 입력 처리, 이동, 카메라 추적을 담당한다. 입력은 `PlayerInput`이 읽고 `GameController`를 경유해 `Player.Move()`로 전달된다. View(`PlayerView`)는 이벤트를 구독하여 `UnitMovement`를 구동한다.

```
[PlayerInput (MB)] ── MoveDirection ──→ [GameController (C#)]
                                               │ player.Move(direction)
                                               ▼
                                        [Player (C#)]
                                               │ OnMoveRequested 이벤트
                                               ▼
                                        [PlayerView (MB)]
                                               └──→ UnitMovement.Move()
```

---

## PlayerInput (MonoBehaviour)

Unity Input System 또는 Legacy Input을 읽어 정규화된 방향 벡터를 노출한다. GameController.Update()에서 이 값을 읽는다.

```csharp
public class PlayerInput : MonoBehaviour
{
    public Vector2 MoveDirection { get; private set; }

    private void Update()
    {
        MoveDirection = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;
    }
}
```

---

## Player (pure C#) : Character

`Move()` 호출 시 이벤트로 View에 통지한다. View가 Update()에서 입력을 직접 읽지 않도록 모든 입력 경로는 GameController → Player → PlayerView를 따른다.

```csharp
public class Player : Character
{
    public override UnitTeam Team => UnitTeam.Player;

    public event Action<Vector2> OnMoveRequested;

    public Player(PlayerView view, UnitCombat combat) : base(view, combat)
    {
        view.Subscribe(this);
    }

    public void Move(Vector2 direction) => OnMoveRequested?.Invoke(direction);
}
```

---

## PlayerView (MonoBehaviour) : CharacterView

Player 이벤트를 구독하여 Movement를 구동한다.

```csharp
public class PlayerView : CharacterView
{
    public void Subscribe(Player player)
    {
        player.OnMoveRequested += direction => Movement.Move(direction);
    }
}
```

---

## QuarterViewCamera (MonoBehaviour)

2D 쿼터뷰 카메라. 플레이어를 부드럽게 추적한다.

```csharp
public class QuarterViewCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset;
    [SerializeField] private float smoothSpeed;

    private void LateUpdate()
    {
        var desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }
}
```
