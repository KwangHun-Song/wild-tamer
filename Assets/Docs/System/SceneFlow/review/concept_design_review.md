# SceneFlow - 컨셉/설계/구현 리뷰

## 컨셉 문서 (`concept.md`) 리뷰

### 작업 컨벤션 충족도

| 항목 | 상태 | 비고 |
|------|------|------|
| 시스템의 목적과 역할 | O | "씬 진입 시 초기화, 리소스 로딩, 페이지 진입 등 일련의 플로우를 상태 트리로 관리" |
| 유저 관점의 동작 설명 | O | PlayScene 상태 목록 표와 3단계 구조 분석으로 동작 흐름이 명확함 |
| 레퍼런스 자료 | X | 별도 레퍼런스 없음 (유사 패턴: Sequence Pattern, Behavior Tree 등 언급 가능) |
| 다른 시스템과의 관계 | △ | Notifier 활용은 언급하나, Facade와의 관계가 불명확 |

### 긍정적인 점

- **MonoBehaviour + 하이에라키 = 시각적 편집**: 상태를 컴포넌트로, 부모-자식 관계를 트리 구조로 활용하는 것은 Unity의 강점을 잘 살린 설계. 별도 에디터 툴 없이 하이에라키 창에서 플로우를 직관적으로 파악하고 편집할 수 있음
- **CanEnter()를 통한 조건부 스킵**: 조건에 따라 서브트리 전체를 건너뛰는 설계가 깔끔함. 새 컨텐츠 추가 시 자식 상태를 프리팹에 추가하고 CanEnter()만 구현하면 되므로 확장성이 좋음
- **3단계 구조 분석이 명확**: Entry Flow / Main Loop / Exit Flow로 나눈 분석이 플로우의 의도를 잘 전달함
- **프리팹 기반 관리**: 씬 파일을 건드리지 않고 프리팹 편집으로 플로우를 변경할 수 있는 점이 실용적

### 개선 제안

1. **InPlayState의 완료 조건 모호** — "실제 게임 플레이 루프 (유저 조작 대기)"라고만 되어 있고, DFS 단방향 실행에서 게임 플레이가 끝나는 시점을 어떻게 판단하는지(이벤트? UniTaskCompletionSource?) 설명이 없음

2. **기존 FSM과의 차별점 설명 부족** — 프로젝트에 이미 `FiniteStateMachine` 모듈이 있는데, SceneFlow가 왜 별도 시스템으로 필요한지 컨셉 문서 내에서 직접 언급하면 이해도가 높아짐

---

## 설계 문서 (`design.md`) 리뷰

### 작업 컨벤션 충족도

| 항목 | 상태 | 비고 |
|------|------|------|
| 핵심 클래스 및 인터페이스 구조 | O | SceneState, SceneStateMachine, SceneLauncher, 이벤트 인터페이스 2개 정의 |
| 클래스 간 의존 관계 및 데이터 흐름 | O | 의존성 구조 다이어그램 + 실행 흐름 상세 기술 |
| 사용할 디자인 패턴 | O | DFS 트리 탐색, Pub/Sub(Notifier), Template Method(OnExecuteAsync) |
| 외부 시스템과의 인터페이스 정의 | O | Facade, PageChanger, PopupManager와의 연동 방식 + 원칙 명시 |

### 긍정적인 점

- **FSM과의 관계 정리가 명확**: "FSM은 제네릭 기반 범용 상태 머신, SceneFlow는 씬 플로우 전용 DFS 실행기"로 역할 구분이 잘 되어 있음
- **SetUp / Execute 2단계 분리**: 모든 상태가 SetUp 완료된 후 Execute가 시작되어, 실행 중 참조 안전성이 보장됨
- **직접 자식 탐색 근거**: `GetComponentsInChildren` 대신 직접 자식만 수집하는 이유가 논리적으로 잘 설명됨
- **이벤트 분리 근거**: FSM의 `IStateChangeEvent`(from/to 동시)와 달리 Enter/Exit를 분리한 이유(비동기 실행의 긴 시간 간격)가 설득력 있음
- **연동 원칙**: "프레임워크는 외부에 의존하지 않고, 구현체에서 의존한다"는 원칙이 깔끔함
- **사용 예시가 풍부**: PlayScene 상태 구현, 하이에라키, 이벤트 구독 예시가 모두 포함되어 있어 이해하기 쉬움

### 개선 제안

1. **에러 전파 전략 미정의 (중요)**
   - `OnExecuteAsync()`에서 예외 발생 시의 동작이 정의되어 있지 않음
   - 전체 플로우 중단인지, 해당 상태만 스킵인지, 재시도인지 기본 정책이 필요

2. **프리팹의 씬 오브젝트 참조에 대한 보충 설명 권장**
   - 상태 트리가 프리팹인데, 프리팹 에셋 자체는 씬 오브젝트를 직접 참조할 수 없음
   - 단, 설계 의도대로 프리팹을 씬에 배치하면 **인스턴스 오버라이드**로 씬 오브젝트를 참조할 수 있음
   - `EnterPageState`가 PageChanger를 SerializeField로 참조하는 예시는 씬 인스턴스에서 연결하는 방식이므로 실제 동작에 문제는 없지만, 문서에 이 점을 명시하면 혼동을 줄일 수 있음

---

## 구현 코드 리뷰

### 리뷰 대상 파일

| 파일 | 경로 |
|------|------|
| `SceneState.cs` | `Modules/Base/Runtime/Scripts/SceneFlow/SceneState.cs` |
| `SceneStateMachine.cs` | `Modules/Base/Runtime/Scripts/SceneFlow/SceneStateMachine.cs` |
| `SceneLauncher.cs` | `Modules/Base/Runtime/Scripts/SceneFlow/SceneLauncher.cs` |
| `ISceneStateEnterEvent.cs` | `Modules/Base/Runtime/Scripts/SceneFlow/ISceneStateEnterEvent.cs` |
| `ISceneStateExitEvent.cs` | `Modules/Base/Runtime/Scripts/SceneFlow/ISceneStateExitEvent.cs` |

### 긍정적인 점

- **설계 문서와 구현의 일관성이 높음**: 전체 구조, 실행 흐름, 이벤트 인터페이스가 설계 문서와 거의 일치함
- **internal ExecuteAsync() 래퍼**: 설계 문서에서는 `OnExecuteAsync()`를 직접 호출하는 것처럼 보이지만, 실제 구현은 `internal ExecuteAsync()`로 감싸서 `OnExecuteAsync()`를 `protected abstract`로 유지. 같은 어셈블리(Base.Runtime) 내에서만 호출 가능하도록 캡슐화가 잘 되어 있음
- **CollectDirectChildStates 통합**: 설계 문서에서 `CollectDirectChildStates()`와 `CollectDirectChildStatesOf(Transform)`으로 분리되어 있던 것을 `CollectDirectChildStates(Transform parent)` 하나로 통합. 코드 중복이 없음
- **코딩 컨벤션 준수**: 네이밍, 네임스페이스(`Base`), 파일 구성이 프로젝트 컨벤션에 부합함

### 이슈

#### 1. SceneLauncher의 async void Start() 예외 처리 부재 (중요도: 높음)

**파일**: `SceneLauncher.cs:10-13`

```csharp
private async void Start()
{
    await stateMachine.ExecuteAsync();
}
```

`async void`에서 예외가 발생하면 Unity의 UnitySynchronizationContext로 전달되어 콘솔에 에러 로그가 출력된다. 앱이 크래시하지는 않지만, 플로우 실패 후 어떤 상태에 머물러 있는지 외부에서 알 수 없다. 전체 씬 플로우의 진입점이므로 명시적 try-catch로 실패를 제어하는 것이 좋다.

```csharp
// 제안
private async void Start()
{
    try
    {
        await stateMachine.ExecuteAsync();
    }
    catch (System.Exception e)
    {
        Debug.LogException(e, this);
    }
}
```

> 참고: 프레임워크 코드이므로 `Facade.Logger` 대신 `Debug.LogException`을 사용한다. 설계 원칙 "프레임워크는 외부에 의존하지 않는다"를 준수하기 위함.

#### 2. ExecuteStateAsync에서 ExitEvent 발행이 보장되지 않음 (중요도: 높음)

**파일**: `SceneStateMachine.cs:36-50`

```csharp
private async UniTask ExecuteStateAsync(SceneState state)
{
    if (!state.CanEnter())
        return;

    CurrentState = state;
    Notifier.Notify<ISceneStateEnterEvent>(l => l.OnSceneStateEnter(state));

    await state.ExecuteAsync();  // 여기서 예외 발생 시 ExitEvent 미발행

    var childStates = CollectDirectChildStates(state.transform);
    await ExecuteChildrenAsync(childStates);

    Notifier.Notify<ISceneStateExitEvent>(l => l.OnSceneStateExit(state));
}
```

`state.ExecuteAsync()` 또는 자식 실행 중 예외가 발생하면:
- EnterEvent는 발행되었으나 ExitEvent가 발행되지 않음
- 이벤트 구독자(로딩 화면 등)가 Enter 상태에 머물게 됨

try-finally로 ExitEvent 발행을 보장해야 한다.

#### 3. 플로우 완료 이벤트 부재 (중요도: 중간)

**파일**: `SceneStateMachine.cs:12-17`

```csharp
public async UniTask ExecuteAsync()
{
    SetUpAll();
    var children = CollectDirectChildStates(transform);
    await ExecuteChildrenAsync(children);
    // 전체 플로우 완료 이벤트 없음
}
```

개별 상태의 Enter/Exit 이벤트는 있지만, 전체 플로우가 완료되었음을 알리는 이벤트가 없다. 로딩 화면을 최종적으로 닫거나, 플로우 완료 후 로깅하는 등의 유스케이스에서 필요할 수 있다.

`ISceneFlowCompleteEvent` 또는 ExecuteAsync 완료 시점의 Notifier 알림을 고려할 수 있다.

#### 4. CurrentState가 플로우 종료 후에도 마지막 상태를 유지 (중요도: 낮음)

**파일**: `SceneStateMachine.cs:10, 41`

`CurrentState`는 상태 진입 시 갱신되지만, 모든 상태 실행이 끝난 후에도 마지막 상태를 계속 참조한다. 외부에서 `CurrentState`를 확인할 때 "현재 실행 중"과 "이미 완료됨"을 구분할 수 없다.

플로우 완료 후 `CurrentState = null`로 초기화하거나, `IsRunning` 프로퍼티를 추가하는 것을 고려할 수 있다.

#### 5. 설계 문서와 구현의 경미한 차이 (중요도: 낮음)

| 항목 | 설계 문서 | 실제 구현 |
|------|-----------|-----------|
| 자식 수집 메서드 | `CollectDirectChildStates()` + `CollectDirectChildStatesOf(Transform)` 2개 | `CollectDirectChildStates(Transform parent)` 1개로 통합 |
| 상태 실행 호출 | `state.OnExecuteAsync()` 직접 호출 | `state.ExecuteAsync()` (internal 래퍼) 경유 |

구현이 설계보다 개선된 형태이므로, 설계 문서를 구현에 맞춰 업데이트하면 문서 일관성이 높아진다.

#### 6. 테스트 부재 (중요도: 중간)

SceneFlow에 대한 테스트 코드가 없다. MonoBehaviour 기반이라 순수 유닛 테스트는 어렵지만, 최소한 PlayMode 테스트로 다음을 검증할 수 있다:
- DFS 실행 순서 검증
- CanEnter() false 시 서브트리 스킵 검증
- Notifier 이벤트 발행 순서 검증
- 예외 발생 시 동작 검증

---

## 종합 평가

| 항목 | 점수 | 설명 |
|------|------|------|
| 컨셉 설계 | **A-** | 핵심 아이디어가 명확하고 Unity에 잘 맞는 설계. InPlayState 완료 조건만 보완 필요 |
| 설계 문서 | **A** | 클래스 구조, 실행 흐름, 외부 연동, 이벤트 분리 근거가 모두 잘 정리됨. 컨벤션 충족도 높음 |
| 구현 품질 | **B+** | 설계와의 일관성이 높고 캡슐화가 잘 됨. 에러 처리 부재가 주요 감점 요소 |
| 문서-코드 일관성 | **A-** | 전체 구조가 일치하며, 차이점은 구현이 더 개선된 방향. 문서 역반영만 하면 됨 |

### 우선 보강이 필요한 2가지

1. **SceneLauncher의 async void 예외 처리** — try-catch 추가로 플로우 실패 시 명시적 에러 로깅
2. **ExecuteStateAsync의 try-finally** — ExitEvent 발행 보장
