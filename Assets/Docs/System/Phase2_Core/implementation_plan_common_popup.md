# CommonPopup 구현 계획

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 제목·내용·버튼 수(1개/2개)를 파라미터로 전달하면 공용 팝업이 표시되고, OK 버튼 클릭 시 `true`, 그 외 버튼 클릭 시 `false`를 반환하는 공용 팝업 시스템 구현

**Architecture:** `Base.Popup`을 상속하는 `CommonPopup`(View)과 파라미터 클래스 `CommonPopupParam`(Model)을 분리한다. `PopupManager.ShowAsync<bool>("Popups/CommonPopup", param)`으로 팝업을 띄우고, 결과는 `Close(true/false)` → `WaitForCloseAsync()` 체인으로 반환한다. PlayPage는 설정 버튼 클릭을 이벤트로 노출하고, InPlayState가 구독해서 팝업을 연다(MVP 패턴).

**Tech Stack:** Unity uGUI, UniTask, Base.Popup/PopupManager, TextMeshPro, Resources.Load

---

## 사전 확인

- `PopupManager.ShowAsync<T>(name, param)` 동작: `Resources.Load<GameObject>(name)` → 프리팹 로드 후 `ShowAsync(param)` 호출 → `WaitForCloseAsync()` → `T` 반환
- 팝업 프리팹 경로: `Assets/Resources/Popups/CommonPopup.prefab` (Resources 상대 경로: `"Popups/CommonPopup"`)
- UI 텍스쳐 위치: `Assets/Graphic/UI/`
  - 배경: `Papers/RegularPaper.png`
  - OK 버튼: `Buttons/BigBlueButton_Regular.png` / `BigBlueButton_Pressed.png`
  - Cancel 버튼: `Buttons/BigRedButton_Regular.png` / `BigRedButton_Pressed.png`
  - 타이틀 리본: `Ribbons/SmallRibbons.png`

---

## Task 1: CommonPopupParam 클래스 작성

**파일:**
- Create: `Assets/Scripts/02.Page/CommonPopupParam.cs`

**Step 1: 파일 작성**

```csharp
public class CommonPopupParam
{
    public string Title { get; }
    public string Content { get; }
    public bool HasTwoButtons { get; }

    public CommonPopupParam(string title, string content, bool hasTwoButtons = false)
    {
        Title = title;
        Content = content;
        HasTwoButtons = hasTwoButtons;
    }
}
```

**Step 2: 컴파일 확인**

Unity Editor Console에서 컴파일 에러 없음 확인.

**Step 3: 커밋**

```bash
git add Assets/Scripts/02.Page/CommonPopupParam.cs
git add Assets/Scripts/02.Page/CommonPopupParam.cs.meta
git commit -m "CommonPopupParam 파라미터 클래스 추가"
```

---

## Task 2: CommonPopup 스크립트 작성

**파일:**
- Create: `Assets/Scripts/02.Page/CommonPopup.cs`

**Step 1: 파일 작성**

```csharp
using Base;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CommonPopup : Popup
{
    public override string PopupName => "Popups/CommonPopup";

    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private Button okButton;
    [SerializeField] private Button cancelButton;

    public override UniTask ShowAsync(object enterParam = null)
    {
        if (enterParam is CommonPopupParam param)
        {
            titleText.text = param.Title;
            contentText.text = param.Content;
            cancelButton.gameObject.SetActive(param.HasTwoButtons);
        }

        okButton.onClick.AddListener(OnOkClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);

        return base.ShowAsync(enterParam);
    }

    public override void Close(object leaveParam = null)
    {
        okButton.onClick.RemoveListener(OnOkClicked);
        cancelButton.onClick.RemoveListener(OnCancelClicked);
        base.Close(leaveParam);
    }

    private void OnOkClicked() => Close(true);
    private void OnCancelClicked() => Close(false);
}
```

**Step 2: 컴파일 확인**

Unity Editor Console에서 컴파일 에러 없음 확인.

**Step 3: 커밋**

```bash
git add Assets/Scripts/02.Page/CommonPopup.cs
git add Assets/Scripts/02.Page/CommonPopup.cs.meta
git commit -m "CommonPopup 스크립트 작성 (Popup 상속, bool 결과 반환)"
```

---

## Task 3: UI 텍스쳐 Sprite 임포트 설정

팝업 배경 및 버튼에 사용할 텍스쳐들의 Texture Type을 `Sprite (2D and UI)`로 설정한다.

**대상 파일:**
- `Assets/Graphic/UI/Papers/RegularPaper.png` — 9-Slice 사용 예정
- `Assets/Graphic/UI/Buttons/BigBlueButton_Regular.png`
- `Assets/Graphic/UI/Buttons/BigBlueButton_Pressed.png`
- `Assets/Graphic/UI/Buttons/BigRedButton_Regular.png`
- `Assets/Graphic/UI/Buttons/BigRedButton_Pressed.png`
- `Assets/Graphic/UI/Ribbons/SmallRibbons.png`

**Step 1: Unity MCP로 각 텍스쳐 임포트 설정 확인 및 수정**

Unity MCP `manage_asset` 또는 `manage_texture` 툴로 각 텍스쳐의 `textureType`을 `Sprite`로 설정한다.
이미 Sprite로 설정되어 있다면 이 단계는 생략한다.

**Step 2: RegularPaper.png 9-Slice 보더 설정**

Unity Sprite Editor에서 RegularPaper.png의 Border를 설정한다 (예: Left=20, Right=20, Top=20, Bottom=20).
실제 이미지 크기에 맞게 조정 필요.

**Step 3: 커밋**

```bash
git add Assets/Graphic/UI/Papers/RegularPaper.png.meta
git add Assets/Graphic/UI/Buttons/BigBlueButton_Regular.png.meta
git add Assets/Graphic/UI/Buttons/BigBlueButton_Pressed.png.meta
git add Assets/Graphic/UI/Buttons/BigRedButton_Regular.png.meta
git add Assets/Graphic/UI/Buttons/BigRedButton_Pressed.png.meta
git add Assets/Graphic/UI/Ribbons/SmallRibbons.png.meta
git commit -m "CommonPopup용 UI 텍스쳐 Sprite 임포트 설정"
```

---

## Task 4: Resources/Popups 폴더 생성 및 CommonPopup 프리팹 제작

**파일:**
- Create folder: `Assets/Resources/Popups/`
- Create: `Assets/Resources/Popups/CommonPopup.prefab`

**프리팹 계층 구조:**

```
CommonPopup                     ← CommonPopup 스크립트 + Canvas (Screen Space - Overlay, Sort Order 0)
└── Overlay                     ← Image (색상: 000000, Alpha: 150/255), FullScreen stretch
    └── Panel                   ← Image (RegularPaper.png, Image Type: Sliced), 고정 크기 (예: 500x300)
        ├── TitleArea           ← Image (SmallRibbons.png), 상단 고정
        │   └── TitleText       ← TextMeshProUGUI (가운데 정렬)
        ├── ContentText         ← TextMeshProUGUI (중앙, 여백 포함)
        └── ButtonArea          ← HorizontalLayoutGroup
            ├── OkButton        ← Button + Image (BigBlueButton sprites, Transition: SpriteSwap)
            │   └── OkText      ← TextMeshProUGUI ("확인")
            └── CancelButton    ← Button + Image (BigRedButton sprites, Transition: SpriteSwap)
                └── CancelText  ← TextMeshProUGUI ("취소")
```

**Step 1: Resources/Popups 폴더 생성**

Unity MCP `manage_asset` (`action: "create_folder"`)으로 `Resources/Popups` 폴더 생성.

**Step 2: 프리팹 계층 GameObject 생성**

Unity MCP `manage_gameobject` / `manage_scene` 툴을 사용해 위 계층 구조대로 GameObject를 생성한다.

생성 순서:
1. `CommonPopup` (빈 GameObject, Canvas 컴포넌트 추가, Screen Space - Overlay)
2. `CommonPopup/Overlay` (Image 컴포넌트, RectTransform FullScreen stretch, 반투명 검정)
3. `CommonPopup/Overlay/Panel` (Image + RegularPaper.png, Image Type = Sliced, RectTransform 500×300)
4. `Panel/TitleArea` (Image + SmallRibbons.png, 상단 앵커)
5. `TitleArea/TitleText` (TextMeshProUGUI, 가운데 정렬)
6. `Panel/ContentText` (TextMeshProUGUI, 중앙, 여백)
7. `Panel/ButtonArea` (HorizontalLayoutGroup)
8. `ButtonArea/OkButton` (Button + Image, BigBlueButton Sprite)
9. `OkButton/OkText` (TextMeshProUGUI, "확인")
10. `ButtonArea/CancelButton` (Button + Image, BigRedButton Sprite)
11. `CancelButton/CancelText` (TextMeshProUGUI, "취소")

**Step 3: CommonPopup 스크립트 연결**

`CommonPopup` GameObject에 `CommonPopup` 컴포넌트 추가. Inspector에서 SerializeField 연결:
- `titleText` → TitleText
- `contentText` → ContentText
- `okButton` → OkButton
- `cancelButton` → CancelButton

**Step 4: 프리팹으로 저장**

`Assets/Resources/Popups/CommonPopup.prefab`으로 저장.

**Step 5: 커밋**

```bash
git add Assets/Resources/Popups/
git add Assets/Resources/Popups.meta
git commit -m "CommonPopup 프리팹 생성 (Resources/Popups/CommonPopup)"
```

---

## Task 5: PlayPage에 설정 버튼 이벤트 추가

**파일:**
- Modify: `Assets/Scripts/02.Page/PlayPage.cs`

**Step 1: 파일 수정**

`OnSettingClicked` 이벤트를 추가하고 `OnSettingButtonClicked()`에서 발생시킨다.

변경 전:
```csharp
private void OnSettingButtonClicked()
{
    // TODO: 세팅 팝업 열기 (Phase 3에서 구현)
    Facade.Logger.Log("Setting button clicked");
}
```

변경 후:
```csharp
public event System.Action OnSettingClicked;

private void OnSettingButtonClicked()
{
    OnSettingClicked?.Invoke();
}
```

**Step 2: 컴파일 확인**

Unity Editor Console에서 컴파일 에러 없음 확인.

**Step 3: 커밋**

```bash
git add Assets/Scripts/02.Page/PlayPage.cs
git commit -m "PlayPage: 설정 버튼 클릭 이벤트(OnSettingClicked) 노출"
```

---

## Task 6: InPlayState에서 팝업 열기 연결

**파일:**
- Modify: `Assets/Scripts/01.Scene/PlayScene/States/InPlayState.cs`

**Step 1: 파일 수정**

`OnExecuteAsync` 내에서 이벤트 구독 및 `finally`에서 해제. 팝업 열기 메서드 추가.

추가할 필드/메서드:

```csharp
// OnExecuteAsync 내, playPage null 체크 이후에 추가
void OnSettingClicked() => OpenSettingPopupAsync(playStates).Forget();
playPage.OnSettingClicked += OnSettingClicked;
```

```csharp
// finally 블록에 추가
playPage.OnSettingClicked -= OnSettingClicked;
```

```csharp
// 클래스 멤버 메서드로 추가
private async UniTaskVoid OpenSettingPopupAsync(PlayStates playStates)
{
    var param = new CommonPopupParam("설정", "게임이 일시정지됩니다.", hasTwoButtons: false);
    bool result = await playStates.PopupManager.ShowAsync<bool>("Popups/CommonPopup", param);
    Facade.Logger.Log($"[InPlayState] 설정 팝업 닫힘: {result}");
}
```

**Step 2: 수정 후 전체 OnExecuteAsync 구조 확인**

```csharp
protected override async UniTask OnExecuteAsync()
{
    var playStates = (PlayStates)StateMachine;
    playPage = playStates.PageChanger.CurrentPage as PlayPage;

    if (playPage == null) { ... return; }

    // ... 기존 초기화 코드 ...

    void OnSettingClicked() => OpenSettingPopupAsync(playStates).Forget();
    playPage.OnSettingClicked += OnSettingClicked;

    try
    {
        await UniTask.WaitUntil(() => false, cancellationToken: this.GetCancellationTokenOnDestroy());
    }
    finally
    {
        playPage.OnSettingClicked -= OnSettingClicked;
        cameraShake?.Dispose();
        gameController.Cleanup();
        gameController = null;
    }
}
```

**Step 3: 컴파일 확인**

Unity Editor Console에서 컴파일 에러 없음 확인.

**Step 4: 커밋**

```bash
git add Assets/Scripts/01.Scene/PlayScene/States/InPlayState.cs
git commit -m "InPlayState: 설정 버튼 이벤트 구독 → CommonPopup 열기 연결"
```

---

## Task 7: 수동 검증

**Step 1: PlayMode 진입**

Unity Editor에서 Play 버튼을 눌러 게임 실행.

**Step 2: 설정 버튼 클릭**

PlayPage의 설정 버튼(settingButton) 클릭.

**검증 항목:**
- [ ] 팝업이 화면 중앙에 표시된다
- [ ] TitleText에 "설정"이 표시된다
- [ ] ContentText에 "게임이 일시정지됩니다."가 표시된다
- [ ] OK 버튼(파란색)만 표시된다 (HasTwoButtons=false)
- [ ] OK 버튼 클릭 시 팝업이 닫힌다
- [ ] Console에 `[InPlayState] 설정 팝업 닫힘: True` 출력 확인

**Step 3: 2버튼 케이스 확인 (선택)**

`InPlayState.OpenSettingPopupAsync`에서 임시로 `hasTwoButtons: true`로 변경하여:
- [ ] OK + Cancel 버튼이 모두 표시된다
- [ ] Cancel 클릭 시 Console에 `False` 출력 확인

변경 후 다시 `false`로 복구.

---

## 파일 요약

| 작업 | 파일 |
|------|------|
| 신규 생성 | `Assets/Scripts/02.Page/CommonPopupParam.cs` |
| 신규 생성 | `Assets/Scripts/02.Page/CommonPopup.cs` |
| 신규 생성 | `Assets/Resources/Popups/CommonPopup.prefab` |
| 수정 | `Assets/Scripts/02.Page/PlayPage.cs` |
| 수정 | `Assets/Scripts/01.Scene/PlayScene/States/InPlayState.cs` |
| 수정 (meta) | 텍스쳐 `.meta` 파일들 (Sprite 설정) |
