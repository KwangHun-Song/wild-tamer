# Base 모듈 & FSM 모듈 코드 리뷰 (2차)

## 리뷰 대상

- **Base 모듈**: `Modules/Base/` (Facade, Interfaces, Defaults, Notifier, PageChanger, PopupManager, Utility, Extensions, Tests)
- **FSM 모듈**: `Modules/FiniteStateMachine/` (State, StateMachine, StateTransition, IStateChangeEvent, Tests)
- **설계 문서**: `design.md`

---

## 1. 이전 리뷰 조치 현황

| 이전 지적사항 | 심각도 | 현재 상태 |
|--------------|--------|----------|
| 2-1. Notifier 열거 중 컬렉션 변경 위험 | 높음 | **해소됨** — `.ToList()` 스냅샷 적용 |
| 2-2. Notifier 중복 구독 방지 없음 | 중간 | **해소됨** — `Contains` 체크 추가 |
| 2-3. StateTransition.NullTrigger default(Enum) 위험 | 높음 | **해소됨** — `bool HasTrigger` 플래그로 교체 |
| 2-4. Facade 초기화 전 접근 보호 | 중간 | **해소됨** — 필드 이니셜라이저로 기본값 할당 |
| 4-2. ILogger.cs 불필요한 using | 낮음 | **해소됨** — `using System` 제거 |
| 1-1. IInstanceLoader 구현 미완료 | 낮음 | **해소됨** — 인터페이스 + DefaultInstanceLoader 구현 |
| 1-2. Bootstrapper 초기화 코드 주석 | 낮음 | **해소됨** — MonoBehaviour 서비스만 초기화하는 방식으로 구현 |
| 테스트 부재 (Notifier, FSM) | 권고 | **해소됨** — NotifierTests 10개, StateMachineTests 20개+ 추가 |
| 코드 스타일 불일치 | 권고 | **대부분 해소** — Allman 스타일로 통일 |

---

## 2. 코드 품질 이슈 (신규)

### 2-1. TestStateMachine — 생성자 파라미터가 필드를 가림 (shadowing) [심각도: 높음]

`StateMachineTests.cs:68-79`:
```csharp
private readonly StateTransition<TestEntity, TestTrigger>[] transitions;  // 필드

public TestStateMachine(TestEntity owner, StateTransition<TestEntity, TestTrigger>[] transitions = null)
    : base(owner)
{
    transitions = transitions ?? new[]  // ← 파라미터에 할당, 필드가 아님!
    {
        StateTransition<TestEntity, TestTrigger>.Generate(Idle, Attack, TestTrigger.Attack),
        ...
    };
}

protected override StateTransition<TestEntity, TestTrigger>[] Transitions => transitions;  // 필드 반환 → null
```

생성자 파라미터 `transitions`가 필드 `transitions`를 가리기 때문에(shadowing), `this.transitions` 필드는 항상 `null`로 남는다. `Transitions` 프로퍼티가 null을 반환하므로 `SetUp()` → `BuildTransitionLookup()` → `foreach (var transition in Transitions)` 에서 `NullReferenceException`이 발생한다.

**수정:**
```csharp
this.transitions = transitions ?? new[] { ... };
```

### 2-2. ExecuteCommand — 혼합 전이에서 Condition 무시 [심각도: 중간]

`StateMachine.cs:56-74`:
```csharp
public bool ExecuteCommand(TEnumTrigger inTrigger)
{
    foreach (var transition in transitions)
    {
        if (!transition.HasTrigger)
            continue;
        if (!transition.TransitionTrigger.Equals(inTrigger))
            continue;

        ChangeState(transition.ToState);  // Condition 체크 없이 바로 전이
        return true;
    }
}
```

Trigger + Condition 혼합 전이에서 `ExecuteCommand`는 트리거 매칭만 확인하고 `TransitionCondition`은 체크하지 않는다. 즉 Condition이 false여도 트리거만 맞으면 전이가 발생한다.

테스트 코드의 `Mixed_TriggerAndConditionTrue_TransitionsViaExecuteCommand`는 Condition이 항상 true인 케이스만 검증하고, **Condition이 false인 경우의 테스트가 누락**되어 있다.

이것이 의도된 설계라면 (ExecuteCommand = 강제 전이, Condition은 자동 전이 전용), 해당 의도를 코드 주석으로 명시해야 한다.

**권고:**
- 의도적 설계인 경우: ExecuteCommand 메서드에 "트리거 매칭만 확인하며, Condition은 무시한다" 주석 추가
- Condition도 체크해야 하는 경우: `transition.TransitionCondition != null && !transition.IsTransferable` 체크 추가

### 2-3. DefaultDatabase.EnsureLoaded — Resources.LoadAll 전체 스캔 [심각도: 중간]

`DefaultDatabase.cs:51-61`:
```csharp
private void EnsureLoaded<T>() where T : class
{
    if (cache.ContainsKey(typeof(T)))
        return;

    var assets = Resources.LoadAll<ScriptableObject>("")  // Resources 폴더 전체 스캔
        .Where(so => so is T)
        .ToList();

    cache[typeof(T)] = assets;
}
```

빈 문자열 `""`로 `Resources.LoadAll`을 호출하면 **Resources 폴더 내 모든 ScriptableObject**를 로드한다. 프로젝트 규모가 커지면 심각한 성능 문제가 될 수 있다.

**권고:** 타입별 서브폴더 규칙을 도입하거나, `Resources.LoadAll<T>("")`로 직접 제네릭 타입을 사용.

단, `where T : class` 제약으로 인해 `Resources.LoadAll<T>`를 직접 사용하려면 제약 조건 변경이 필요. 현재 프로젝트 규모에서는 큰 문제가 아니므로 **향후 개선 사항**으로 기록.

### 2-4. DefaultObjectPool.ClearPool(prefab) — instanceToPrefabId 정리 누락 [심각도: 중간]

`DefaultObjectPool.cs:62-94`:
```csharp
public void ClearPool(GameObject prefab = null)
{
    if (prefab != null)
    {
        int id = prefab.GetInstanceID();
        if (pools.TryGetValue(id, out var pool))
        {
            while (pool.Count > 0)
            {
                var obj = pool.Dequeue();
                if (obj != null)
                    Object.Destroy(obj);
            }
            pools.Remove(id);
        }
        // instanceToPrefabId에서 해당 prefab의 항목들을 정리하지 않음!
    }
    else
    {
        // 전체 클리어 시에는 instanceToPrefabId.Clear() 호출됨
        pools.Clear();
        instanceToPrefabId.Clear();
    }
}
```

특정 프리팹의 풀만 클리어할 때, `instanceToPrefabId`에서 해당 프리팹에 속하는 인스턴스 항목들이 정리되지 않는다. Destroy된 오브젝트의 InstanceID가 `instanceToPrefabId`에 남아있게 되어, Unity가 같은 InstanceID를 재사용하면 의도하지 않은 동작이 발생할 수 있다.

**수정:**
```csharp
if (prefab != null)
{
    int id = prefab.GetInstanceID();
    if (pools.TryGetValue(id, out var pool))
    {
        while (pool.Count > 0)
        {
            var obj = pool.Dequeue();
            if (obj != null)
            {
                instanceToPrefabId.Remove(obj.GetInstanceID());
                Object.Destroy(obj);
            }
        }
        pools.Remove(id);
    }
}
```

### 2-5. DefaultSoundManager — AudioClip 캐싱 없음 [심각도: 낮음]

`DefaultSoundManager.cs:53-65, 72-82`:
```csharp
public void PlayBGM(string clipName)
{
    var clip = Resources.Load<AudioClip>(clipName);  // 매번 Resources.Load
    ...
}

public void PlaySFX(string clipName)
{
    var clip = Resources.Load<AudioClip>(clipName);  // 매번 Resources.Load
    ...
}
```

`Resources.Load`는 내부적으로 캐싱이 있으나, 빈번한 SFX 재생 시 문자열 검색 오버헤드가 누적될 수 있다.

**권고 (선택적):** Dictionary 기반 캐싱 추가. 현재 단계에서는 필수 아님.

### 2-6. ITimeProvider — 설계 문서와 구현 인터페이스 불일치 [심각도: 낮음]

**설계 문서:**
```csharp
public interface ITimeProvider
{
    DateTime Now { get; }
    void JumpSeconds(double seconds);
    void ResetOffset();
}
```

**실제 구현:**
```csharp
public interface ITimeProvider
{
    DateTime Now { get; }
    double OffsetSeconds { get; set; }
}
```

설계의 `JumpSeconds()` + `ResetOffset()` 메서드 방식 대신, `OffsetSeconds` 프로퍼티로 단순화됨. 기능적으로는 동등하며 오히려 더 간결하지만, 설계 문서와 코드 사이의 불일치는 해소하는 것이 바람직.

**권고:** 설계 문서를 현재 구현에 맞춰 업데이트.

---

## 3. 구조 및 아키텍처

### 3-1. 긍정적인 점

- **Facade 초기화 전략이 개선됨.** 순수 클래스 서비스는 필드 이니셜라이저로, MonoBehaviour 서비스는 Bootstrapper에서 초기화하는 이원화 구조가 안전하고 명확함
- **인터페이스-구현체 분리가 일관적.** 11개 Facade 인터페이스 모두 `Defaults/` 폴더에 기본 구현체가 존재
- **Default 구현체들의 품질이 양호.** DefaultLogger의 LogLevel 필터링, DefaultDataStore의 역직렬화 실패 처리, DefaultObjectPool의 prefab-instance 매핑 등 실용적인 구현
- **DefaultSceneChanger-DefaultSceneTransition 협력 패턴이 설계 의도를 잘 반영.** Transition의 null 체크도 포함
- **테스트 설계가 체계적.** FSM 테스트가 Trigger-only / Condition-only / Mixed 세 유형을 구분하여 검증
- **FSM Notifier 연동 테스트 존재.** `StateChangeEvent_NotifiesListeners`로 모듈 간 통합도 검증
- **이전 리뷰의 핵심 이슈가 모두 해결됨.** Notifier 스냅샷, 중복 구독 방지, HasTrigger 플래그 등

### 3-2. Facade 서비스 간 의존성

`DefaultDataStore`가 `Facade.Json`을 직접 사용:
```csharp
public void Save<T>(string key, T data)
{
    string json = Facade.Json.Serialize(data);  // 다른 Facade 서비스에 의존
    ...
}
```

`DefaultSceneChanger`가 `Facade.Transition`을 직접 사용:
```csharp
public async UniTask ChangeSceneAsync(string sceneName)
{
    if (Facade.Transition != null)
        await Facade.Transition.TransitionInAsync();  // 다른 Facade 서비스에 의존
    ...
}
```

서비스 간 의존 관계:
```
DefaultDataStore → Facade.Json
DefaultSceneChanger → Facade.Transition
DefaultSoundManager → Facade.Logger
DefaultDatabase → Facade.Logger
```

현재 Facade가 필드 이니셜라이저로 기본값을 할당하므로 null 문제는 없다. 다만 서비스가 Facade 정적 클래스를 통해 다른 서비스를 참조하는 구조는, 서비스를 독립적으로 테스트하기 어렵게 만든다 (Facade 전체를 셋업해야 함).

현재 프로젝트 규모에서는 문제없으며, 향후 필요 시 생성자 주입으로 전환 가능.

### 3-3. Notifier.Subscribe — 제네릭 파라미터의 의미

`Notifier.cs:21-38`:
```csharp
public void Subscribe<T>(T listener) where T : IListener
{
    var listenerInterfaces = listener.GetType()
        .GetInterfaces()
        .Where(i => typeof(IListener).IsAssignableFrom(i) && i != typeof(IListener));

    foreach (var listenerInterface in listenerInterfaces)
    {
        // T가 아닌, 런타임 타입의 모든 IListener 인터페이스에 대해 등록
    }
}
```

`Subscribe<T>(T listener)`의 제네릭 파라미터 `T`는 컴파일 타임 타입이지만, 실제로는 **런타임 타입의 모든 IListener 인터페이스**에 대해 등록한다. 이는 `MultiListener` 같은 다중 이벤트 구현체를 지원하기 위한 의도적 설계이며, 테스트(`MultiListener_ReceivesBothEventTypes`)로 검증되어 있다.

다만 API 사용자 입장에서 `Subscribe<ITestEvent>(listener)`를 호출하면 ITestEvent에만 등록될 것으로 예상할 수 있으므로, XML 주석으로 이 동작을 명시하면 좋겠다.

---

## 4. 코드 스타일 및 컨벤션

### 4-1. Allman 스타일 통일 — 대부분 해소

이전 리뷰에서 지적된 K&R 스타일이 Allman으로 통일됨. Notifier.cs, FSM 모듈 전체 모두 Allman 스타일 적용 확인.

### 4-2. EnumLikeTests.cs 1행 들여쓰기

`EnumLikeTests.cs:1`:
```csharp
 using NUnit.Framework;  // 첫 줄 앞에 불필요한 공백
```

사소한 포맷팅 이슈. `using` 문 앞에 공백이 하나 있음.

### 4-3. 접근 제한자 — 컨벤션 준수 양호

코딩 컨벤션의 "모든 선언에는 접근 제한자를 명시" 규칙이 잘 준수되고 있음. `private`, `public`, `protected` 모두 명시적으로 선언.

### 4-4. 중괄호 생략 — 컨벤션 준수

코딩 컨벤션의 "한 줄 본문은 중괄호 생략 가능" 규칙에 맞게, 단일 문장 `if`에서 중괄호를 일관적으로 사용하거나 생략하고 있음.

---

## 5. 테스트 커버리지

| 대상 | 테스트 여부 | 테스트 수 | 비고 |
|------|------------|-----------|------|
| EnumLike\<T\> | O | 17개 | 충분한 커버리지 |
| Notifier | O | 10개 | 구독/해지/발행/중복/반복안전성 등 주요 시나리오 커버 |
| FSM (StateMachine) | O | 20개+ | Trigger-only/Condition-only/Mixed 세 유형 분류 테스트 |
| Singleton\<T\> | X | - | MonoBehaviour 의존으로 EditMode 테스트 어려움 |
| Default 구현체들 | X | - | MonoBehaviour/Unity API 의존으로 EditMode 테스트 어려움 |
| CollectionExtensions | X | - | 단순하여 우선순위 낮음 |

### 5-1. 테스트 품질 평가

**NotifierTests — 양호:**
- 기본 구독/발행, 구독 해지, 다중 리스너, 다중 이벤트 타입, 중복 구독 방지, 반복 중 해지 안전성 등 핵심 시나리오를 모두 커버
- `UnsubscribingListener` 패턴으로 열거 중 변경 안전성을 검증한 점이 우수

**StateMachineTests — 양호하나 보완 필요:**
- 세 가지 전이 유형을 체계적으로 분류하여 검증
- Notifier 연동 및 Owner 접근 테스트도 포함
- **단, TestStateMachine 생성자 버그로 인해 실제 실행 시 테스트가 실패할 가능성 높음** (2-1 참조)
- `ExecuteCommand`에서 혼합 전이의 Condition이 false인 경우의 테스트가 누락

### 5-2. 누락된 테스트 케이스

- `ExecuteCommand` + 혼합 전이 + Condition false → 전이 여부 (현재 동작: 전이됨, Condition 무시)
- `SendMessage` 메서드 테스트 전무
- 존재하지 않는 상태에서의 전이 시도

---

## 6. 설계 문서 대비 구현 차이

| 항목 | 설계 문서 | 실제 구현 | 심각도 |
|------|----------|----------|--------|
| ITimeProvider | `JumpSeconds()`, `ResetOffset()` | `OffsetSeconds { get; set; }` | 낮음 — 구현이 더 간결 |
| Bootstrapper | 모든 서비스를 명시적 초기화 | 순수 클래스는 필드 이니셜라이저, MonoBehaviour만 Bootstrapper | 낮음 — 구현이 더 우수 |
| Facade 폴더구조 | `Defaults/` 폴더 미언급 | `Defaults/` 하위에 11개 기본 구현체 | 낮음 — 설계 문서 업데이트 필요 |

이전 리뷰 대비 설계-구현 간격이 크게 줄어듦. 남은 차이들은 구현이 더 나은 방향으로 발전한 경우이므로, 설계 문서를 구현에 맞춰 업데이트하면 해소됨.

---

## 7. 종합 평가

| 항목 | 이전 점수 | 현재 점수 | 변화 | 설명 |
|------|----------|----------|------|------|
| 설계-구현 일치도 | A- | **A** | +1 | IInstanceLoader 구현, Defaults 전체 구현. ITimeProvider 사소한 차이만 남음 |
| 코드 품질 | B | **B+** | +1 | 이전 핵심 이슈 해소. 신규 이슈는 TestStateMachine 버그, ObjectPool 정리 누락 등 |
| 구조/아키텍처 | A- | **A** | +1 | Defaults 분리, Bootstrapper 이원화, 인터페이스-구현체 완전 대응 |
| 테스트 | C+ | **B+** | +2 | Notifier 10개, FSM 20개+ 추가. 생성자 버그 수정 후 견고한 커버리지 |
| 코드 스타일 | B- | **A-** | +2 | Allman 스타일 통일, 접근 제한자 명시, 중괄호 규칙 준수 |
| 컨벤션 준수 | A- | **A** | +1 | 전반적으로 코딩 컨벤션 잘 준수 |

### 우선 조치 사항

1. **[필수]** TestStateMachine 생성자에서 `this.transitions` 으로 필드 할당 수정
2. **[권고]** ExecuteCommand의 혼합 전이 Condition 처리 정책 결정 및 주석/테스트 보완
3. **[권고]** DefaultObjectPool.ClearPool(prefab) 시 instanceToPrefabId 정리 추가
4. **[권고]** 설계 문서 업데이트 — ITimeProvider API, Defaults 폴더, Bootstrapper 이원화 구조
5. **[선택]** DefaultDatabase.EnsureLoaded의 Resources.LoadAll 범위 최적화
6. **[선택]** EnumLikeTests.cs 1행 공백 제거
