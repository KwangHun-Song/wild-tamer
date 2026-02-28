# 1.1 프로젝트 구조 - 컨셉/설계 문서 리뷰

## 컨셉 문서 (`concept.md`) 리뷰

### 작업 컨벤션 충족도

| 항목 | 상태 | 비고 |
|------|------|------|
| 시스템의 목적과 역할 | O | "유지보수성과 확장성 확보" 명시 |
| 유저 관점의 동작 설명 | △ | 개발자 관점 중심, 유저(=개발자) 동작 흐름은 없음 |
| 레퍼런스 자료 | X | UPM 레이아웃 참조 외 별도 레퍼런스 없음 |
| 다른 시스템과의 관계 | O | 의존성 방향 규칙으로 명시 |

### 긍정적인 점

- 폴더 구조가 명확하고 역할 분리가 잘 되어 있음
- `asmdef` 기반 모듈 분리는 컴파일 시간 단축과 의존성 관리에 효과적
- UPM 레이아웃(Runtime/Editor/Tests) 채택은 업계 표준에 부합
- 의존성 방향 규칙(`Scripts → Modules` 허용, 역방향 금지)이 깔끔함

### 개선 제안

1. **Resources 폴더 사용 기준 구체화 필요** — "꼭 필요한 경우에만"이라는 표현이 모호함. 구체적으로 어떤 에셋이 들어가는지 예시가 있으면 좋겠음 (e.g., Prefab 동적 로드용 등)

2. **Scripts 폴더 내부 구조 미정의** — Phase 2 이후 게임 로직이 들어갈 `Scripts/` 폴더의 하위 구조 가이드라인이 없음. 시스템별로 폴더를 나눌지, 기능별로 나눌지 방향성이라도 있으면 좋겠음

3. **Graphic 폴더 내 Prefabs 위치 부재** — 프리팹이 Graphic 아래인지, Scripts 관련인지, 별도 `Prefabs/` 폴더를 둘지 언급이 없음. 게임 특성상 몬스터/캐릭터 프리팹이 많을텐데 관리 방안이 필요함

4. **서드파티 관리 규칙** — DOTween만 언급되어 있고, UniTask 등 추가 라이브러리에 대한 위치 규칙이 없음. `ThirdParty/` 또는 `Plugins/` 같은 통합 폴더 고려 여부

---

## 설계 문서 (`design.md`) 리뷰

### 작업 컨벤션 충족도

| 항목 | 상태 | 비고 |
|------|------|------|
| 핵심 클래스 및 인터페이스 구조 | O | Facade + 10개 인터페이스 + 4개 독립 시스템 |
| 클래스 간 의존 관계 및 데이터 흐름 | O | 의존성 구조 다이어그램 포함 |
| 사용할 디자인 패턴 | O | Facade, Pub/Sub, FSM 등 명시 |
| 외부 시스템과의 인터페이스 정의 | O | 인터페이스 시그니처 전부 정의 |

### 긍정적인 점

- 인터페이스 기반 설계로 테스트 가능성과 교체 가능성 확보
- `Facade` 정적 클래스로 서비스 로케이터 역할 — 간단하고 접근성 좋음
- `PopupManager.ShowAsync<T>` 패턴이 실용적 — 결과값까지 await로 받는 구조가 깔끔
- `EnumLike<T>` 유틸리티가 확장성 면에서 enum보다 유연

### 개선 제안 / 잠재적 이슈

1. **Facade의 DI 초기화 전략 미정의 (중요)**
   - `Facade`의 프로퍼티가 전부 `{ get; set; }`인데, 어디서 언제 초기화하는지 설계에 없음
   - 초기화 전에 접근하면 NullReferenceException 발생. 초기화 시점과 방법(Bootstrapper, ScriptableObject 등)을 명시해야 함
   - 초기화 순서 의존성도 고려 필요 (e.g., Logger가 먼저 초기화되어야 다른 서비스에서 로깅 가능)

2. **ISceneChanger와 ISceneTransition의 관계 불명확**
   - `ISceneChanger.ChangeSceneAsync()`가 내부적으로 `ISceneTransition`을 사용하는 건지, 호출자가 둘 다 직접 조합해야 하는 건지 명확하지 않음
   - 사용 시나리오 예시가 있으면 좋겠음

3. **ICoroutineRunner와 UniTask 혼용 우려**
   - `ISceneChanger`, `IPageChanger`, `IPopupManager`, `ISceneTransition`은 `UniTask`를 반환하는데, `ICoroutineRunner`는 전통적 코루틴 기반
   - 프로젝트 내에서 비동기 처리 방식의 통일 기준이 필요함. UniTask로 통일할지, 코루틴이 필요한 케이스를 구체적으로 명시할지

4. **IObjectPool의 키가 GameObject인 점**
   - `Spawn(GameObject prefab)`에서 프리팹 인스턴스 자체를 키로 사용하는데, string 기반 키 대비 프리팹 참조 관리가 필요해짐
   - 프리팹을 직접 참조하려면 어딘가에서 프리팹 목록을 들고 있어야 하는데 그 구조가 미정의

5. **독립 시스템이 Facade에 포함되지 않은 이유 불명확**
   - PageChanger, PopupManager, Notifier가 Facade에 없는 이유 설명이 부족함
   - "별도로 관리한다"만 있고, 어떻게 접근하는지(직접 참조? DI?) 명확하지 않음
   - 특히 `Notifier`는 거의 모든 시스템에서 사용할 텐데, 정적 접근점 없이 어떻게 주입하는지

6. **IDatabase와 IDataStore의 경계**
   - 설명은 되어 있지만, 실제 사용 시 혼동될 수 있음. 예를 들어 "유저가 획득한 아이템 목록"은 IDataStore인지 IDatabase인지
   - IDatabase의 데이터 소스(JSON, ScriptableObject, CSV 등)가 미정의

7. **FSM의 제네릭 타입 파라미터 부재**
   - 현재 `IStateMachine`에 상태 전환 조건(Trigger/Condition)이 없어서 `ChangeState`를 외부에서 직접 호출해야 함
   - 게임 흐름 제어와 AI 양쪽에 범용적으로 쓰려면 Transition 조건 메커니즘이 필요할 수 있음

8. **Notifier에 구독 해제 누락 방지 장치 없음**
   - `Subscribe`/`Unsubscribe` 패턴에서 구독 해제를 잊으면 메모리 리크가 생김
   - `IDisposable` 반환이나 WeakReference 기반 구독 등 안전 장치 고려 필요

---

## 종합 평가

| 항목 | 점수 | 설명 |
|------|------|------|
| 구조 설계 | **A** | 폴더 구조, asmdef 분리, 의존성 규칙 모두 잘 잡혀있음 |
| 인터페이스 설계 | **B+** | 인터페이스 분리가 적절하나, 일부 관계와 경계가 모호 |
| 완성도 | **B** | 핵심은 잘 정의되어 있으나, 초기화 전략과 접근 방식 등 실제 구현에 필요한 세부사항이 부족 |
| 컨벤션 준수 | **B+** | 작업 컨벤션 항목 대부분 충족, 레퍼런스와 유저 관점 동작 설명이 약함 |

### 우선 보강이 필요한 3가지

1. Facade 초기화 전략 (Bootstrapper 패턴 등)
2. 독립 시스템(PageChanger, PopupManager, Notifier)의 접근/주입 방식
3. 코루틴 vs UniTask 비동기 처리 통일 기준
