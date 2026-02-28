# Base 모듈 & FSM 모듈 코드 리뷰

## 리뷰 대상

- **Base 모듈**: `Modules/Base/` (Facade, Interfaces, Notifier, PageChanger, PopupManager, Utility, Extensions, Tests)
- **FSM 모듈**: `Modules/FiniteStateMachine/` (State, StateMachine, StateTransition, IStateChangeEvent)
- **설계 문서**: `design.md` (2차 업데이트 반영)

---

## 1. 설계 문서 대비 구현 차이 (Design-Implementation Gap)

### 1-1. IInstanceLoader — 설계에 추가되었으나 구현 미완료 [심각도: 낮음]

설계 문서에 새로운 Facade 인터페이스 `IInstanceLoader`가 추가됨:

```csharp
public interface IInstanceLoader
{
    GameObject Load(GameObject prefab, Transform parent = null);
    void Unload(GameObject instance);
}
```

Facade 클래스와 Bootstrapper에도 `Loader` 프로퍼티가 설계에 포함되었으나, 현재 코드에는 아직 반영되지 않음:
- `Facade.cs` — `IInstanceLoader Loader` 프로퍼티 없음
- `IInstanceLoader.cs` — 파일 미생성
- `Bootstrapper.cs` — `Facade.Loader` 초기화 없음

**참고:** `IInstanceLoader`와 `IObjectPool`의 역할이 부분적으로 겹침. `IObjectPool.Spawn()`은 풀링 기반 생성, `IInstanceLoader.Load()`는 범용 생성(구현체에 따라 Instantiate, 풀링, 활성화 등 교체 가능). 설계 문서에서 이 관계를 명시한 점("구현체에 따라 오브젝트 풀링 등 다양한 방식으로 교체 가능")은 좋으나, 실제 사용 시 **어떤 상황에서 IObjectPool을 쓰고 어떤 상황에서 IInstanceLoader를 쓰는지** 가이드라인이 있으면 혼동을 줄일 수 있음.

### 1-2. Bootstrapper — 초기화 코드 전체 주석 처리 [심각도: 낮음]

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
private static void Initialize()
{
    // 모든 초기화 코드가 주석 처리됨
}
```

구현체가 아직 없으므로 의도적인 것으로 보이나, 설계에서 정의한 초기화 순서 규칙(Logger → Data → Pool 순)이 주석으로 보존되어 있어 긍정적.

### 1-3. 이전 리뷰 지적사항 해소 현황

| 이전 지적사항 | 현재 상태 |
|--------------|----------|
| Notifier 설계-구현 불일치 | **해소됨** — 설계가 IListener 기반 패턴으로 업데이트됨 |
| FSM 별도 모듈 분리 미반영 | **해소됨** — 설계에 FiniteStateMachine 모듈 섹션 추가 |
| FSM 사용 예시 부재 | **해소됨** — MonsterTrigger/MonsterFSM 예시 추가 |
| Notifier 사용 예시 부재 | **해소됨** — IMonsterDefeatedListener/QuestTracker 예시 추가 |
| 독립 시스템 접근 방식 불명확 | **해소됨** — "직접 인스턴스를 참조" 명시 |

---

## 2. 코드 품질 이슈

### 2-1. Notifier — 열거 중 컬렉션 변경 위험 [심각도: 높음]

`Notifier.cs:46-49`:
```csharp
public void Notify<T>(Action<T> action) where T : IListener {
    foreach (var listener in GetListeners<T>()) {
        action?.Invoke(listener);
    }
}
```

`GetListeners<T>()`가 반환하는 `listeners.OfType<T>()`는 원본 `List<object>`에 대한 지연 열거자. Notify 콜백 내에서 `Subscribe` 또는 `Unsubscribe`를 호출하면 `InvalidOperationException` 발생.

**실제 발생 시나리오:** 설계 문서의 사용 예시처럼 MonoBehaviour의 `OnDisable()`에서 Unsubscribe하는 패턴에서, Notify 중 오브젝트가 비활성화되면 콜백 내에서 Unsubscribe가 호출될 수 있음.

**권고:** Notify 시작 시 리스너 목록의 스냅샷을 생성.

```csharp
public void Notify<T>(Action<T> action) where T : IListener {
    var snapshot = GetListeners<T>().ToList();
    foreach (var listener in snapshot) {
        action?.Invoke(listener);
    }
}
```

### 2-2. Notifier — 중복 구독 방지 없음 [심각도: 중간]

`Subscribe`에서 동일 리스너의 중복 등록을 체크하지 않음. 같은 객체를 두 번 Subscribe하면 Notify 시 두 번 호출됨.

```csharp
// 현재 코드 - 중복 체크 없음
Listeners[listenerInterface].Add(listener);
```

**권고:** `Contains` 체크 추가 또는 `HashSet<object>` 사용.

### 2-3. StateTransition.NullTrigger — default(Enum) 위험 [심각도: 높음]

`StateTransition.cs:8`:
```csharp
public static TEnumTrigger NullTrigger = default;
```

`default(TEnumTrigger)`는 enum의 0번째 값으로, 유효한 트리거 값일 수 있음. "트리거 없음"을 표현하기에 부적절.

**문제 시나리오 (설계 문서의 예시 기준):**
```csharp
enum MonsterTrigger { Detect, Lose, Attack }  // Detect = 0 = default
// condition-only 전이에서 TransitionTrigger = default = Detect
// ExecuteCommand(MonsterTrigger.Detect) 호출 시 condition-only 전이도 매칭될 수 있음
```

`ExecuteCommand` 메서드에서 `TransitionTrigger`가 default인 경우(condition-only 전이)를 구분하는 로직이 없음:

```csharp
public bool ExecuteCommand(TEnumTrigger inTrigger)
{
    foreach (var transition in transitions)
    {
        if (!transition.TransitionTrigger.Equals(inTrigger)) // default와 매칭될 수 있음
            continue;
        ChangeState(transition.ToState);
        return true;
    }
}
```

**권고:**
- condition-only 전이인지 여부를 나타내는 `bool HasTrigger` 플래그 추가
- 또는 `TEnumTrigger?` (nullable) 사용 고려

### 2-4. Facade — 초기화 전 접근 보호 없음 [심각도: 중간]

```csharp
public static class Facade
{
    public static IDataStore Data { get; set; }       // null 가능
    public static ISceneChanger Scene { get; set; }   // null 가능
    // ...
}
```

Bootstrapper가 실행되기 전에 Facade 프로퍼티에 접근하면 NullReferenceException. 특히 에디터 모드에서 테스트 실행 시 Bootstrapper가 동작하지 않을 수 있음.

**권고 (선택적):** 접근 시 초기화 여부 체크 또는 `[RuntimeInitializeOnLoadMethod]` 실행 순서 보장 확인.

### 2-5. State — 타입 기반 동등성 비교 [심각도: 낮음]

`State.cs:26-31`:
```csharp
public bool Equals(State<TEntity, TEnumTrigger> other)
{
    if (other is null) return false;
    if (ReferenceEquals(this, other)) return true;
    return this.GetType() == other.GetType();  // 인스턴스가 달라도 타입이 같으면 equal
}
```

서로 다른 인스턴스라도 같은 타입이면 동등하게 취급. `StateMachine.transitionLookup`이 Dictionary의 키로 State를 사용하므로, 같은 타입의 State 인스턴스가 여러 개 있으면 Dictionary에서 충돌 발생.

설계 문서의 사용 예시(`private readonly IdleState _idle = new()`)를 보면 **상태 타입당 하나의 인스턴스**를 전제하고 있으므로 의도적인 설계로 보임. 다만 이 제약이 문서화되어 있지 않음.

**권고:** 이 제약 조건을 주석이나 문서로 명시.

### 2-6. CollectionExtensions.ForEach — 루프 내 null 체크 [심각도: 낮음]

`CollectionExtensions.cs:8-14`:
```csharp
public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
{
    foreach (var item in source)
    {
        action?.Invoke(item);  // 매 반복마다 null 체크
    }
}
```

`action`이 null이면 전체 루프가 아무 일도 하지 않으므로, 루프 진입 전에 한 번만 체크하는 것이 효율적.

---

## 3. 구조 및 아키텍처

### 3-1. 긍정적인 점

- **인터페이스 분리가 잘 되어 있음.** 각 Facade 인터페이스가 단일 책임 원칙을 따르고, 시그니처가 깔끔함
- **asmdef 의존성 구조가 올바름.** `FiniteStateMachine.Runtime` → `Base.Runtime` 단방향 의존, 순환 참조 없음
- **FSM 설계가 실용적.** Trigger + Condition 이중 전이 메커니즘, Notifier 연동, 추상 클래스 기반으로 확장 용이
- **EnumLike<T> 구현이 견고함.** null 안전성, 연산자 오버로딩, IEquatable/IComparable 모두 정확하게 구현
- **테스트 커버리지가 적절함** (EnumLike). Equals, HashCode, CompareTo, 연산자, null 케이스 모두 커버
- **Singleton<T> 구현이 깔끔함.** DontDestroyOnLoad, 중복 인스턴스 처리, OnDestroy 정리 모두 포함
- **UniTask 통일.** 비동기 인터페이스(ISceneChanger, IPageChanger, IPopupManager, ISceneTransition)가 모두 UniTask를 반환하여 일관성 있음
- **설계 문서가 구현을 잘 반영.** Notifier의 IListener 패턴, FSM의 별도 모듈 구조, 사용 예시가 모두 실제 코드와 일치

### 3-2. FSM 모듈의 Base 모듈 의존성

FSM이 `Base.Notifier`와 `Base.IListener`, `Base.CollectionExtensions.ForEach`를 직접 사용. 이는 의존성 방향(`Modules → Modules` 허용)에 부합하며, 설계 문서에도 명시되어 있음.

다만 FSM이 `Notifier` 구체 클래스에 직접 의존하고 있어, Notifier 구현을 교체하려면 FSM도 함께 수정해야 함. 현재 프로젝트 규모에서는 문제없으나, 향후 확장 시 인터페이스 추출을 고려할 수 있음.

### 3-3. IInstanceLoader와 IObjectPool의 역할 경계

설계에 새로 추가된 `IInstanceLoader`는 범용 GameObject 생성/파괴를, `IObjectPool`은 풀링 기반 최적화를 담당. 두 인터페이스의 사용 기준이 명확해야 혼동을 방지할 수 있음.

| | IObjectPool | IInstanceLoader |
|--|-------------|-----------------|
| 목적 | 성능 최적화 (재활용) | 범용 인스턴스 관리 |
| 키 | GameObject prefab | GameObject prefab |
| 생성 | `Spawn()` | `Load()` |
| 파괴 | `Despawn()` (풀로 반환) | `Unload()` (파괴 또는 구현체 정책) |
| 추가 기능 | `Preload()`, `ClearPool()` | 없음 |

**권고:** 실제 구현 시 "일반적으로는 IInstanceLoader를 사용하고, 빈번한 생성/파괴가 필요한 경우(투사체, 이펙트 등)에만 IObjectPool을 사용한다" 같은 가이드라인을 설계 문서에 추가.

---

## 4. 코드 스타일

### 4-1. 모듈 간 중괄호 스타일 불일치

| 모듈 | 스타일 | 예시 |
|------|--------|------|
| Base (대부분) | Allman (다음 줄) | `class Foo\n{` |
| Notifier.cs | K&R (같은 줄) | `class Notifier {` |
| FSM 모듈 전체 | K&R (같은 줄) | `class State<...> {` |

C# 코딩 컨벤션(MSDN/Unity 표준)은 Allman 스타일을 권장. 모듈 간 일관성을 위해 통일 필요.

### 4-2. ILogger.cs — 불필요한 using

`ILogger.cs:1`:
```csharp
using System;  // ILogger 인터페이스에서 System 타입을 사용하지 않음
```

enum(`LogLevel`, `DebugColor`)은 `System` 네임스페이스를 필요로 하지 않음.

---

## 5. 테스트 커버리지

| 대상 | 테스트 여부 | 비고 |
|------|------------|------|
| EnumLike\<T\> | O (17개 테스트) | 충분한 커버리지 |
| Notifier | X | Subscribe/Unsubscribe/Notify 테스트 필요 |
| Singleton\<T\> | X | MonoBehaviour 의존이라 EditMode 테스트 어려움 |
| FSM (State, StateMachine, StateTransition) | X | 핵심 로직이라 테스트 우선 필요 |
| CollectionExtensions | X | 단순하여 우선순위 낮음 |

**권고:** Notifier와 FSM의 유닛 테스트를 추가. 특히 FSM의 전이 로직(Trigger, Condition, 혼합)과 Notifier의 구독/발행 동작은 버그 발생 가능성이 높은 영역.

---

## 6. 종합 평가

| 항목 | 점수 | 설명 |
|------|------|------|
| 설계-구현 일치도 | **A-** | Notifier, FSM 설계 반영 완료. IInstanceLoader 구현 대기 중 |
| 코드 품질 | **B** | 대체로 견고하나 Notifier 열거 안전성, StateTransition NullTrigger 이슈 존재 |
| 구조/아키텍처 | **A-** | asmdef 분리, 인터페이스 설계, 의존성 방향 모두 잘 잡혀있음 |
| 테스트 | **C+** | EnumLike만 테스트 존재. Notifier, FSM 테스트 부재 |
| 코드 스타일 | **B-** | 모듈 간 중괄호 스타일 불일치 |
| 컨벤션 준수 | **A-** | 설계 문서가 구현을 잘 반영하도록 업데이트됨 |

### 우선 조치 사항

1. **[필수]** `StateTransition.NullTrigger` 이슈 해결 — default(Enum) 충돌 방지
2. **[필수]** `Notifier.Notify()` 열거 중 컬렉션 변경 안전성 확보
3. **[권고]** `IInstanceLoader` 인터페이스 및 Facade.Loader 프로퍼티 구현
4. **[권고]** Notifier, FSM 유닛 테스트 추가
5. **[권고]** 코드 스타일 통일 (Allman 스타일로 일관성 확보)
6. **[선택]** Notifier에 중복 구독 방지 로직 추가
7. **[선택]** IInstanceLoader vs IObjectPool 사용 가이드라인 추가
