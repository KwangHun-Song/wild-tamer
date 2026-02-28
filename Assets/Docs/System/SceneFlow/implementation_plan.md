# Scene 플로우 시스템 - 구현 계획

## 현재 상태

### 완료

- SceneFlow 프레임워크 (SceneState, SceneStateMachine, SceneLauncher, 이벤트 인터페이스)
- Base 모듈 전체 (Facade, PageChanger, PopupManager, Notifier 등)
- Scripts 폴더 구조 (00.Common ~ 05.Utility)
- Step 1. PlayScene SceneState 구현체 (InitState, LoadState, EnterPageState, InPlayState)
- Step 2. PlayPage
- Step 3. PlayScene 씬 하이에라키 구성
- PlayPage 프리팹 (Assets/Resources/PlayPage.prefab)
- 플레이 모드 검증 완료 (DFS 순서 정상, 에러/경고 0건)

---

## 구현 범위

컨셉 문서의 7개 상태 중, 게임 시스템에 의존하지 않는 **진입 흐름 3개 + InPlayState**를 우선 구현한다. 나머지는 해당 게임 시스템 구현 시 추가한다.

| 상태 | 이번에 구현 | 사유 |
|------|:-----------:|------|
| InitState | O | 의존 없음 |
| LoadState | O | 의존 없음 (로딩 대상은 추후 추가) |
| EnterPageState | O | PageChanger 구현체 존재 |
| InPlayState | O | 게임 루프 진입점 (스텁) |
| PendingRewardState | X | 보상 시스템 미구현 |
| ContentsState | X | 컨텐츠 시스템 미구현 |
| NextSceneState | X | 다음 씬 미정 |

---

## 구현 파일 목록

### Step 1. PlayScene SceneState 구현체

| 파일 | 위치 | 역할 |
|------|------|------|
| `InitState.cs` | `Assets/Scripts/01.Scene/PlayScene/` | 씬 초기화 (로그 출력) |
| `LoadState.cs` | `Assets/Scripts/01.Scene/PlayScene/` | 리소스 로딩 (현재는 즉시 완료) |
| `EnterPageState.cs` | `Assets/Scripts/01.Scene/PlayScene/` | PageChanger로 PlayPage 진입 |
| `InPlayState.cs` | `Assets/Scripts/01.Scene/PlayScene/` | 게임 플레이 대기 (스텁) |

### Step 2. PlayPage

| 파일 | 위치 | 역할 |
|------|------|------|
| `PlayPage.cs` | `Assets/Scripts/02.Page/` | 최소 구현 Page (빈 UI) |

EnterPageState가 진입할 페이지. Page 베이스 클래스를 상속하여 최소한으로 구현한다.

### Step 3. PlayScene 씬 구성

Play.unity 씬에 다음 하이에라키를 구성한다.

```
Play
├── Cameras
│   ├── MainCamera             [Camera, AudioListener]
│   └── UICamera               [Camera]
├── SceneLauncher              [SceneLauncher]
├── SceneStateMachine          [SceneStateMachine]
│   ├── Init                   [InitState]
│   ├── Load                   [LoadState]
│   ├── EnterPage              [EnterPageState]
│   └── InPlay                 [InPlayState]
├── PageRoot                   [빈 오브젝트]
└── PopupRoot                  [빈 오브젝트]
```

> States 중간 오브젝트 없이 SceneStateMachine의 직접 자식으로 배치한다. `CollectDirectChildStates(transform)`이 직접 자식만 탐색하기 때문이다.

### Step 4. PlayPage 프리팹

PlayPage를 Resources 폴더에 프리팹으로 저장한다.

| 파일 | 위치 |
|------|------|
| `PlayPage.prefab` | `Assets/Resources/` |

---

## 작업 순서 및 의존 관계

```
Step 1 (SceneState 구현체) + Step 2 (PlayPage) — 병렬 가능 (코드 작성만)
  └→ Step 3 (씬 구성) — 모든 스크립트가 컴파일된 후 씬 편집
       └→ Step 4 (프리팹) — PlayPage를 Resources에 프리팹으로 저장
```

Scripts 폴더는 프로젝트 레이어이므로 asmdef 없이 모든 모듈을 참조할 수 있다. Step 1과 Step 2는 독립적인 코드 작성이므로 병렬 진행이 가능하다.

---

## 각 Step 상세

### Step 1. SceneState 구현체

#### InitState

```csharp
public class InitState : SceneState
{
    protected override async UniTask OnExecuteAsync()
    {
        Debug.Log("[PlayScene] InitState: 씬 초기화 완료");
        await UniTask.CompletedTask;
    }
}
```

- 현재 단계에서는 로그만 출력. 게임 시스템 추가 시 서비스 바인딩 로직을 넣는다.

#### LoadState

```csharp
public class LoadState : SceneState
{
    protected override async UniTask OnExecuteAsync()
    {
        // 로딩 대상이 추가되면 여기서 로드
        Debug.Log("[PlayScene] LoadState: 리소스 로딩 완료");
        await UniTask.CompletedTask;
    }
}
```

- 로딩 대상 리소스가 아직 없으므로 즉시 완료. 추후 리소스 추가 시 실제 로딩 로직을 넣는다.

#### EnterPageState

```csharp
public class EnterPageState : SceneState
{
    [SerializeField]
    private Transform pageRoot;

    private PageChanger pageChanger;

    protected override async UniTask OnExecuteAsync()
    {
        pageChanger = new PageChanger(pageRoot);
        await pageChanger.ChangePageAsync("PlayPage");
    }
}
```

- PageChanger는 클래스 인스턴스이므로 상태 내에서 생성한다.
- pageRoot는 씬의 PageRoot 오브젝트를 인스턴스 오버라이드로 참조한다.
- PlayPage 프리팹을 Resources에서 로드하여 표시한다.

#### InPlayState

```csharp
public class InPlayState : SceneState
{
    protected override async UniTask OnExecuteAsync()
    {
        Debug.Log("[PlayScene] InPlayState: 게임 플레이 진입");
        // 게임 루프 구현 전까지는 무한 대기
        await UniTask.WaitUntil(() => false, cancellationToken: this.GetCancellationTokenOnDestroy());
    }
}
```

- 게임 루프가 아직 없으므로, 오브젝트 Destroy 시까지 대기한다.
- 추후 게임 종료 조건을 구현하면 대기 조건을 변경한다.

### Step 2. PlayPage

```csharp
public class PlayPage : Page
{
    public override string PageName => "PlayPage";
}
```

- Page 베이스 클래스를 상속하는 최소 구현체.
- Resources/PlayPage 프리팹으로 저장하여 PageChanger가 로드할 수 있게 한다.

### Step 3. 씬 구성

Unity 에디터에서 Play.unity를 다음과 같이 구성한다.

1. 기존 오브젝트 정리
2. SceneLauncher 오브젝트 생성 + SceneLauncher 컴포넌트 추가
3. SceneStateMachine 오브젝트 생성 + SceneStateMachine 컴포넌트 추가
4. SceneStateMachine 직접 자식으로 오브젝트 생성 (Init, Load, EnterPage, InPlay) + 각 SceneState 컴포넌트 추가
5. SceneLauncher의 stateMachine 필드에 SceneStateMachine 연결
6. Cameras 구성 (MainCamera, UICamera)
7. PageRoot, PopupRoot 빈 오브젝트 생성
8. EnterPageState의 pageRoot 필드에 PageRoot 연결

### Step 4. PlayPage 프리팹

PlayPage 컴포넌트가 붙은 GameObject를 `Assets/Resources/PlayPage.prefab`으로 저장한다. PageChanger는 `Resources.Load<GameObject>(pageName)`으로 프리팹을 로드하므로 Resources 루트에 배치해야 한다.

---

## 기술적 리스크

| 리스크 | 대응 |
|--------|------|
| PageChanger가 Resources.Load로 프리팹을 찾지 못함 | PlayPage 프리팹을 정확한 Resources 경로에 배치 |
| InPlayState의 무한 대기가 씬 전환을 막음 | GetCancellationTokenOnDestroy로 씬 전환 시 자동 해제 |
| 프리팹에서 씬 오브젝트 참조 불가 | 인스턴스 오버라이드로 해결 (설계 문서에 명시) |

---

## 검증 방법

1. **컴파일 확인** — 모든 스크립트가 에러 없이 컴파일되는지 확인
2. **플레이 모드 실행** — Play.unity를 실행하여 콘솔에 상태 진입 로그가 순서대로 출력되는지 확인
   - `[PlayScene] InitState: 씬 초기화 완료`
   - `[PlayScene] LoadState: 리소스 로딩 완료`
   - `[PlayScene] InPlayState: 게임 플레이 진입`
3. **PlayPage 표시** — EnterPageState 실행 후 PageRoot 아래에 PlayPage 인스턴스가 생성되는지 확인
4. **이벤트 발행** — SceneStateMachine.Notifier를 통해 Enter/Exit 이벤트가 정상 발행되는지 확인 (디버그 로거 추가 시)
