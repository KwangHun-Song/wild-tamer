# PlayPage 구현 계획

## 개요

`PlayPage`는 게임 플레이 씬의 메인 페이지다.
**Canvas(UI)** 와 **WorldMapRoot(월드 공간)** 를 동시에 보유하는 구조로, 두 가지 역할을 수행한다.

| 역할 | 내용 |
|------|------|
| UI 페이지 | 세팅 버튼 등 HUD 요소를 포함하는 Canvas |
| 월드맵 루트 | `WorldMap.prefab`이 런타임에 인스턴스화되어 배치되는 부모 Transform |

`PageChanger`가 `PlayPage` 프리팹을 로드/언로드하며,
씬 플로우(`EnterPageState`)에서 `PlayPage`로 전환 시 WorldMap도 함께 활성화된다.

---

## 프리팹 하이에라키 구조

```
PlayPage (프리팹 루트)           [PlayPage 컴포넌트]
├── Canvas                       [Canvas + CanvasScaler + GraphicRaycaster]
│   └── HUD
│       ├── TopBar               [빈 RectTransform — 상단 영역]
│       │   └── SettingButton    [Button + Image]
│       └── MinimapPlaceholder   [빈 RectTransform — 미니맵 예약 영역, 우하단]
└── WorldMapRoot                 [빈 Transform — 월드 공간, Canvas 밖]
```

> `WorldMapRoot`는 Canvas 하위가 아닌 PlayPage 루트의 **형제 또는 자식** Transform이다.
> Canvas와 동일 레벨에 두어 월드 좌표계를 유지한다.

---

## PlayPage.cs 변경 사항

현재 `PlayPage.cs`는 스켈레톤만 존재한다.
세팅 버튼 바인딩과 WorldMapRoot 참조를 추가한다.

```csharp
using Base;
using UnityEngine;
using UnityEngine.UI;

public class PlayPage : Page
{
    public override string PageName => "PlayPage";

    [SerializeField] private Button settingButton;
    [SerializeField] private Transform worldMapRoot;

    public Transform WorldMapRoot => worldMapRoot;

    protected override void OnEnter()
    {
        settingButton.onClick.AddListener(OnSettingButtonClicked);
    }

    protected override void OnExit()
    {
        settingButton.onClick.RemoveListener(OnSettingButtonClicked);
    }

    private void OnSettingButtonClicked()
    {
        // TODO: 세팅 팝업 열기 (Phase 3에서 구현)
        // Facade.PopupManager.ShowAsync<SettingPopup>("SettingPopup");
        Facade.Logger.Log("Setting button clicked");
    }
}
```

> `Page` 베이스 클래스의 `OnEnter`/`OnExit` 생명주기를 활용한다.
> 현재 `Page.cs`에 해당 메서드가 없다면, `OnEnable`/`OnDisable`로 대체한다.

---

## 단계별 구현 순서

### Step 1 — PlayPage.cs 코드 수정

`Assets/Scripts/02.Page/PlayPage.cs`에 위 코드 적용.

확인 사항:
- `Page` 베이스 클래스에 `OnEnter`/`OnExit` virtual 메서드 존재 여부 확인
- 없으면 `OnEnable`/`OnDisable` 사용

---

### Step 2 — PlayPage 프리팹 생성 (Canvas 구성)

#### 2-A: 프리팹 파일 생성

1. `Assets/Prefabs/Pages/` 폴더 확인 (없으면 생성)
2. Hierarchy에 빈 GameObject 생성 → 이름: `PlayPage`
3. `PlayPage` 컴포넌트 추가

#### 2-B: Canvas 설정

`PlayPage` 하위에 Canvas 오브젝트 생성:

| 컴포넌트 | 설정값 |
|---------|--------|
| Canvas | Render Mode: **Screen Space - Overlay** |
| Canvas Scaler | UI Scale Mode: **Scale With Screen Size**, Reference Resolution: **1920×1080**, Match: **0.5** |
| Graphic Raycaster | 기본값 유지 |

#### 2-C: HUD 구성

`Canvas` 하위에 `HUD` 빈 RectTransform 생성 (Stretch to full):

**TopBar** (`HUD` 하위):
- Anchor: Top-Stretch, Height: 80
- 배경 Image (선택적): 반투명 검정

**SettingButton** (`TopBar` 하위):
- RectTransform: Anchor Right-Top, Width/Height: 60×60, 우상단 여백 10px
- Image 컴포넌트: 임시 흰색 사각형 (추후 설정 아이콘 스프라이트로 교체)
- Button 컴포넌트: 추가
- TextMeshProUGUI 자식: "⚙" 또는 "SET" (임시 레이블)

**MinimapPlaceholder** (`HUD` 하위):
- Anchor: Bottom-Right, Width/Height: 200×200, 여백 10px
- 빈 Panel (반투명 회색) — 미니맵 시스템 연결 예약 영역
- 비활성(Inactive) 상태로 유지해도 무방

---

### Step 3 — WorldMapRoot 설정

`PlayPage` 루트 하위에 Canvas와 **형제 레벨**로 빈 GameObject 생성:

- 이름: `WorldMapRoot`
- Transform: Position (0, 0, 0), Rotation 기본값, Scale (1, 1, 1)
- 컴포넌트: 없음 (빈 Transform)

> `WorldMap.prefab`은 런타임(`GameLoop` 또는 씬 초기화 시)에 `WorldMapRoot` 하위로 인스턴스화된다.

---

### Step 4 — SerializeField 연결

`PlayPage` 오브젝트 선택 → Inspector:

| 필드 | 연결 대상 |
|------|----------|
| `settingButton` | `Canvas/HUD/TopBar/SettingButton` |
| `worldMapRoot` | `WorldMapRoot` |

---

### Step 5 — WorldMap 인스턴스화 연동

`GameLoop.cs`에서 WorldMap 프리팹을 `PlayPage.WorldMapRoot` 하위에 배치한다.

현재 `GameLoop`는 씬에 직접 배치된 `MapGenerator`를 참조한다.
WorldMap 프리팹 방식으로 전환할 경우 두 가지 선택지:

**Option A — 씬에 WorldMap 인스턴스를 미리 배치 (권장 — 초기 단계)**
- PlayPage 프리팹 내 `WorldMapRoot` 하위에 WorldMap 오브젝트를 직접 중첩 배치
- `GameLoop`의 `mapGenerator` SerializeField를 WorldMap 내부 `MapGenerator`로 연결
- 프리팹 안의 프리팹(Nested Prefab) 형태

**Option B — 런타임 인스턴스화 (유연, 추후 전환 권장)**
- `GameLoop.Start()`에서 `WorldMapPrefab`을 `WorldMapRoot` 하위에 `Instantiate`
- 생성된 인스턴스에서 `MapGenerator` 컴포넌트를 `GetComponentInChildren`으로 획득

> **이 단계에서는 Option A로 시작한다.** 구조가 검증되면 Option B로 리팩토링.

**Option A 적용 방법:**
1. PlayPage 프리팹 열기 (Prefab Mode)
2. `WorldMapRoot` 하위에 `WorldMap.prefab`을 Nested Prefab으로 배치
3. `GameLoop`의 `mapGenerator` 필드를 `WorldMapRoot/WorldMap/MapGenerator`로 연결
4. 씬의 `GameLoop`가 PlayPage 프리팹 인스턴스를 SerializeField로 참조하도록 설정

---

### Step 6 — PlayPage 프리팹 저장 및 Resources 등록

`PageChanger`는 `Resources.Load`로 페이지를 로드한다.

1. PlayPage 오브젝트를 `Assets/Resources/Pages/PlayPage.prefab`으로 저장
   (또는 프로젝트에서 사용하는 Pages 경로 확인)
2. 기존 `EnterPageState`의 `pageChanger.ChangePageAsync("PlayPage")` 호출과 이름 일치 확인

---

### Step 7 — 씬 연동 확인 (Play Mode 테스트)

1. Unity Play Mode 진입
2. SceneFlow: Init → Load → EnterPage(PlayPage) 순서로 진행되는지 확인
3. PlayPage가 화면에 표시되고 SettingButton이 보이는지 확인
4. WorldMap이 `WorldMapRoot` 하위에 배치되었는지 Hierarchy에서 확인
5. Console에 컴파일 에러 및 런타임 NullReferenceException 없음 확인

---

## 검증 체크리스트

- [ ] `PlayPage.cs`에 `settingButton`, `worldMapRoot` SerializeField 추가됨
- [ ] Canvas Render Mode: Screen Space - Overlay
- [ ] Canvas Scaler: 1920×1080, Scale With Screen Size
- [ ] `SettingButton` 우상단 배치, Button 컴포넌트 연결됨
- [ ] `MinimapPlaceholder` 우하단에 예약 영역 존재
- [ ] `WorldMapRoot` 빈 Transform이 Canvas와 형제 레벨로 존재
- [ ] `settingButton` SerializeField → SettingButton 오브젝트 연결됨
- [ ] `worldMapRoot` SerializeField → WorldMapRoot Transform 연결됨
- [ ] `WorldMap.prefab`이 WorldMapRoot 하위에 배치됨 (Option A)
- [ ] `PlayPage.prefab`이 `Resources/Pages/` 경로에 저장됨
- [ ] Play Mode에서 SettingButton 클릭 시 콘솔에 로그 출력됨

---

## 관련 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/02.Page/PlayPage.cs` | (수정 대상) 버튼 바인딩, WorldMapRoot 참조 |
| `Assets/Scripts/01.Scene/PlayScene/States/EnterPageState.cs` | PlayPage 진입 트리거 |
| `Assets/Scripts/01.Scene/PlayScene/GameLoop.cs` | MapGenerator 참조, 게임 루프 |
| `Assets/Prefabs/Pages/PlayPage.prefab` | (생성 대상) |
| `Assets/Prefabs/WorldMap/WorldMap.prefab` | WorldMapRoot에 배치될 프리팹 |

---

## 작업 순서 요약

```
1. PlayPage.cs 코드 수정 (settingButton, worldMapRoot 필드 추가)
2. Canvas 생성 및 HUD 구성 (SettingButton, MinimapPlaceholder)
3. WorldMapRoot 빈 Transform 생성
4. SerializeField 연결
5. WorldMap을 WorldMapRoot 하위에 Nested Prefab으로 배치
6. Resources/Pages/ 경로에 프리팹 저장
7. Play Mode 테스트 및 검증
```
