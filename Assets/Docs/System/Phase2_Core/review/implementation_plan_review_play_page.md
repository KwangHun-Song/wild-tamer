# PlayPage 구현 계획 리뷰

## 리뷰 대상 문서

| 문서 | 경로 |
|------|------|
| `implementation_plan_play_page.md` | `Assets/Docs/System/Phase2_Core/implementation_plan_play_page.md` |

---

## 구현 컨벤션 충족도

| 항목 | 충족 | 비고 |
|------|------|------|
| 1. 기술 스택 | X | 섹션 자체가 없음 |
| 2. 시스템 설계 (클래스 목록, 책임, 의존성) | △ | PlayPage.cs 코드 예시는 있으나 의존성 기술 없음 |
| 3. 구현 순서 (병렬 가능 여부 명시) | △ | 7단계 순서 명확하나 `[병렬 가능]` 마커 없음 |
| 4. 테스트 계획 (유닛/수동 검증) | △ | 검증 체크리스트만 있고, 유닛/수동 분리 없음 |

---

## 긍정적인 점

- **PlayPage.cs 코드 예시 포함**: 변경 사항을 코드로 직접 제시하여 구현자의 해석 여지를 줄였다.
- **Option A/B 비교 제시**: WorldMap 인스턴스화 방식을 두 가지로 비교하고 단계별 권장 옵션을 명시한 점이 실용적이다.
- **Canvas 설정값 구체화**: Render Mode, Canvas Scaler 수치, Anchor 설정까지 명시하여 UI 구성 오류를 예방한다.
- **OnEnter/OnExit 구독 해제 패턴**: `onClick.AddListener`/`RemoveListener` 쌍을 올바르게 구현하여 이벤트 누수가 없다.

---

## 이슈

### 1. 기술 스택 섹션 누락 (중요도: 높음)

컨벤션 필수 항목 #1이 없다. 이 구현 계획은 여러 Unity 패키지와 Phase 1 모듈에 의존한다.

포함되어야 할 항목:

| 항목 | 내용 |
|------|------|
| TextMeshPro | SettingButton 레이블에 사용 (`TextMeshProUGUI`) |
| UnityEngine.UI | Button, Canvas, CanvasScaler, GraphicRaycaster |
| Base.PageChanger | Phase 1 모듈 — `Resources.Load` 기반 페이지 로드 |
| Base.Page | PlayPage의 베이스 클래스 |

---

### 2. 테스트 계획 섹션 누락 (중요도: 높음)

검증 체크리스트가 있지만 컨벤션이 요구하는 **테스트 계획 섹션**이 없다. PlayPage는 주로 Unity 에디터 작업이므로 유닛 테스트 대상이 적지만, 명시적으로 분리해야 한다.

유닛 테스트 가능한 항목:
- `PlayPage.OnEnter()` / `OnExit()`: settingButton 리스너 등록/해제 검증
- `PlayPage.WorldMapRoot` 프로퍼티: null이 아닌 Transform 반환 검증

수동 검증 항목(Play Mode):
- SceneFlow Init → Load → EnterPage(PlayPage) 전환 순서
- SettingButton 클릭 시 콘솔 로그 출력
- WorldMap이 WorldMapRoot 하위에 배치됨
- Canvas 1920×1080 기준으로 올바르게 스케일됨

---

### 3. Page 베이스 클래스 OnEnter/OnExit 존재 여부 미확인 (중요도: 중간)

Step 1에 다음 내용이 있다.

> `Page` 베이스 클래스에 `OnEnter`/`OnExit` virtual 메서드 존재 여부 확인. 없으면 `OnEnable`/`OnDisable` 사용.

이 불확실성이 구현 계획에 그대로 남겨져 있다. `Page.cs`를 사전에 확인하여 확정된 상태로 문서를 작성해야 한다. `OnEnter`/`OnExit`가 없다면 코드 예시 전체를 재작성해야 하므로 계획의 신뢰도가 낮아진다.

**수정 방향**: `Page.cs`를 먼저 읽고 실제 인터페이스를 확인한 후 코드 예시를 확정 버전으로 교체.

---

### 4. PageChanger Resources 경로 미확인 (중요도: 중간)

Step 6에 다음 내용이 있다.

> 프로젝트에서 사용하는 Pages 경로 확인

`Resources.Load` 경로가 `EnterPageState`의 `ChangePageAsync("PlayPage")` 호출과 일치해야 하는데, 정확한 경로가 확인되지 않은 상태로 "확인"을 구현 단계로 미루고 있다. 경로 불일치는 런타임에서 페이지 로드 실패로 이어지며, 에러 메시지도 불명확하다.

**수정 방향**: `PageChanger.cs` 또는 `EnterPageState.cs`를 미리 읽고 실제 사용 경로를 계획에 확정 기재.

---

### 5. 병렬 실행 가능 항목 미표시 (중요도: 중간)

Step 1(PlayPage.cs 코드 수정)과 Step 2/3(프리팹 에디터 작업)은 서로 독립적이어서 코드 작성과 에디터 작업을 병렬로 진행할 수 있다. 또한 Step 2(Canvas 구성)와 Step 3(WorldMapRoot 생성)도 동일 오브젝트를 수정하지 않으므로 병렬 가능하다.

**수정 방향**: Step 2, 3에 `[병렬 가능]` 마커 추가. Step 1과 Step 2/3도 검토 후 표시.

---

### 6. MinimapPlaceholder와 Minimap 연동 방법 미언급 (중요도: 낮음)

Step 2-C에서 `MinimapPlaceholder`를 예약 영역으로 생성하지만, 실제 `Minimap` MonoBehaviour가 이 영역에 어떻게 연결될지 언급이 없다. `Minimap.cs`는 `RawImage`와 아이콘 프리팹을 SerializeField로 받는 구조인데, PlayPage 어디에 Minimap 컴포넌트가 추가되고 어떤 필드에 연결될지 설계 연계가 필요하다.

**수정 방향**: MinimapPlaceholder 영역에 `Minimap` 컴포넌트 추가 계획 또는 Phase 3 구현 범위임을 명시.

---

### 7. WorldMapRoot가 Canvas 자식인지 형제인지 혼재 (중요도: 낮음)

본문 계층 다이어그램:

```
PlayPage (프리팹 루트)
├── Canvas
└── WorldMapRoot   ← PlayPage의 직접 자식
```

그런데 Step 3 설명에는:

> `PlayPage` 루트 하위에 Canvas와 **형제 레벨**로 빈 GameObject 생성

"형제 레벨"이라는 표현이 Canvas와 WorldMapRoot가 같은 부모(PlayPage) 아래 형제 관계임을 의미하는데, 다이어그램과 설명이 일치한다. 그러나 주석에 "형제 또는 자식"이라는 표현이 혼재하여 혼동을 줄 수 있다.

**수정 방향**: "PlayPage의 직접 자식이며, Canvas와 형제 관계"로 표현을 통일.

---

## 종합 평가

| 항목 | 등급 | 설명 |
|------|------|------|
| 컨벤션 충족도 | **C+** | 기술 스택·테스트 계획 2개 필수 섹션 누락 |
| 작업 구체성 | **A** | Canvas 수치, SerializeField 연결 대상까지 명시 |
| 코드 품질 | **A** | PlayPage.cs 예시가 OnEnter/OnExit 패턴을 올바르게 적용 |
| 사전 조사 완성도 | **B** | Page.cs 인터페이스, PageChanger 경로가 미확인 상태로 계획에 포함됨 |

### 구현 시작 전 보완 권장 사항 3가지

1. **기술 스택 섹션 추가** — TextMeshPro, UnityEngine.UI, Base.Page/PageChanger 명시
2. **Page.cs 및 PageChanger.cs 사전 확인** — OnEnter/OnExit 존재 여부, Resources 경로 확정 후 계획 업데이트
3. **테스트 계획 섹션 추가** — PlayPage 유닛 테스트 대상과 수동 검증 항목 분리 기술
