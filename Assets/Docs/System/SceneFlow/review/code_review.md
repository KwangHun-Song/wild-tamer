# SceneFlow - PlayScene 구현 코드 리뷰

## 리뷰 대상 파일

| 파일 | 경로 |
|------|------|
| `InitState.cs` | `Scripts/01.Scene/PlayScene/InitState.cs` |
| `LoadState.cs` | `Scripts/01.Scene/PlayScene/LoadState.cs` |
| `EnterPageState.cs` | `Scripts/01.Scene/PlayScene/EnterPageState.cs` |
| `InPlayState.cs` | `Scripts/01.Scene/PlayScene/InPlayState.cs` |
| `PlayPage.cs` | `Scripts/02.Page/PlayPage.cs` |

## 씬 하이에라키 (실제 Unity 에디터 확인)

```
Play (Scene)
├── Cameras
│   ├── Main Camera          [Camera, AudioListener] (tag: MainCamera)
│   └── UICamera             [Camera]
├── SceneLauncher            [SceneLauncher]
├── SceneStateMachine        [SceneStateMachine]
│   ├── Init                 [InitState]
│   ├── Load                 [LoadState]
│   ├── EnterPage            [EnterPageState]
│   └── InPlay               [InPlayState]
├── PageRoot
└── PopupRoot
```

### 설계 문서 대비 하이에라키 비교

| 설계 문서 | 실제 구현 | 상태 |
|-----------|-----------|------|
| SceneLauncher | O | 구현됨 |
| SceneStateMachine | O | 구현됨 |
| InitState | O | 구현됨 |
| LoadState | O | 구현됨 |
| EnterPageState | O | 구현됨 |
| PendingRewardState | X | 미구현 (컨텐츠 미개발 단계이므로 적절) |
| ContentsState | X | 미구현 (컨텐츠 미개발 단계이므로 적절) |
| InPlayState | O | 구현됨 |
| NextSceneState | X | 미구현 (단일 씬 단계이므로 적절) |
| Cameras (Main + UI) | O | 구현됨 |
| PageRoot | O | 구현됨 |
| PopupRoot | O | 구현됨 |

현재 구현은 Entry Flow(Init → Load → EnterPage) + InPlay까지의 최소 플로우로, 개발 단계에 적합한 범위이다.

---

## 긍정적인 점

- **설계 문서와의 구조 일치**: 하이에라키 구성이 설계 문서의 PlayScene 레이아웃과 일치함. 상태 순서, 카메라 구성, PageRoot/PopupRoot 배치 모두 설계대로 구현됨
- **Scripts 폴더 분류가 명확**: `01.Scene/PlayScene/`에 씬별 상태들, `02.Page/`에 페이지 클래스를 배치하여 역할별 분류가 잘 되어 있음
- **InPlayState의 CancellationToken 사용**: `this.GetCancellationTokenOnDestroy()`로 오브젝트 파괴 시 자동 취소를 구현. 프레임워크 레벨에서 누락된 CancellationToken을 구현체에서 적절히 보완함
- **EnterPageState의 씬 참조 해결**: `[SerializeField] private Transform pageRoot`로 씬 오브젝트(PageRoot)를 참조. 프리팹에서 직접 참조할 수 없는 제약을 씬 인스턴스 오버라이드로 해결한 점이 설계 의도에 부합

---

## 이슈

### 1. 네임스페이스 미사용 (중요도: 중간)

**파일**: 모든 PlayScene 상태 파일 (`InitState.cs`, `LoadState.cs`, `EnterPageState.cs`, `InPlayState.cs`, `PlayPage.cs`)

```csharp
// 현재 - 글로벌 네임스페이스
public class InitState : SceneState
```

모든 클래스가 글로벌 네임스페이스에 선언되어 있다. 프로젝트 규모가 커지면 이름 충돌 가능성이 있다. 특히 `InitState`, `LoadState` 같은 범용적인 이름은 다른 씬에서도 동일한 이름을 쓸 가능성이 높다.

```csharp
// 제안
namespace PlayScene
{
    public class InitState : SceneState { ... }
}
```

또는 클래스명 자체에 씬 구분을 포함하는 방법도 있다 (e.g., `PlayInitState`). 프로젝트 네임스페이스 컨벤션이 정해지면 그에 따르면 된다.

### 2. EnterPageState에서 PageChanger가 외부에 공유되지 않음 (중요도: 중간)

**파일**: `EnterPageState.cs:10-15`

```csharp
private PageChanger pageChanger;

protected override async UniTask OnExecuteAsync()
{
    pageChanger = new PageChanger(pageRoot);
    await pageChanger.ChangePageAsync("PlayPage");
}
```

`PageChanger`가 `EnterPageState`의 private 필드로만 존재한다. 이후 다른 상태나 시스템에서 페이지 전환이 필요할 때(예: InPlayState에서 결과 페이지로 전환) 이 PageChanger에 접근할 수 없다.

고려할 수 있는 방안:
- Facade에 등록: `Facade.PageChanger = pageChanger` (단, IPageChanger로 접근)
- SceneStateMachine을 통해 공유 데이터를 전달하는 메커니즘 활용
- InitState에서 생성하여 씬 레벨에서 관리

### 3. 페이지명 "PlayPage" 하드코딩 (중요도: 낮음)

**파일**: `EnterPageState.cs:15`

```csharp
await pageChanger.ChangePageAsync("PlayPage");
```

페이지명이 문자열 리터럴로 하드코딩되어 있다. 오타나 리네이밍 시 컴파일 타임에 잡히지 않는다. 현재 단계에서는 문제없지만, 페이지가 많아지면 상수 또는 enum으로 관리하는 것을 고려할 수 있다.

### 4. InitState/LoadState가 실제 로직 없이 로그만 출력 (중요도: 낮음)

**파일**: `InitState.cs:7-11`, `LoadState.cs:7-11`

```csharp
protected override async UniTask OnExecuteAsync()
{
    Debug.Log("[PlayScene] InitState: 씬 초기화 완료");
    await UniTask.CompletedTask;
}
```

현재는 스텁(stub) 구현이다. 개발 초기 단계에서 플로우 검증 목적으로는 적절하지만, 향후 실제 로직이 추가될 때 다음을 고려해야 한다:
- `InitState`: Facade 서비스 바인딩, 이벤트 구독 등
- `LoadState`: 리소스 로딩, 로딩 UI 표시/숨김

이 이슈는 현재 단계에서는 정상이며, 기능 구현 시 자연스럽게 해소된다.

### 5. PlayPage가 최소 구현 (중요도: 낮음)

**파일**: `PlayPage.cs:3-6`

```csharp
public class PlayPage : Page
{
    public override string PageName => "PlayPage";
}
```

`Page` 베이스 클래스의 `ShowAsync`/`Hide`를 오버라이드하지 않아 기본 동작(SetActive)만 수행한다. 현재 단계에서는 적절하나, 실제 UI 요소가 추가되면 `ShowAsync`에서 UI 초기화 로직이 필요하다.

---

## 설계 일관성

| 항목 | 설계 문서 | 실제 구현 | 판단 |
|------|-----------|-----------|------|
| 상태 트리 프리팹 | 프리팹으로 관리 | 씬에 직접 배치 | 현재 단계에서 적절. 프리팹화는 플로우 확정 후 |
| EnterPageState의 PageChanger | SerializeField로 참조 | OnExecuteAsync에서 new 생성 | 접근 방식 차이 (아래 분석) |
| 연동 원칙 | 프레임워크는 외부에 무의존 | 구현체에서 Base 모듈 사용 | 원칙 준수 |

**EnterPageState의 PageChanger 생성 방식 분석:**

설계 문서에서는 `[SerializeField] private PageChanger pageChanger`로 외부에서 주입하는 형태였으나, 실제 구현에서는 `OnExecuteAsync()` 내에서 `new PageChanger(pageRoot)`로 직접 생성한다. `PageChanger`가 `MonoBehaviour`가 아닌 일반 클래스이므로 SerializeField로 노출할 수 없어, 대신 `pageRoot`(Transform)를 SerializeField로 받아 런타임에 생성하는 방식을 택한 것이다. 합리적인 판단이다.

---

## 종합 평가

| 항목 | 점수 | 설명 |
|------|------|------|
| 설계 일관성 | **A** | 하이에라키 구조, 상태 순서, 외부 연동 모두 설계 문서와 일치 |
| 코딩 컨벤션 | **B+** | 네이밍, 코드 스타일 양호하나 네임스페이스 미사용 |
| 구현 완성도 | **B** | 최소 플로우 동작 확인 수준. 스텁 상태가 다수이나 단계에 적합 |
| 확장성 | **B** | PageChanger 공유 방안, 페이지명 관리 등 확장 시 고려 필요 |

### 우선 고려 사항 2가지

1. **네임스페이스 도입** — 씬별 상태 클래스의 이름 충돌 방지를 위해 네임스페이스 컨벤션 수립
2. **PageChanger 공유 전략** — 다른 상태/시스템에서 페이지 전환이 필요할 때의 접근 방식 결정
