# 1.1 프로젝트 구조 - 구현 계획

## 1. 기술 스택

| 항목 | 선택 | 비고 |
|------|------|------|
| Unity | 2022.3 LTS | 프로젝트 기준 버전 |
| UniTask | v2 | 비동기 처리 기본 방식 (UPM 설치) |
| DOTween | v2 | 트윈 애니메이션 (이미 포함) |
| JSON | Newtonsoft Json.NET | Unity 기본 패키지 (`com.unity.nuget.newtonsoft-json`) |
| 테스트 | Unity Test Framework | EditMode 유닛 테스트 |

## 2. 시스템 설계

### 구현할 파일 목록

#### Facade 및 인터페이스

| 파일 | 책임 |
|------|------|
| `Facade/Facade.cs` | 정적 서비스 접근점. 11개 인터페이스 프로퍼티 (기본 구현체 초기값 포함) |
| `Facade/Bootstrapper.cs` | Facade 초기화. RuntimeInitializeOnLoadMethod |
| `Facade/Interfaces/IDataStore.cs` | 런타임 데이터 저장/로드 계약 |
| `Facade/Interfaces/ISceneChanger.cs` | 씬 변경 계약 |
| `Facade/Interfaces/IObjectPool.cs` | 오브젝트 풀링 계약 |
| `Facade/Interfaces/ISoundManager.cs` | 사운드 관리 계약 |
| `Facade/Interfaces/ILogger.cs` | 로깅 계약 + LogLevel, DebugColor enum |
| `Facade/Interfaces/ITimeProvider.cs` | 시간 제공 계약 |
| `Facade/Interfaces/ISceneTransition.cs` | 화면 전환 연출 계약 |
| `Facade/Interfaces/IJsonSerializer.cs` | JSON 직렬화 계약 |
| `Facade/Interfaces/IDatabase.cs` | 정적 데이터 조회 계약 |
| `Facade/Interfaces/ICoroutineRunner.cs` | 코루틴 실행/지연 호출 계약 |

#### 독립 시스템

| 파일 | 책임 |
|------|------|
| `PageChanger/IPage.cs` | 페이지 계약 |
| `PageChanger/IPageChanger.cs` | 페이지 전환 계약 |
| `PageChanger/Page.cs` | Page MonoBehaviour 추상 클래스 |
| `PageChanger/PageChanger.cs` | Resources 기반 페이지 전환 구현 (GoBack 히스토리 포함) |
| `PopupManager/IPopup.cs` | 팝업 계약 |
| `PopupManager/IPopupManager.cs` | 팝업 관리 계약 |
| `PopupManager/Popup.cs` | Popup MonoBehaviour 추상 클래스 (Canvas sorting, CompletionSource) |
| `PopupManager/PopupManager.cs` | 스택 기반 팝업 관리 구현 |
| `EscapeHandler/IEscapeListener.cs` | Esc 이벤트 리스너 계약 (IListener 상속) |
| `EscapeHandler/IEscapeHandler.cs` | Esc 핸들러 계약 |
| `EscapeHandler/EscapeHandler.cs` | 스택 기반 Esc 이벤트 처리 구현 |

> **참고:** Notifier, FSM은 외부 모듈을 가져왔으며 이 구현 계획과 별도로 관리.

#### Facade 기본 구현체

| 파일 | 책임 |
|------|------|
| `Facade/Defaults/DefaultLogger.cs` | UnityEngine.Debug 기반 로깅. 색상 및 레벨 필터링 |
| `Facade/Defaults/DefaultJsonSerializer.cs` | Newtonsoft Json.NET 기반 직렬화/역직렬화 |
| `Facade/Defaults/DefaultCoroutineRunner.cs` | MonoBehaviour 기반 코루틴 실행 및 지연 호출 |
| `Facade/Defaults/DefaultTimeProvider.cs` | DateTime.UtcNow 기반. 오프셋으로 시간 조작 |
| `Facade/Defaults/DefaultDataStore.cs` | PlayerPrefs + JSON 기반 런타임 데이터 저장/로드 |
| `Facade/Defaults/DefaultDatabase.cs` | ScriptableObject 기반 정적 데이터 조회 |
| `Facade/Defaults/DefaultObjectPool.cs` | Dictionary + Queue 기반 오브젝트 풀링 |
| `Facade/Defaults/DefaultSoundManager.cs` | AudioSource 기반 BGM/SFX 재생 |
| `Facade/Defaults/DefaultSceneChanger.cs` | SceneManager + SceneTransition 활용 씬 전환 |
| `Facade/Defaults/DefaultSceneTransition.cs` | 기본 전환 연출 (초기 구현: 즉시 전환) |
| `Facade/Defaults/DefaultInstanceLoader.cs` | Object.Instantiate/Destroy 기반 인스턴스 관리 |

#### Utility / Extensions

| 파일 | 책임 |
|------|------|
| `Utility/EnumLike.cs` | Enum 대체 확장 가능 클래스 |
| `Utility/Singleton.cs` | MonoBehaviour 싱글톤 베이스 |
| `Extensions/CollectionExtensions.cs` | IEnumerable ForEach 확장 메서드 |

#### 프로젝트 설정

| 파일 | 책임 |
|------|------|
| `Base.Runtime.asmdef` | Base 모듈 런타임 어셈블리 정의 |
| `Base.Tests.asmdef` | Base 모듈 테스트 어셈블리 정의 |

### 의존성 관계

```
Bootstrapper → Facade → Interfaces (전부)
독립 시스템 (PageChanger, PopupManager, EscapeHandler) → Facade (Loader, Logger 사용)
FiniteStateMachine 모듈 → Base.Runtime (Notifier, IListener, CollectionExtensions)
Utility, Extensions → 의존 없음
```

## 3. 구현 순서

### Step 1: 프로젝트 폴더 및 asmdef 생성 ✅

- [x] Base 모듈 폴더 구조 생성 (`Modules/Base/Runtime/Scripts/...`)
- [x] `Base.Runtime.asmdef` 생성 (UniTask 참조 포함)
- [x] `Base.Tests.asmdef` 생성
- [x] `Prefabs/`, `Plugins/` 폴더 생성
- [x] DOTween을 `Plugins/` 폴더로 이동

### Step 2: Facade 인터페이스 및 Enum 정의 ✅

- [x] **2-A: Facade 인터페이스 (그룹 1)** — ILogger (+ LogLevel, DebugColor enum), IJsonSerializer, ICoroutineRunner, ITimeProvider
- [x] **2-B: Facade 인터페이스 (그룹 2)** — IDataStore, IDatabase, IObjectPool, ISoundManager
- [x] **2-C: Facade 인터페이스 (그룹 3)** — ISceneChanger, ISceneTransition, IInstanceLoader
- [x] **2-D: 독립 시스템 인터페이스** — IPage, IPageChanger, IPopup, IPopupManager, IEscapeListener, IEscapeHandler

### Step 3: Facade 클래스 및 Utility ✅

- [x] **3-A: Facade + Bootstrapper** — Facade 정적 클래스 (기본 구현체 초기값 포함), Bootstrapper 초기화 (MonoBehaviour 서비스만)
- [x] **3-B: Utility** — EnumLike\<T\>, Singleton\<T\>, CollectionExtensions

### Step 4: Facade 기본 구현체 ✅

- [x] **4-A: 기반 서비스 구현** — DefaultLogger, DefaultJsonSerializer, DefaultCoroutineRunner, DefaultTimeProvider
- [x] **4-B: 데이터/인스턴스 서비스 구현** — DefaultDataStore, DefaultDatabase, DefaultObjectPool, DefaultInstanceLoader
- [x] **4-C: 씬/사운드 서비스 구현** — DefaultSceneChanger, DefaultSceneTransition, DefaultSoundManager

Facade 클래스의 각 프로퍼티가 기본 구현체를 초기값으로 가지도록 하여, Bootstrapper 없이도 기본 동작이 보장된다. MonoBehaviour 기반 서비스(Coroutine, Sound, Escape)만 Bootstrapper에서 초기화.

### Step 5: 독립 시스템 구현 및 유닛 테스트 ✅

- [x] PageChanger 구현 (Page 추상 클래스, PageChanger)
- [x] PopupManager 구현 (Popup 추상 클래스, PopupManager)
- [x] EscapeHandler 구현 (IEscapeListener, IEscapeHandler, EscapeHandler)
- [x] EnumLike\<T\> 유닛 테스트 (17개)
- [x] Notifier 유닛 테스트 (10개)
- [x] FSM 유닛 테스트 (22개+, Trigger/Condition/Mixed 분류)

### Step 6: 컴파일 검증 및 정리 ✅

- [x] asmdef 참조 관계 검증 (Base.Runtime → UniTask, FSM → Base.Runtime)
- [x] 불필요 파일 정리 (고아 .meta 파일 제거)
- [x] 코드리뷰 v1, v2 반영 완료

## 4. 테스트 계획

### 유닛 테스트 (EditMode)

| 대상 | 테스트 내용 |
|------|------------|
| `EnumLike<T>` | 값 비교, 동등성, 해시코드, ToString, 정렬 |
| `Notifier` | Subscribe/Unsubscribe/Notify, 중복 구독 방지, 열거 중 변경 안전성 |
| `FSM` | Trigger/Condition/혼합 전이, 상태 Enter/Exit, 이벤트 발행 |

### 수동 검증

| 항목 | 검증 방법 |
|------|----------|
| asmdef 의존성 | Unity Editor에서 Base.Runtime.asmdef 인스펙터 확인 |
| 컴파일 | Unity Console에 에러/경고 없음 확인 |
| 폴더 구조 | Unity Project 창에서 구조 확인 |
