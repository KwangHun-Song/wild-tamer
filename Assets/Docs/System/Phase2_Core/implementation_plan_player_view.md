# PlayerView 프리팹 구현 계획

## 개요

`PlayerView`는 플레이어 캐릭터의 View 레이어 컴포넌트다.
`Player`(pure C#) 모델로부터 이벤트를 구독하고, `UnitMovement`에 이동을 위임하며,
`Animator`를 통해 Idle/Run 애니메이션을 제어한다.

| 클래스 | 역할 |
|--------|------|
| `CharacterView` | `UnitMovement` SerializeField 보유, 추상 View 베이스 |
| `PlayerView` | `Player` 이벤트 구독, 이동 위임, 애니메이션 전환 |
| `UnitMovement` | `transform.Translate`로 실제 위치 이동 |

---

## 프리팹 하이에라키 구조

```
PlayerView (root GO)                [PlayerView 컴포넌트, UnitMovement 컴포넌트]
└── Visual (child GO)               [SpriteRenderer 컴포넌트, Animator 컴포넌트]
```

- `UnitMovement`는 루트 GO에 부착 → 루트 Transform이 직접 이동
- `SpriteRenderer` + `Animator`는 Visual 자식에 분리 → 향후 스프라이트 flip, 오프셋 조정 용이
- `CharacterView.movement` → 루트 GO의 `UnitMovement` 연결

---

## 사용 스프라이트

플레이어는 Blue Pawn을 사용한다.

| 파일 | 경로 | 용도 |
|------|------|------|
| `Pawn_Idle.png` | `Assets/Graphic/Sprites/Units/Blue Units/Pawn/` | Idle 애니메이션 |
| `Pawn_Run.png` | `Assets/Graphic/Sprites/Units/Blue Units/Pawn/` | Run 애니메이션 |

---

## 단계별 구현 순서

### Step 1 — 스프라이트 슬라이싱 (Unity MCP / 에디터 작업)

`Pawn_Idle.png`, `Pawn_Run.png` 두 스프라이트 시트를 슬라이싱한다.

#### 공통 설정 (두 파일 모두 동일)

| 설정 항목 | 값 |
|----------|----|
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | **Multiple** |
| Pixels Per Unit | 16 (픽셀아트 기준) |
| Filter Mode | **Point (no filter)** |
| Compression | None |

#### 슬라이싱 방법

1. Sprite Editor 열기 → Slice → Type: **Grid By Cell Count**
2. `Pawn_Idle.png`: 행·열 수를 확인하여 입력 (에디터에서 이미지 크기 ÷ 셀 크기)
3. `Pawn_Run.png`: 동일 방법
4. Apply

> 슬라이싱 후 각 스프라이트에 `Pawn_Idle_0`, `Pawn_Idle_1` … 형태의 이름이 자동 부여된다.

---

### Step 2 — Animator Controller 생성 (Unity MCP / 에디터 작업)

`Assets/Animations/Player/` 폴더를 생성하고 Animator Controller를 만든다.

#### 2-A: 파일 생성

- 파일명: `PlayerAnimator.controller`
- 경로: `Assets/Animations/Player/PlayerAnimator.controller`

#### 2-B: 상태(State) 구성

| 상태 | 애니메이션 클립 | 기본 상태 |
|------|----------------|----------|
| `Idle` | `Pawn_Idle` (슬라이싱된 스프라이트 시퀀스) | ✅ 기본 |
| `Run` | `Pawn_Run` (슬라이싱된 스프라이트 시퀀스) | - |

#### 2-C: 파라미터 및 전환 조건

| 파라미터 | 타입 | 용도 |
|---------|------|------|
| `isMoving` | Bool | 이동 중 여부 |

| 전환 | 조건 | Has Exit Time |
|------|------|---------------|
| Idle → Run | `isMoving = true` | false |
| Run → Idle | `isMoving = false` | false |

#### 2-D: 애니메이션 클립 설정

각 상태를 더블클릭 → Animation 창에서:
- `Pawn_Idle` 클립: 슬라이싱된 Idle 스프라이트를 타임라인에 배치, Loop Time ✅
- `Pawn_Run` 클립: 슬라이싱된 Run 스프라이트를 타임라인에 배치, Loop Time ✅

---

### Step 3 — PlayerView.cs 코드 수정 (코드 작업)

`Animator`를 SerializeField로 추가하고, 이동 이벤트에서 파라미터를 갱신한다.

```csharp
using UnityEngine;

public class PlayerView : CharacterView
{
    [SerializeField] private Animator animator;

    private static readonly int IsMoving = Animator.StringToHash("isMoving");
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

    private void OnMoveRequested(Vector2 direction)
    {
        animator.SetBool(IsMoving, direction.sqrMagnitude > 0.01f);
        Movement.Move(direction);
    }

    private void OnDestroy() => Unsubscribe();
}
```

변경 사항:
- `[SerializeField] private Animator animator` 추가
- `IsMoving` 해시 캐시 (문자열 룩업 비용 제거)
- `OnMoveRequested`에서 방향 크기로 이동 여부 판별 후 `SetBool` 호출

---

### Step 4 — PlayerView 프리팹 생성 (Unity MCP / 에디터 작업)

#### 4-A: 프리팹 생성

1. Hierarchy에 빈 GameObject → 이름: `PlayerView`
2. 컴포넌트 추가:
   - `PlayerView` 스크립트
   - `UnitMovement` 스크립트

#### 4-B: Visual 자식 생성

`PlayerView` 하위에 빈 GameObject → 이름: `Visual`

컴포넌트 추가:
- `SpriteRenderer`
  - Sprite: `Pawn_Idle_0` (기본 스프라이트, 임시)
  - Order in Layer: 1
- `Animator`
  - Controller: `PlayerAnimator.controller` (Step 2에서 생성)

#### 4-C: SerializeField 연결

`PlayerView` 오브젝트 선택 → Inspector:

| 필드 | 연결 대상 |
|------|----------|
| `movement` (CharacterView) | `PlayerView` 루트의 `UnitMovement` |
| `animator` (PlayerView) | `Visual` 자식의 `Animator` |

#### 4-D: UnitMovement 초기값 설정

`PlayerView` 루트의 `UnitMovement` 선택:
- `MoveSpeed`: `5` (런타임에 GameController가 덮어쓸 수 있으므로 기본값)

#### 4-E: 프리팹 저장

`Assets/Prefabs/Player/PlayerView.prefab`으로 저장

---

### Step 5 — PlayPage 프리팹 통합 및 카메라 연결 (Unity MCP / 에디터 작업)

#### 5-A: PlayPage 프리팹에 PlayerView 배치

1. `Assets/Resources/PlayPage.prefab` 열기 (Prefab Mode)
2. `WorldMapRoot` 하위에 `PlayerView.prefab`을 Nested Prefab으로 배치
3. Position: `(0, 0, 0)` (맵 생성 후 적절한 스폰 위치로 조정 예정)

#### 5-B: PlayPage SerializeField 연결

`PlayPage` 루트 오브젝트 → Inspector:

| 필드 | 연결 대상 |
|------|----------|
| `playerView` | `WorldMapRoot/PlayerView` |

#### 5-C: QuarterViewCamera 연결

씬의 `Cameras` 하위 카메라 오브젝트에 `QuarterViewCamera` 컴포넌트 확인:

| 필드 | 연결 대상 | 설정값 |
|------|----------|--------|
| `target` | `PlayerView` 루트 Transform | - |
| `offset` | - | `(0, 5, -7)` (쿼터뷰 기준 초기값) |
| `smoothSpeed` | - | `5` |

> `offset`은 쿼터뷰 카메라 각도에 따라 플레이 테스트 후 조정한다.

---

## 검증 체크리스트

- [ ] `Pawn_Idle.png`, `Pawn_Run.png`가 Multiple 모드로 슬라이싱됨
- [ ] 슬라이싱된 스프라이트가 Sprite Editor에서 개별 프레임으로 분리됨
- [ ] `PlayerAnimator.controller`에 `Idle`, `Run` 상태 존재
- [ ] `isMoving` Bool 파라미터 및 전환 조건 설정됨
- [ ] 각 클립에 스프라이트 시퀀스가 할당되고 Loop Time이 활성화됨
- [ ] `PlayerView.cs`에 `animator` SerializeField 및 `SetBool` 호출 추가됨
- [ ] `PlayerView.prefab`에 `PlayerView` + `UnitMovement` 컴포넌트 존재
- [ ] `Visual` 자식에 `SpriteRenderer` + `Animator` 존재
- [ ] `CharacterView.movement` → 루트 `UnitMovement` 연결됨
- [ ] `PlayerView.animator` → `Visual`의 `Animator` 연결됨
- [ ] `PlayerView.prefab`이 `Assets/Prefabs/Player/` 경로에 저장됨
- [ ] `PlayPage.prefab`의 `playerView` 필드가 PlayerView 인스턴스와 연결됨
- [ ] `QuarterViewCamera.target`이 PlayerView Transform으로 연결됨
- [ ] Play Mode에서 WASD/방향키 입력 시 캐릭터가 이동함
- [ ] 이동 중 Run 애니메이션, 정지 시 Idle 애니메이션이 전환됨
- [ ] 카메라가 플레이어를 부드럽게 추적함

---

## 작업 분류

| Step | 방법 | 선행 조건 |
|------|------|----------|
| Step 1 — 스프라이트 슬라이싱 | Unity MCP / 에디터 | Unity MCP 연결 |
| Step 2 — Animator Controller | Unity MCP / 에디터 | Step 1 완료 |
| Step 3 — PlayerView.cs 코드 수정 | 코드 편집 | 없음 (즉시 가능) |
| Step 4 — PlayerView 프리팹 생성 | Unity MCP / 에디터 | Step 2, 3 완료 |
| Step 5 — PlayPage 통합 및 카메라 연결 | Unity MCP / 에디터 | Step 4 완료 |

---

## 관련 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/04.Game/01.Entity/Player/PlayerView.cs` | (수정 대상) Animator 제어 추가 |
| `Assets/Scripts/04.Game/01.Entity/Common/CharacterView.cs` | UnitMovement SerializeField 보유 |
| `Assets/Scripts/04.Game/01.Entity/Common/UnitMovement.cs` | transform.Translate 이동 |
| `Assets/Scripts/04.Game/01.Entity/Player/QuarterViewCamera.cs` | 플레이어 추적 카메라 |
| `Assets/Graphic/Sprites/Units/Blue Units/Pawn/Pawn_Idle.png` | Idle 스프라이트 시트 |
| `Assets/Graphic/Sprites/Units/Blue Units/Pawn/Pawn_Run.png` | Run 스프라이트 시트 |
| `Assets/Animations/Player/PlayerAnimator.controller` | (생성 대상) |
| `Assets/Prefabs/Player/PlayerView.prefab` | (생성 대상) |
| `Assets/Resources/PlayPage.prefab` | (수정 대상) playerView 연결 |
