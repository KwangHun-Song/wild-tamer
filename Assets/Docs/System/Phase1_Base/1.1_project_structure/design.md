# 1.1 프로젝트 구조 - 설계

## Base 모듈 개요

Base 모듈은 프로젝트의 기반 기능을 제공한다. 핵심 서비스는 **Facade 패턴**으로 정적 접근하고, 독립적인 시스템(PageChanger, PopupManager, Notifier, FSM)은 별도 폴더로 분리하여 관리한다.

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
        │   │   │   └── ICoroutineRunner.cs
        │   │   └── Facade.cs
        │   ├── PageChanger/
        │   │   ├── IPage.cs
        │   │   └── IPageChanger.cs
        │   ├── PopupManager/
        │   │   ├── IPopup.cs
        │   │   └── IPopupManager.cs
        │   ├── Notifier/
        │   ├── FSM/
        │   ├── Utility/
        │   │   └── EnumLike.cs
        │   └── Extensions/
        └── Base.Runtime.asmdef
```

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
}
```

- 각 프로퍼티에 **기본 구현체**를 할당하여 즉시 사용 가능하게 한다.
- 테스트나 특수 상황에서 구현체를 교체(Mock 주입 등)할 수 있다.

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

Unity Scene 변경.

```csharp
public interface ISceneChanger
{
    UniTask ChangeSceneAsync(string sceneName);
    string CurrentScene { get; }
}
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

### ISceneTransition

페이지나 씬 전환 시 화면 전환 연출.

```csharp
public interface ISceneTransition
{
    UniTask TransitionInAsync();
    UniTask TransitionOutAsync();
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

```csharp
public interface IDatabase
{
    T Get<T>(string id) where T : class;
    IReadOnlyList<T> GetAll<T>() where T : class;
    bool TryGet<T>(string id, out T result) where T : class;
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

### PageChanger

Page(UI 단위 화면) 전환 시스템. Facade에 포함하지 않고 별도로 관리한다.

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
// 팝업을 열고 닫힐 때까지 대기, 결과값 수신
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

이벤트 버스 역할. 시스템 간 느슨한 결합을 위한 발행/구독 패턴.

```csharp
public interface INotifier
{
    void Subscribe<T>(Action<T> handler);
    void Unsubscribe<T>(Action<T> handler);
    void Publish<T>(T eventData);
}
```

### FSM (Finite State Machine)

상태 기계 프레임워크. 게임 흐름 제어, AI 등에 범용적으로 활용.

```csharp
public interface IState
{
    void Enter();
    void Update();
    void Exit();
}

public interface IStateMachine
{
    void ChangeState(IState newState);
    void Update();
    IState CurrentState { get; }
}
```

---

## 의존성 구조

```
Game Scripts (Scripts/)
    └──→ Base Module (Modules/Base/)
             ├── Facade (정적 접근점)
             │   └── Interfaces (계약 정의)
             ├── PageChanger (페이지 전환)
             ├── PopupManager (팝업 관리)
             ├── Notifier (이벤트 버스)
             ├── FSM (상태 기계)
             ├── Utility (공통 유틸리티)
             └── Extensions (확장 함수)
```

- **Game Scripts**는 `Facade`를 통해서 베이스 서비스에 접근한다.
- PageChanger, PopupManager, Notifier, FSM은 직접 참조하여 사용한다.
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

// 외부에서 확장
public class BossMonsterType : MonsterType
{
    public static readonly MonsterType Dragon = new BossMonsterType(100, "Dragon");
    // ...
}
```

### Extensions

프로젝트 전역에서 공통으로 사용하는 확장 메서드.

- `TransformExtensions` : Transform 관련 확장
- `CollectionExtensions` : List, Array 등 컬렉션 확장
- 기타 Unity 타입 확장 메서드
