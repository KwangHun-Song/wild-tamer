# Scene 플로우 시스템 - 설계

## 시스템 개요

Scene 플로우 시스템은 씬 진입 시 초기화, 리소스 로딩, 페이지 진입 등 일련의 플로우를 **상태 트리**로 관리한다. 각 상태(SceneState)는 MonoBehaviour 컴포넌트로 구현되며, 하이에라키의 부모-자식 관계가 곧 트리 구조가 된다. 상태 트리 전체는 하나의 프리팹으로 관리되어, 플로우 변경이 프리팹 편집만으로 가능하다.

기존 FiniteStateMachine 모듈이 제네릭 기반의 범용 FSM이라면, SceneFlow는 **씬 단위 비동기 플로우 전용** 시스템이다. DFS(깊이 우선 탐색) 기반 단방향 실행으로, 씬 진입부터 게임 플레이 진입까지의 순차적 흐름을 처리한다.

---

## 모듈 배치

SceneFlow는 **Base 모듈 내부**에 배치한다. PageChanger, PopupManager와 동일하게 재사용 가능한 프레임워크 레벨의 시스템이기 때문이다.

```
Modules/Base/Runtime/Scripts/
├── Facade/
├── PageChanger/
├── PopupManager/
├── Notifier/
├── SceneFlow/          ← 여기에 배치
├── Utility/
└── Extensions/
```

**배치 근거:**

| 기준 | SceneFlow | 비교 대상 |
|------|-----------|-----------|
| 재사용성 | 어떤 씬에든 적용 가능 | PageChanger, PopupManager |
| 게임 로직 의존 | 없음 (프레임워크) | Facade 인터페이스들 |
| Base 내 의존성 | Notifier, IListener | FSM도 Notifier에 의존 |

---

## 폴더 구조

```
Modules/Base/Runtime/Scripts/SceneFlow/
├── SceneState.cs
├── SceneStateMachine.cs
├── SceneLauncher.cs
├── ISceneStateEnterEvent.cs
└── ISceneStateExitEvent.cs
```

| 파일 | 역할 |
|------|------|
| `SceneState.cs` | 개별 상태의 추상 베이스 클래스 |
| `SceneStateMachine.cs` | 상태 트리 실행 관리자, Notifier 보유 |
| `SceneLauncher.cs` | 씬 진입점, StateMachine 실행 트리거 |
| `ISceneStateEnterEvent.cs` | 상태 진입 이벤트 인터페이스 |
| `ISceneStateExitEvent.cs` | 상태 완료 이벤트 인터페이스 |

---

## 핵심 클래스 설계

### SceneState

개별 상태의 추상 베이스 클래스. MonoBehaviour를 상속하여 하이에라키에서 컴포넌트로 동작한다.

```csharp
public abstract class SceneState : MonoBehaviour
{
    public SceneStateMachine StateMachine { get; private set; }

    // SceneStateMachine이 실행 전에 호출하여 참조를 주입한다.
    public void OnSetUp(SceneStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    // 이 상태에 진입할 수 있는지 판단한다.
    // false를 반환하면 이 상태와 그 자식들을 건너뛴다.
    public virtual bool CanEnter()
    {
        return true;
    }

    // 상태의 핵심 로직을 비동기로 실행한다.
    // 자신의 작업 완료 후 자식 상태들이 DFS로 실행된다.
    protected abstract UniTask OnExecuteAsync();
}
```

**설계 포인트:**
- `OnSetUp()`: SceneStateMachine이 실행 전에 호출한다. 모든 상태가 StateMachine 참조를 통해 Notifier 등에 접근할 수 있다.
- `CanEnter()`: 기본값 true. 오버라이드하여 조건부 실행을 구현한다. false이면 자식 상태 포함 전체 스킵.
- `OnExecuteAsync()`: protected abstract로, 외부에서 직접 호출하지 않는다. SceneStateMachine이 내부적으로 실행 흐름을 제어한다.

### SceneStateMachine

상태 트리의 실행을 관리하는 MonoBehaviour. 직접 자식 Transform에서 SceneState를 수집하고, DFS로 재귀 실행한다.

```csharp
public class SceneStateMachine : MonoBehaviour
{
    public Notifier Notifier { get; } = new();
    public SceneState CurrentState { get; private set; }

    public async UniTask ExecuteAsync()
    {
        SetUpAll();
        var children = CollectDirectChildStates();
        await ExecuteChildrenAsync(children);
    }

    private void SetUpAll()
    {
        var allStates = GetComponentsInChildren<SceneState>(true);
        foreach (var state in allStates)
        {
            state.OnSetUp(this);
        }
    }

    private List<SceneState> CollectDirectChildStates()
    {
        var states = new List<SceneState>();
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent<SceneState>(out var state))
            {
                states.Add(state);
            }
        }
        return states;
    }

    private async UniTask ExecuteChildrenAsync(List<SceneState> children)
    {
        foreach (var child in children)
        {
            await ExecuteStateAsync(child);
        }
    }

    private async UniTask ExecuteStateAsync(SceneState state)
    {
        if (!state.CanEnter())
            return;

        CurrentState = state;
        Notifier.Notify<ISceneStateEnterEvent>(l => l.OnSceneStateEnter(state));

        await state.OnExecuteAsync();

        // 자식 상태들을 DFS로 재귀 실행
        var childStates = CollectDirectChildStatesOf(state.transform);
        await ExecuteChildrenAsync(childStates);

        Notifier.Notify<ISceneStateExitEvent>(l => l.OnSceneStateExit(state));
    }

    private List<SceneState> CollectDirectChildStatesOf(Transform parent)
    {
        var states = new List<SceneState>();
        foreach (Transform child in parent)
        {
            if (child.TryGetComponent<SceneState>(out var state))
            {
                states.Add(state);
            }
        }
        return states;
    }
}
```

**설계 포인트:**
- `SetUpAll()`: GetComponentsInChildren으로 **모든** SceneState를 한 번에 수집하여 OnSetUp을 호출한다. 실행 전 일괄 초기화.
- `CollectDirectChildStates()`: Transform의 **직접 자식만** 순회한다. 깊은 자식은 재귀 호출 시점에 다시 수집한다.
- `CurrentState`: 현재 실행 중인 상태를 추적한다. 디버깅 및 외부 참조용.
- `Notifier`: FSM 모듈의 StateMachine과 동일한 패턴으로 이벤트를 발행한다.

### SceneLauncher

씬의 진입점. 씬에 배치되어 SceneStateMachine의 실행을 시작한다.

```csharp
public class SceneLauncher : MonoBehaviour
{
    [SerializeField]
    private SceneStateMachine stateMachine;

    private async void Start()
    {
        await stateMachine.ExecuteAsync();
    }
}
```

**설계 포인트:**
- `SerializeField`로 SceneStateMachine을 참조한다. 인스펙터에서 드래그로 연결.
- `Start()`에서 실행을 시작한다. Awake가 아닌 Start를 사용하여 다른 컴포넌트의 Awake 초기화가 완료된 후 실행한다.
- SceneLauncher 자체는 상태 트리 외부에 위치한다. 순수한 트리거 역할만 수행.

---

## 이벤트 인터페이스

IListener 기반의 이벤트 인터페이스로, 상태 진입과 완료를 별도로 구독할 수 있다. 기존 FSM의 `IStateChangeEvent`가 from/to를 한 번에 전달하는 것과 달리, 비동기 실행의 특성상 Enter와 Exit 시점이 크게 벌어질 수 있어 **분리 설계**한다.

### ISceneStateEnterEvent

```csharp
public interface ISceneStateEnterEvent : IListener
{
    void OnSceneStateEnter(SceneState state);
}
```

상태의 `OnExecuteAsync()`가 시작되기 직전에 발행된다.

### ISceneStateExitEvent

```csharp
public interface ISceneStateExitEvent : IListener
{
    void OnSceneStateExit(SceneState state);
}
```

상태의 `OnExecuteAsync()`와 모든 자식 상태 실행이 완료된 후 발행된다.

**이벤트 분리 근거:**

| 기존 FSM | SceneFlow |
|----------|-----------|
| `IStateChangeEvent` (from, to 동시 전달) | Enter/Exit 분리 |
| 동기 전이 (즉시 from → to) | 비동기 실행 (Enter ~ Exit 사이 긴 시간) |
| from/to 쌍이 항상 의미 있음 | 개별 시점에 관심 (로딩 시작/끝 등) |

**사용 예시:**

```csharp
// 로딩 화면이 LoadState의 진입/완료를 구독하는 예시
public class LoadingScreen : MonoBehaviour, ISceneStateEnterEvent, ISceneStateExitEvent
{
    [SerializeField]
    private SceneStateMachine stateMachine;

    private void OnEnable()
    {
        stateMachine.Notifier.Subscribe(this);
    }

    private void OnDisable()
    {
        stateMachine.Notifier.Unsubscribe(this);
    }

    public void OnSceneStateEnter(SceneState state)
    {
        if (state is LoadState)
            ShowLoadingUI();
    }

    public void OnSceneStateExit(SceneState state)
    {
        if (state is LoadState)
            HideLoadingUI();
    }
}
```

**선택적 구독:** 필요한 이벤트 인터페이스만 구현하면 된다. Enter만 필요하면 `ISceneStateEnterEvent`만, 둘 다 필요하면 두 인터페이스를 모두 구현한다.

---

## 실행 흐름

### 전체 흐름

```
SceneLauncher.Start()
  → SceneStateMachine.ExecuteAsync()
    → SetUpAll()                         // 모든 SceneState에 StateMachine 참조 주입
    → CollectDirectChildStates()         // 직접 자식 SceneState 수집
    → ExecuteChildrenAsync(children)     // 순차 실행 시작
      → ExecuteStateAsync(child[0])
      → ExecuteStateAsync(child[1])
      → ...
```

### 개별 상태 실행 흐름

```
ExecuteStateAsync(state)
  → state.CanEnter()
    → false → return (자식 포함 스킵)
    → true  → 계속 진행
  → CurrentState = state
  → Notify ISceneStateEnterEvent
  → state.OnExecuteAsync()              // 상태의 핵심 로직 실행
  → CollectDirectChildStatesOf(state)   // 자식 SceneState 수집
  → ExecuteChildrenAsync(childStates)   // 자식들을 DFS 재귀 실행
  → Notify ISceneStateExitEvent
```

### SetUp → Execute 2단계 분리

실행은 **SetUp 단계**와 **Execute 단계**로 분리된다.

1. **SetUp 단계**: `GetComponentsInChildren<SceneState>(true)`로 트리 내 모든 SceneState를 한 번에 수집하고, 각각에 `OnSetUp(this)`을 호출하여 StateMachine 참조를 주입한다.
2. **Execute 단계**: 루트부터 DFS로 순차 실행한다. 각 상태는 `CanEnter()` 확인 → `OnExecuteAsync()` 실행 → 자식 재귀 순서로 진행한다.

이렇게 분리하면, 어떤 상태가 `OnExecuteAsync()` 내에서 다른 상태를 참조하거나 Notifier를 사용할 때 이미 SetUp이 완료된 상태이므로 안전하다.

---

## 직접 자식 탐색

상태 트리의 자식 수집 시, `GetComponentsInChildren`이 아닌 **Transform 직접 자식만 순회**한다.

### 탐색 방식

```csharp
// 직접 자식만 수집
foreach (Transform child in parent)
{
    if (child.TryGetComponent<SceneState>(out var state))
    {
        states.Add(state);
    }
}
```

### 근거

`GetComponentsInChildren`은 모든 하위 계층의 컴포넌트를 평면 리스트로 반환한다. 트리 구조를 유지하려면 각 노드에서 **자신의 직접 자식만** 알면 된다. 깊은 자식은 재귀 호출 시점에 해당 부모가 직접 수집한다.

```
ContentsState (직접 자식: DailyReward, EventState)
├── DailyRewardState       ← ContentsState가 수집
└── EventState             ← ContentsState가 수집
    └── SubEventState      ← EventState가 수집 (ContentsState는 모름)
```

### SetUp 시에만 GetComponentsInChildren 사용

SetUp 단계에서는 트리 구조와 무관하게 **모든** SceneState에 참조를 주입해야 하므로, 이 경우에만 `GetComponentsInChildren<SceneState>(true)`를 사용한다. `includeInactive: true`로 비활성 오브젝트도 포함하여, CanEnter에서 false를 반환할 수 있는 상태도 SetUp은 완료된 상태가 된다.

---

## 외부 시스템 연동

SceneFlow는 Base 모듈 내의 다른 시스템과 협력하되, 직접 의존하지 않는다.

### Facade

각 SceneState 구현체가 필요에 따라 Facade를 통해 서비스에 접근한다.

```csharp
public class LoadState : SceneState
{
    protected override async UniTask OnExecuteAsync()
    {
        // Facade를 통해 리소스 로딩
        await Facade.Loader.LoadAsync("PlaySceneResources");
    }
}
```

SceneFlow 프레임워크 자체는 Facade에 의존하지 않는다. 의존은 **구현체(각 씬의 State 클래스)** 레벨에서 발생한다.

### PageChanger

페이지 진입 상태에서 PageChanger를 사용한다. PageChanger 인스턴스는 SerializeField로 직접 참조한다.

```csharp
public class EnterPageState : SceneState
{
    [SerializeField]
    private PageChanger pageChanger;

    protected override async UniTask OnExecuteAsync()
    {
        await pageChanger.ChangePageAsync("PlayPage");
    }
}
```

### PopupManager

보상/컨텐츠 팝업 표시에 PopupManager를 사용한다.

```csharp
public class PendingRewardState : SceneState
{
    [SerializeField]
    private PopupManager popupManager;

    public override bool CanEnter()
    {
        return RewardService.HasPendingReward();
    }

    protected override async UniTask OnExecuteAsync()
    {
        var result = await popupManager.ShowAsync<RewardResult>("RewardPopup");
    }
}
```

### 연동 원칙

- SceneFlow 프레임워크(SceneState, SceneStateMachine, SceneLauncher)는 외부 시스템에 **의존하지 않는다**.
- 외부 시스템과의 연동은 각 씬별 **SceneState 구현체**에서 수행한다.
- 구현체는 SerializeField 또는 Facade를 통해 외부 시스템에 접근한다.

---

## 사용 예시

### PlayScene 상태 구현

```csharp
// InitState - 씬 초기화
public class InitState : SceneState
{
    protected override async UniTask OnExecuteAsync()
    {
        // 서비스 바인딩, 이벤트 구독 등
        GameService.Initialize();
        await UniTask.CompletedTask;
    }
}

// LoadState - 리소스 로딩
public class LoadState : SceneState
{
    protected override async UniTask OnExecuteAsync()
    {
        await ResourceLoader.LoadAllAsync();
    }
}

// EnterPageState - 메인 페이지 진입
public class EnterPageState : SceneState
{
    [SerializeField]
    private PageChanger pageChanger;

    protected override async UniTask OnExecuteAsync()
    {
        await pageChanger.ChangePageAsync("PlayPage");
    }
}

// PendingRewardState - 미수령 보상 처리 (조건부)
public class PendingRewardState : SceneState
{
    [SerializeField]
    private PopupManager popupManager;

    public override bool CanEnter()
    {
        return RewardService.HasPendingReward();
    }

    protected override async UniTask OnExecuteAsync()
    {
        var result = await popupManager.ShowAsync<RewardResult>("RewardPopup");
    }
}

// ContentsState - 컨텐츠 상위 상태 (조건부)
public class ContentsState : SceneState
{
    public override bool CanEnter()
    {
        return ContentsService.HasUnconfirmedContents();
    }

    protected override async UniTask OnExecuteAsync()
    {
        // 자식 상태(DailyRewardState 등)가 DFS로 실행된다.
        // 자신은 별도 로직 없이 자식에게 위임.
        await UniTask.CompletedTask;
    }
}

// InPlayState - 실제 게임 플레이
public class InPlayState : SceneState
{
    protected override async UniTask OnExecuteAsync()
    {
        // 게임 루프 진입. 유저 조작 대기.
        // 게임 종료 조건이 충족될 때까지 대기.
        await GameLoop.WaitUntilFinishAsync();
    }
}
```

### PlayScene 하이에라키

```
PlayScene
├── SceneLauncher              [SceneLauncher]
├── SceneStateMachine          [SceneStateMachine]
│   └── States (프리팹)
│       ├── Init               [InitState]
│       ├── Load               [LoadState]
│       ├── EnterPage          [EnterPageState]
│       ├── PendingReward      [PendingRewardState]
│       ├── Contents           [ContentsState]
│       │   ├── DailyReward    [DailyRewardState]
│       │   └── ...
│       ├── InPlay             [InPlayState]
│       └── NextScene          [NextSceneState]
├── Cameras
│   ├── UICamera
│   └── MainCamera
├── PageRoot
└── PopupRoot
```

### 이벤트 구독 예시

```csharp
// 디버그 로거 - 모든 상태 변경을 로깅
public class SceneFlowLogger : MonoBehaviour, ISceneStateEnterEvent, ISceneStateExitEvent
{
    [SerializeField]
    private SceneStateMachine stateMachine;

    private void OnEnable()
    {
        stateMachine.Notifier.Subscribe(this);
    }

    private void OnDisable()
    {
        stateMachine.Notifier.Unsubscribe(this);
    }

    public void OnSceneStateEnter(SceneState state)
    {
        Facade.Logger.Log($"[SceneFlow] Enter: {state.GetType().Name}");
    }

    public void OnSceneStateExit(SceneState state)
    {
        Facade.Logger.Log($"[SceneFlow] Exit: {state.GetType().Name}");
    }
}
```

---

## 의존성 구조

```
Game Scripts (Scripts/)
    └──→ Base Module (Modules/Base/)
             ├── Facade
             ├── PageChanger
             ├── PopupManager
             ├── Notifier ←── SceneFlow가 의존
             │   └── IListener
             └── SceneFlow
                 ├── SceneState (abstract MonoBehaviour)
                 ├── SceneStateMachine (Notifier 보유)
                 ├── SceneLauncher (진입점)
                 ├── ISceneStateEnterEvent : IListener
                 └── ISceneStateExitEvent : IListener
```

**의존 관계:**
- `SceneFlow` → `Notifier`, `IListener` (Base 모듈 내부 의존)
- `SceneFlow` → `UniTask` (비동기 처리)
- `SceneState 구현체` → `Facade`, `PageChanger`, `PopupManager` (사용처에서 의존)
- `SceneFlow`는 게임 로직(Scripts)에 의존하지 않는다.

**FSM 모듈과의 관계:**
- SceneFlow와 FiniteStateMachine은 서로 독립적이다.
- 둘 다 Base.Notifier/IListener를 사용하지만, 직접적인 의존 관계는 없다.
- FSM은 제네릭 기반의 범용 상태 머신이고, SceneFlow는 씬 플로우 전용 DFS 실행기이다.
