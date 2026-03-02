# 2.1 플레이어 이동 및 입력

> 상위 문서: [Phase 2 설계](../design.md)

플레이어 캐릭터의 입력 처리, 이동, 카메라 추적을 담당한다. 입력은 `PlayerInput`이 읽고 `GameController`를 경유해 `Player.SetInput()`으로 전달된다. FSM(`PlayerFSM`)이 매 프레임 상태에 따라 이동·애님·공격을 제어한다.

```
[PlayerInput (MB)] ── MoveDirection ──→ [GameController (C#)]
                                               │ Player.SetInput(direction)
                                               │ Player.Update()  ← FSM 구동
                                               ▼
                                        [PlayerFSM]
                                         ├── PlayerIdleState   ← 이동 + 애님 처리
                                         ├── PlayerAttackState ← 이동 + 공격 처리
                                         └── PlayerDeadState
```

---

## PlayerInput (MonoBehaviour)

Unity Legacy Input을 읽어 정규화된 방향 벡터를 노출한다. GameController.Update()에서 이 값을 읽는다.

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

`PlayerFSM`을 소유하며, `SetInput()`으로 입력 방향을 저장하고 `Update()`로 FSM을 구동한다.
공격 이벤트(`OnAttackFired`)가 발생하면 FSM에 `StartAttack` 트리거를 전달한다.

```csharp
public class Player : Character
{
    public override UnitTeam Team => UnitTeam.Player;
    public Vector2 InputDirection { get; private set; }

    private readonly PlayerFSM fsm;

    public Player(PlayerView view, UnitCombat combat, int maxHp) : base(view, combat)
    {
        Health.Initialize(maxHp);
        Health.OnDeath += () => fsm.ExecuteCommand(PlayerTrigger.Die);
        fsm = new PlayerFSM(this);
        fsm.SetUp();
        OnAttackFired += () => fsm.ExecuteCommand(PlayerTrigger.StartAttack);
    }

    public void SetInput(Vector2 direction) => InputDirection = direction;
    public void Update() => fsm.Update();
}
```

---

## PlayerFSM / 상태 구조

Move 상태 없이 **Idle·Attack 모두에서 이동이 가능**하다. Dead 상태만 이동을 차단한다.

| 상태 | 역할 |
|---|---|
| `PlayerIdleState` | 입력 있으면 move 애님 + 이동, 없으면 idle 애님 + 정지 |
| `PlayerAttackState` | 공격 애님 재생, 이동 입력 있으면 이동 병행, 쿨다운 완료 시 Idle 복귀 |
| `PlayerDeadState` | 이동 정지, dead 애님 재생 |

트리거: `StartAttack`, `StopAttack`, `Die` (Move 관련 트리거 없음)

---

## PlayerView (MonoBehaviour) : CharacterView

이벤트 구독 없이 FSM States가 직접 호출하는 애님 API만 제공한다.

```csharp
public class PlayerView : CharacterView { }  // CharacterView 상속만
```

애님 API는 `CharacterView`에 정의되어 있다: `PlayIdleAnimation()`, `PlayMoveAnimation()`, `PlayAttackAnimation()`, `PlayDeadAnimation()`, `UpdateFacing(Vector2)`.

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
