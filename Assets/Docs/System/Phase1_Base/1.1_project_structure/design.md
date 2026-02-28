# 1.1 프로젝트 구조 - 설계

## Base 모듈 개요

Base 모듈은 프로젝트의 기반 기능을 제공한다. 핵심 서비스는 **Facade 패턴**으로 정적 접근하고, 독립적인 시스템(PageChanger, PopupManager, Notifier)은 Base 내 별도 폴더로 분리하여 관리한다. FSM(FiniteStateMachine)은 **별도 모듈**로 분리되어 Base.Runtime에 의존한다.

## 폴더 구조

```
Modules/
└── Base/
    └── Runtime/
        ├── Scripts/
        │   ├── Facade/
        │   │   ├── Interfaces/
        │   │   │   ├── IDataStore.cs
        │   │   │   ├── ISceneChanger.cs
        │   │   │   ├── IObjectPool.cs
        │   │   │   ├── ISoundManager.cs
        │   │   │   ├── ILogger.cs
        │   │   │   ├── ITimeProvider.cs
        │   │   │   ├── ISceneTransition.cs
        │   │   │   ├── IJsonSerializer.cs
        │   │   │   ├── IDatabase.cs
        │   │   │   ├── ICoroutineRunner.cs
        │   │   │   └── IInstanceLoader.cs
        │   │   ├── Facade.cs
        │   │   └── Bootstrapper.cs
        │   ├── PageChanger/
        │   │   ├── IPage.cs
        │   │   └── IPageChanger.cs
        │   ├── PopupManager/
        │   │   ├── IPopup.cs
        │   │   └── IPopupManager.cs
        │   ├── Notifier/
        │   │   ├── IListener.cs
        │   │   └── Notifier.cs
        │   ├── Utility/
        │   │   └── EnumLike.cs
        │   └── Extensions/
        └── Base.Runtime.asmdef
```

---

## Facade 초기화

### Bootstrapper

`Bootstrapper`는 게임 시작 시 Facade의 모든 서비스를 초기화하는 MonoBehaviour이다. 첫 번째 씬에 배치하여 `[RuntimeInitializeOnLoadMethod]` 또는 `Awake()`에서 실행한다.

```csharp
public class Bootstrapper : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // 1. 다른 서비스에서 사용하는 기반 서비스 우선 초기화
        Facade.Logger = new DefaultLogger();
        Facade.Json = new DefaultJsonSerializer();
        Facade.Coroutine = CreateCoroutineRunner();

        // 2. 기반 서비스에 의존하는 서비스 초기화
        Facade.Data = new DefaultDataStore();
        Facade.DB = new DefaultDatabase();
        Facade.Time = new DefaultTimeProvider();

        // 3. Unity 씬/오브젝트 관련 서비스 초기화
        Facade.Pool = new DefaultObjectPool();
        Facade.Sound = new DefaultSoundManager();
        Facade.Scene = new DefaultSceneChanger();
        Facade.Transition = new DefaultSceneTransition();
        Facade.Loader = new DefaultInstanceLoader();
    }
}
```

**초기화 순서 규칙:**
1. Logger, Json, Coroutine — 다른 서비스가 의존하는 기반 서비스
2. Data, DB, Time — 기반 서비스만 의존하는 서비스
3. Pool, Sound, Scene, Transition — Unity 오브젝트와 상호작용하는 서비스

---

## Facade 클래스

```csharp
public static class Facade
{
    public static IDataStore Data { get; set; }
    public static ISceneChanger Scene { get; set; }
    public static IObjectPool Pool { get; set; }
    public static ISoundManager Sound { get; set; }
    public static ILogger Logger { get; set; }
    public static ITimeProvider Time { get; set; }
    public static ISceneTransition Transition { get; set; }
    public static IJsonSerializer Json { get; set; }
    public static IDatabase DB { get; set; }
    public static ICoroutineRunner Coroutine { get; set; }
    public static IInstanceLoader Loader { get; set; }
}
```

- Bootstrapper가 게임 시작 시 **기본 구현체**를 할당한다.
- 테스트나 특수 상황에서 구현체를 교체(Mock 주입 등)할 수 있다.

---

## 비동기 처리 기준

프로젝트 내 비동기 처리 방식은 다음 기준으로 통일한다.

| 방식 | 사용 기준 |
|------|----------|
| **UniTask** | 비동기 흐름의 **기본 방식**. 씬 전환, 페이지/팝업 전환, 페이드 연출 등 |
| **Coroutine** | UniTask를 사용할 수 없는 레거시 연동, 또는 MonoBehaviour 생명주기와 밀접한 반복 처리 |

`ICoroutineRunner`는 UniTask를 사용할 수 없는 상황과 간단한 지연 호출(`DoSecondsAfter`, `DoFramesAfter`)을 위해 유지한다.

---

## Facade 인터페이스 설계

### IDataStore

런타임 데이터 저장 및 불러오기.

```csharp
public interface IDataStore
{
    void Save<T>(string key, T data);
    T Load<T>(string key, T defaultValue = default);
    bool HasKey(string key);
    void Delete(string key);
    void DeleteAll();
}
```

### ISceneChanger

Unity Scene 변경. 내부적으로 `ISceneTransition`을 활용하여 전환 연출을 자동으로 수행한다.

```csharp
public interface ISceneChanger
{
    UniTask ChangeSceneAsync(string sceneName);
    string CurrentScene { get; }
}
```

### ISceneTransition

화면 전환 연출만 담당. `ISceneChanger`가 내부에서 호출하거나, 페이지 전환 등에서 독립적으로도 사용 가능.

```csharp
public interface ISceneTransition
{
    UniTask TransitionInAsync();
    UniTask TransitionOutAsync();
}
```

**SceneChanger ↔ SceneTransition 관계:**

```csharp
// ISceneChanger 구현체 내부 흐름
public async UniTask ChangeSceneAsync(string sceneName)
{
    await Facade.Transition.TransitionInAsync();   // 화면 가림
    await SceneManager.LoadSceneAsync(sceneName);  // 씬 로드
    await Facade.Transition.TransitionOutAsync();  // 화면 열림
}

// ISceneTransition 단독 사용 (페이지 전환 등)
await Facade.Transition.TransitionInAsync();
await pageChanger.ChangePageAsync("InventoryPage");
await Facade.Transition.TransitionOutAsync();
```

### IObjectPool

오브젝트 풀링으로 생성/회수 최적화.

```csharp
public interface IObjectPool
{
    GameObject Spawn(GameObject prefab, Transform parent = null);
    void Despawn(GameObject obj);
    void Preload(GameObject prefab, int count);
    void ClearPool(GameObject prefab = null);
}
```

### ISoundManager

BGM, SFX 재생/정지/볼륨 조절.

```csharp
public interface ISoundManager
{
    void PlayBGM(string clipName);
    void StopBGM();
    void PlaySFX(string clipName);
    float BGMVolume { get; set; }
    float SFXVolume { get; set; }
    bool IsMuted { get; set; }
}
```

### ILogger

로그 레벨과 색상을 지정할 수 있는 로깅 시스템.

```csharp
public enum LogLevel
{
    Verbose,
    Debug,
    Info,
    Warning,
    Error
}

public enum DebugColor
{
    Default,
    Red,
    Green,
    Blue,
    Yellow,
    Cyan,
    Magenta,
    Orange,
    White,
    Gray
}

public interface ILogger
{
    LogLevel MinLogLevel { get; set; }
    void Log(string message, LogLevel level = LogLevel.Info, DebugColor color = DebugColor.Default);
}
```

### ITimeProvider

현재 시간 제공 및 치트용 시간 이동.

```csharp
public interface ITimeProvider
{
    DateTime Now { get; }
    void JumpSeconds(double seconds);
    void ResetOffset();
}
```

### IJsonSerializer

JSON 직렬화/역직렬화. DataStore 등 내부에서도 활용.

```csharp
public interface IJsonSerializer
{
    string Serialize<T>(T obj);
    T Deserialize<T>(string json);
}
```

### IDatabase

빌드 전 정적으로 정의된 데이터를 제네릭 타입으로 조회하는 읽기 전용 데이터베이스. 런타임 저장/로드를 담당하는 IDataStore와 달리, 기획 데이터(몬스터 스탯, 아이템 정보 등)를 조회하는 용도.

데이터 소스는 **ScriptableObject** 기반으로 구현한다. 각 데이터 타입마다 ScriptableObject 에셋을 만들고, IDatabase 구현체가 타입별로 로드하여 관리한다.

```csharp
public interface IDatabase
{
    T Get<T>(string id) where T : class;
    IReadOnlyList<T> GetAll<T>() where T : class;
    bool TryGet<T>(string id, out T result) where T : class;
}
```

**IDataStore vs IDatabase:**

| | IDataStore | IDatabase |
|--|-----------|-----------|
| 용도 | 런타임 저장/로드 (유저 데이터) | 정적 기획 데이터 조회 |
| 읽기/쓰기 | 읽기 + 쓰기 | 읽기 전용 |
| 데이터 소스 | PlayerPrefs / JSON 파일 | ScriptableObject |
| 예시 | 플레이어 진행 상황, 설정값 | 몬스터 스탯, 아이템 정보 |

### IInstanceLoader

Unity 인스턴스(GameObject) 로드/언로드. 구현체에 따라 Instantiate, 오브젝트 풀링, 활성화/비활성화 등 다양한 방식으로 교체 가능하다. 기본 구현은 Instantiate/Destroy 방식.

```csharp
public interface IInstanceLoader
{
    GameObject Load(GameObject prefab, Transform parent = null);
    void Unload(GameObject instance);
}
```

### ICoroutineRunner

MonoBehaviour를 상속하지 않는 클래스에서 코루틴 실행 및 지연 호출.

```csharp
public interface ICoroutineRunner
{
    Coroutine StartCoroutine(IEnumerator routine);
    void StopCoroutine(Coroutine coroutine);
    void StopAllCoroutines();
    void DoSecondsAfter(Action action, float seconds);
    void DoFramesAfter(Action action, int frames);
}
```

---

## 독립 시스템 설계

PageChanger, PopupManager, Notifier, FSM은 Facade에 포함하지 않는다. 이들은 **상태를 가지는 복합 시스템**으로, 정적 프로퍼티 하나로 노출하기보다 사용처에서 직접 인스턴스를 참조하는 것이 적합하다. (e.g., 특정 Canvas에 종속된 PageChanger, 씬마다 다른 Notifier 등)

### PageChanger

Page(UI 단위 화면) 전환 시스템.

```csharp
public interface IPage
{
    string PageName { get; }
    GameObject GetGameObject();
    UniTask ShowAsync(object param = null);
    void Hide();
    bool IsVisible { get; }
}

public interface IPageChanger
{
    UniTask ChangePageAsync(string pageName, object param = null);
    void GoBack();
    IPage CurrentPage { get; }
}
```

### PopupManager

팝업 시스템. ShowAsync로 팝업을 열고, 팝업이 닫힐 때까지 대기하며 결과값을 받을 수 있다.

```csharp
public interface IPopup
{
    string PopupName { get; }
    GameObject GetGameObject();
    UniTask ShowAsync(object enterParam = null);
    void Close(object leaveParam = null);
    bool IsOpen { get; }
}

public interface IPopupManager
{
    UniTask<T> ShowAsync<T>(string popupName, object enterParam = null);
    bool IsPopupOpen(string popupName);
}
```

**사용 예시:**

```csharp
var result = await popupManager.ShowAsync<ConfirmResult>("ConfirmPopup", new ConfirmParam
{
    Title = "정말 삭제하시겠습니까?"
});

if (result.IsConfirmed)
{
    // 확인 처리
}
```

### Notifier

이벤트 버스 역할. 시스템 간 느슨한 결합을 위한 발행/구독 패턴. `IListener` 마커 인터페이스를 상속한 이벤트 인터페이스를 정의하고, 해당 인터페이스를 구현한 객체를 Subscribe/Unsubscribe하여 사용한다.

```csharp
public interface IListener { }

public class Notifier
{
    public void Subscribe<T>(T listener) where T : IListener;
    public void Unsubscribe<T>(T listener) where T : IListener;
    public void Notify<T>(Action<T> action) where T : IListener;
}
```

**사용 예시:**

```csharp
// 이벤트 인터페이스 정의
public interface IMonsterDefeatedListener : IListener
{
    void OnMonsterDefeated(Monster monster);
}

// 구독자 구현
public class QuestTracker : MonoBehaviour, IMonsterDefeatedListener
{
    private Notifier _notifier;

    private void OnEnable()
    {
        _notifier.Subscribe(this);
    }

    private void OnDisable()
    {
        _notifier.Unsubscribe(this);
    }

    public void OnMonsterDefeated(Monster monster)
    {
        // 퀘스트 진행 처리
    }
}

// 발행
_notifier.Notify<IMonsterDefeatedListener>(l => l.OnMonsterDefeated(monster));
```

---

## FiniteStateMachine 모듈 (별도 모듈)

FSM은 **별도 모듈** (`Modules/FiniteStateMachine/`)로 분리되어 있으며, `Base.Runtime`에 의존한다. 제네릭으로 Entity 타입과 Enum 기반 Trigger를 받아, 조건/커맨드 기반 상태 전이를 지원한다. 내부적으로 `Base.Notifier`를 사용하여 상태 변경 이벤트를 발행한다.

```
Modules/
└── FiniteStateMachine/
    └── Runtime/
        ├── StateMachine.cs
        ├── State.cs
        ├── StateTransition.cs
        ├── IStateChangeEvent.cs
        └── FiniteStateMachine.Runtime.asmdef  (Base.Runtime 참조)
```

```csharp
public abstract class State<TEntity, TEnumTrigger> where TEnumTrigger : Enum
{
    public TEntity Owner { get; }
    public StateMachine<TEntity, TEnumTrigger> StateMachine { get; }
    public virtual void OnEnter() { }
    public virtual void OnExit() { }
    public virtual bool OnReceiveCommand(int inCommand, object inData) { return false; }
}

public abstract class StateMachine<TEntity, TEnumTrigger> where TEnumTrigger : Enum
{
    public TEntity Owner { get; }
    public Notifier Notifier { get; }
    public State<TEntity, TEnumTrigger> CurrentState { get; }
    protected abstract State<TEntity, TEnumTrigger> InitialState { get; }
    protected abstract State<TEntity, TEnumTrigger>[] States { get; }
    protected abstract StateTransition<TEntity, TEnumTrigger>[] Transitions { get; }
    public void SetUp();
    public void Update();
    public bool TryTransition();
    public bool ExecuteCommand(TEnumTrigger inTrigger);
}

public class StateTransition<TEntity, TEnumTrigger> where TEnumTrigger : Enum
{
    public State<TEntity, TEnumTrigger> FromState { get; }
    public State<TEntity, TEnumTrigger> ToState { get; }
    public TEnumTrigger TransitionTrigger { get; }
    public Func<State<TEntity, TEnumTrigger>, bool> TransitionCondition { get; }
    public bool IsTransferable { get; }
}

public interface IStateChangeEvent<TEntity, TEnumTrigger> : IListener where TEnumTrigger : Enum
{
    void OnStateChange(StateMachine<TEntity, TEnumTrigger> stateMachine,
        State<TEntity, TEnumTrigger> fromState, State<TEntity, TEnumTrigger> toState);
}
```

**사용 예시:**

```csharp
public enum MonsterTrigger { Detect, Lose, Attack }

public class IdleState : State<Monster, MonsterTrigger>
{
    public override void OnEnter() { /* 대기 애니메이션 */ }
}

public class MonsterFSM : StateMachine<Monster, MonsterTrigger>
{
    private readonly IdleState _idle = new();
    private readonly ChaseState _chase = new();

    protected override State<Monster, MonsterTrigger> InitialState => _idle;
    protected override State<Monster, MonsterTrigger>[] States => new[] { _idle, _chase };
    protected override StateTransition<Monster, MonsterTrigger>[] Transitions => new[]
    {
        StateTransition<Monster, MonsterTrigger>.Generate(_idle, _chase, MonsterTrigger.Detect),
        StateTransition<Monster, MonsterTrigger>.Generate(_chase, _idle, MonsterTrigger.Lose),
    };

    public MonsterFSM(Monster owner) : base(owner) { }
}
```

---

## 의존성 구조

```
Game Scripts (Scripts/)
    ├──→ Base Module (Modules/Base/)
    │        ├── Facade (정적 접근점)
    │        │   ├── Interfaces (계약 정의)
    │        │   └── Bootstrapper (초기화)
    │        ├── PageChanger (페이지 전환)
    │        ├── PopupManager (팝업 관리)
    │        ├── Notifier (이벤트 버스)
    │        ├── Utility (공통 유틸리티)
    │        └── Extensions (확장 함수)
    │
    └──→ FiniteStateMachine Module (Modules/FiniteStateMachine/)
             └──→ Base.Runtime 의존
```

- **Game Scripts**는 `Facade`를 통해서 베이스 서비스에 접근한다.
- PageChanger, PopupManager, Notifier는 사용처에서 **직접 인스턴스를 참조**하여 사용한다.
- **FiniteStateMachine**은 별도 모듈로, Base.Runtime(Notifier, CollectionExtensions 등)에 의존한다.
- 인터페이스와 구현체가 분리되어 있어 구현체 교체가 자유롭다.
- Base 모듈은 게임 로직(Scripts)에 의존하지 않는다.

---

## Utility / Extensions 폴더

### Utility

프로젝트 전역에서 공통으로 사용하는 유틸리티 기능.

- `EnumLike<T>` : int 값을 내부적으로 가지며 비교 가능한 Enum 대체 클래스. 상속을 통해 확장 가능.
- `Singleton<T>` : MonoBehaviour 싱글톤 베이스 클래스
- 기타 범용 헬퍼 클래스

```csharp
public abstract class EnumLike<T> : IEquatable<T>, IComparable<T> where T : EnumLike<T>
{
    public int Value { get; }
    public string Name { get; }

    protected EnumLike(int value, string name) { ... }

    public bool Equals(T other) => other != null && Value == other.Value;
    public int CompareTo(T other) => Value.CompareTo(other?.Value ?? 0);

    public override bool Equals(object obj) => Equals(obj as T);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Name;

    public static bool operator ==(EnumLike<T> left, EnumLike<T> right) => ...;
    public static bool operator !=(EnumLike<T> left, EnumLike<T> right) => ...;
}
```

**사용 예시:**

```csharp
public class MonsterType : EnumLike<MonsterType>
{
    public static readonly MonsterType Wolf = new(0, "Wolf");
    public static readonly MonsterType Bear = new(1, "Bear");
    public static readonly MonsterType Snake = new(2, "Snake");

    private MonsterType(int value, string name) : base(value, name) { }
}
```

### Extensions

프로젝트 전역에서 공통으로 사용하는 확장 메서드.

- `TransformExtensions` : Transform 관련 확장
- `CollectionExtensions` : List, Array 등 컬렉션 확장
- 기타 Unity 타입 확장 메서드
