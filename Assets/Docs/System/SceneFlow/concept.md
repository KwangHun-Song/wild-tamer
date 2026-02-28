# Scene 플로우 시스템 - 컨셉

## 개요

씬 진입 시 초기화, 리소스 로딩, 페이지 진입 등 일련의 플로우를 상태 트리로 관리한다. 각 상태(State)는 **MonoBehaviour 컴포넌트**로 구현되며, 하이에라키의 부모-자식 관계가 곧 트리 구조가 된다. 상태 트리 전체는 **하나의 프리팹**으로 관리되어, 플로우 변경이 프리팹 편집만으로 가능하다.

## 핵심 개념

### 1. SceneState (MonoBehaviour 컴포넌트)

각 상태는 MonoBehaviour를 상속받는 컴포넌트이다. 하이에라키의 자식 오브젝트에 붙은 SceneState가 곧 자식 상태가 된다.

```csharp
public abstract class SceneState : MonoBehaviour
{
    // 이 상태에 진입할 수 있는지 판단한다.
    // false를 반환하면 이 상태와 그 자식들을 건너뛴다.
    public virtual bool CanEnter() => true;

    // 상태의 핵심 로직을 비동기로 실행한다.
    // 자신의 작업 완료 후 자식 상태들이 DFS로 실행된다.
    protected abstract UniTask OnExecuteAsync();
}
```

- `CanEnter()`: 상태 진입 조건을 판단한다. false이면 이 상태 및 모든 자식 상태를 건너뛴다.
- `OnExecuteAsync()`: 상태의 실제 로직을 수행한다. 비동기로 동작하여 로딩이나 연출을 자연스럽게 처리한다.

### 2. 상태 트리 (State Tree)

상태들은 하이에라키의 부모-자식 관계로 트리를 구성한다. 실행 시 DFS(깊이 우선 탐색) 방식으로 재귀 실행된다.

```
실행 흐름:
SceneState.Execute()
  → CanEnter() 확인
  → false이면 스킵 (자식 포함)
  → true이면 OnExecuteAsync() 실행
  → 자식[0].Execute() (재귀)
  → 자식[1].Execute() (재귀)
  → ...
  → 모든 자식 완료 → 자신도 완료
```

### 3. 상태 트리 프리팹

상태 트리 전체가 **하나의 프리팹**으로 관리된다.

- 개별 상태가 프리팹이 아니라, 상태들의 하이에라키 전체가 하나의 프리팹이다.
- 상태의 추가/제거/순서 변경이 프리팹 편집만으로 가능하다.
- 씬 파일을 수정하지 않고도 플로우를 변경할 수 있다.

### 4. SceneStateMachine

상태 트리의 실행을 관리하며, Notifier를 통해 상태 변경 이벤트를 외부에 전달한다.

```csharp
public class SceneStateMachine : MonoBehaviour
{
    public Notifier Notifier { get; } = new();

    // 루트 상태부터 DFS 실행을 시작한다.
    public async UniTask ExecuteAsync() { ... }
}
```

- Notifier를 통해 상태 진입/완료 등의 이벤트를 구독할 수 있다.
- 로딩 화면, 프로그레스 바 등이 상태 이벤트를 구독하여 동작한다.

### 5. SceneLauncher

씬 진입 시 가장 먼저 실행되는 진입점이다.

- 씬에 배치된 SceneStateMachine을 참조하고, 실행을 시작시킨다.
- SceneLauncher 자체는 씬에 놓이는 컴포넌트이다.

## PlayScene 하이에라키

```
PlayScene
├── SceneLauncher              ← 씬 진입점
├── SceneStateMachine          ← 상태 트리 관리자
│   └── States (프리팹)        ← 상태 트리 프리팹 루트
│       ├── InitState
│       ├── LoadState
│       ├── EnterPageState
│       ├── PendingRewardState
│       ├── ContentsState
│       │   ├── DailyRewardState
│       │   └── ...
│       ├── InPlayState
│       └── NextSceneState
├── Cameras
│   ├── UICamera               ← UI 전용 카메라 (상위 렌더 순서)
│   └── MainCamera             ← 기본 카메라
├── PageRoot                   ← 페이지 인스턴스 생성 위치
└── PopupRoot                  ← 팝업 인스턴스 생성 위치
```

## PlayScene 상태 목록

| # | 상태 | CanEnter 조건 | 역할 |
|---|------|--------------|------|
| 1 | **InitState** | 항상 true | 씬 초기화, 서비스 바인딩, 이벤트 구독 등 |
| 2 | **LoadState** | 항상 true | 필요한 리소스/데이터 로딩 (로딩 UI 표시) |
| 3 | **EnterPageState** | 항상 true | 메인 페이지(UI) 진입 및 표시 |
| 4 | **PendingRewardState** | 미수령 보상이 있을 때 | 보상 팝업 표시 (없으면 스킵) |
| 5 | **ContentsState** | 미확인 컨텐츠가 있을 때 | 자식 상태들로 개별 컨텐츠 처리 |
| 5-1 | ├── DailyRewardState | 일일 보상 미수령 시 | 일일 보상 팝업 |
| 5-2 | └── (기타 컨텐츠) | 각 컨텐츠 조건 | 이벤트, 공지 등 |
| 6 | **InPlayState** | 항상 true | 실제 게임 플레이 루프 (유저 조작 대기) |
| 7 | **NextSceneState** | 씬 전환 조건 충족 시 | 다음 씬으로 이동 처리 |

## 3단계 구조 분석

PlayScene의 상태 흐름은 크게 3단계로 나뉜다.

### 진입 흐름 (Entry Flow)
`InitState → LoadState → EnterPageState`

씬 진입 시 항상 순서대로 실행되는 초기화 흐름이다. CanEnter가 항상 true이므로 스킵 없이 실행된다.

### 메인 루프 (Main Loop)
`PendingRewardState → ContentsState → InPlayState`

유저에게 보여줄 컨텐츠를 처리한 후 실제 게임 플레이에 진입하는 구간이다. PendingRewardState와 ContentsState는 CanEnter 조건에 따라 스킵될 수 있어, 불필요한 팝업 없이 바로 게임으로 진입할 수도 있다.

### 종료 흐름 (Exit Flow)
`NextSceneState`

게임 플레이가 끝나면 다음 씬으로 이동하는 종료 처리이다.

## 설계 의견

### 상태 실행의 단방향성

현재 설계는 DFS 기반 단방향 실행이므로, 상태 트리는 한 번 앞으로만 진행한다. InPlayState에서 게임이 끝나면 NextSceneState로 진행되어 씬이 전환된다. 만약 게임 오버 후 같은 씬에서 재시작해야 한다면, InPlayState 내부에서 루프를 처리하거나 상태 트리 자체를 재실행하는 방식을 고려해야 한다.

### ContentsState의 자식 확장

ContentsState는 CanEnter를 통해 자식 상태들을 선택적으로 실행한다. 새로운 컨텐츠 추가 시 자식 상태를 프리팹에 추가하고 CanEnter 조건만 구현하면 되므로, 컨텐츠 확장이 용이하다.

### Notifier 활용

SceneStateMachine의 Notifier를 통해 외부 시스템이 플로우 진행 상황을 추적할 수 있다. 예를 들어:
- 로딩 화면이 LoadState 진입/완료를 구독하여 프로그레스 바를 표시
- 분석 시스템이 각 상태 진입 시점을 로깅
- 디버그 UI가 현재 상태를 실시간으로 표시
