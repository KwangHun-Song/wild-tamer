# 전장의 안개 & 미니맵 구현 계획

## 개요

Step 12(전장의 안개)와 Step 13(미니맵) 구현 계획이다.
`FogOfWar.cs`와 `Minimap.cs`는 스텁 수준으로 이미 존재하나,
설계 문서([fog_of_war.md](design/fog_of_war.md), [minimap.md](design/minimap.md))와 불일치하는 부분이 있어
리팩토링 + 기능 추가 형태로 진행한다.

### 현재 구현 vs 설계 간 주요 차이

| 파일 | 현재 구현 | 설계 요구사항 |
|------|----------|-------------|
| `FogOfWar` | `Awake()`에서 SerializedField로 초기화 | `Initialize(ObstacleGrid)` — MapGenerator 이후 호출 |
| `FogOfWar` | `filterMode = Bilinear` | `filterMode = Point` |
| `FogOfWar` | FogRenderer 위치·크기 자동 설정 없음 | 맵 전체에 맞게 자동 배치 |
| `Minimap` | Texture2D 지형 페인팅 없음 | `PaintTexture()` — 보행 가능·장애물·FogState 색상 표현 |
| `Minimap` | `worldMin/worldMax` SerializedField | `Initialize(ObstacleGrid, FogOfWar)` — 맵 경계 자동 도출 |
| `Minimap` | 적 가시성: `IsRevealed()` (Explored 포함) | `GetState() == Visible`만 표시 |
| `Minimap` | 아군 가시성: `IsRevealed()` 조건 검사 | 항상 표시 |
| `ObstacleGrid` | Width/Height/CellSize/Origin 프로퍼티 없음 | 4개 public 프로퍼티 필요 |
| `ObstacleGrid` | `IsWalkableAtGrid(int, int)` 없음 | 그리드 좌표 직접 조회 필요 |
| `GameController` | `ActiveMonsters` 프로퍼티 없음 | Minimap 적 아이콘 갱신에 필요 |
| `PlayPage` | `Minimap` 레퍼런스 없음 | `[SerializeField] Minimap minimap` 추가 |
| `InPlayState` | FogOfWar/Minimap 연결 없음 | Initialize + Update 연결 필요 |

---

## 아키텍처 개요

### 수정 파일

| 파일 | 변경 내용 |
|------|----------|
| `ObstacleGrid.cs` | Width, Height, CellSize, Origin 프로퍼티 + `IsWalkableAtGrid(int, int)` 추가 |
| `FogOfWar.cs` | Awake/SerializedField 방식 → `Initialize(ObstacleGrid)` 방식으로 리팩토링 |
| `Minimap.cs` | `Initialize(ObstacleGrid, FogOfWar)` + `PaintTexture()` 추가, 가시성 로직 수정 |
| `GameController.cs` | `ActiveMonsters` 프로퍼티 추가 |
| `PlayPage.cs` | `Minimap` SerializedField + 프로퍼티 추가 |
| `InPlayState.cs` | FogOfWar.Initialize, Minimap.Initialize, Update 연결 추가 |

### Unity 작업 (프리팹)

| 대상 | 작업 |
|------|------|
| `WorldMap.prefab` 또는 씬 내 FogOfWar GO | FogRenderer(SpriteRenderer) 연결, viewRadius 설정 |
| `PlayPage.prefab` | MinimapPanel UI 계층 구성, Minimap 컴포넌트 연결 |

---

## 단계별 구현 순서

### Step 1 — ObstacleGrid 프로퍼티 추가

**수정 파일:** `Assets/Scripts/04.Game/02.System/Map/ObstacleGrid.cs`

```csharp
// 기존 private 필드에 public 프로퍼티 노출
public int     Width    => width;
public int     Height   => height;
public float   CellSize => cellSize;
public Vector2 Origin   => origin;

// 그리드 좌표 직접 조회 (Minimap 전용)
public bool IsWalkableAtGrid(int x, int y)
{
    if (x < 0 || x >= width || y < 0 || y >= height) return false;
    return walkable[x, y];
}
```

---

### Step 2 — FogOfWar 리팩토링: Initialize(ObstacleGrid) [병렬 가능: Step 3과]

**수정 파일:** `Assets/Scripts/04.Game/02.System/Map/FogOfWar.cs`

**제거:**
- `[SerializeField] private int gridWidth`
- `[SerializeField] private int gridHeight`
- `[SerializeField] private float cellSize`
- `[SerializeField] private Vector2 origin`
- `private void Awake()` (InitializeGrid + InitializeTexture 호출부)

**추가:**
```csharp
[Header("렌더링")]
[SerializeField] private SpriteRenderer fogRenderer;
[SerializeField] private int viewRadius = 5;

// 런타임 — Initialize() 이후 확정
private FogState[,] fogGrid;
private Texture2D   fogTexture;
private Color[]     colorBuffer;
private bool        isDirty;
private int         width;
private int         height;
private float       cellSize;
private Vector2     origin;

/// <summary>
/// MapGenerator.Generate() 직후 InPlayState에서 호출한다.
/// ObstacleGrid 치수를 복사해 그리드 불일치를 방지한다.
/// </summary>
public void Initialize(ObstacleGrid obstacleGrid)
{
    width    = obstacleGrid.Width;
    height   = obstacleGrid.Height;
    cellSize = obstacleGrid.CellSize;
    origin   = obstacleGrid.Origin;

    // 그리드 초기화 (전부 Hidden)
    fogGrid     = new FogState[width, height];
    fogTexture  = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
    fogTexture.filterMode = FilterMode.Point;
    colorBuffer = new Color[width * height];

    // FogRenderer를 맵 전체에 맞게 배치
    if (fogRenderer != null)
    {
        fogRenderer.transform.position = (Vector3)(origin + new Vector2(
            width  * cellSize * 0.5f,
            height * cellSize * 0.5f));
        fogRenderer.transform.localScale = new Vector3(
            width  * cellSize,
            height * cellSize,
            1f);
        fogRenderer.sprite = Sprite.Create(
            fogTexture,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f),   // 피벗: 중앙
            1f);
    }

    UpdateTexture();
}
```

**RevealAround — 변수명만 갱신 (width/height로 교체):**

기존 `gridWidth` / `gridHeight` 참조를 `width` / `height`로 변경한다.

---

### Step 3 — Minimap 리팩토링: Initialize + PaintTexture [병렬 가능: Step 2와]

**수정 파일:** `Assets/Scripts/04.Game/02.System/Map/Minimap.cs`

**제거:**
- `[SerializeField] private FogOfWar fogOfWar`
- `[SerializeField] private Vector2 worldMin`
- `[SerializeField] private Vector2 worldMax`
- `private void Awake()`

**추가할 필드:**
```csharp
[Header("UI 레퍼런스")]
[SerializeField] private RawImage      mapImage;
[SerializeField] private RectTransform iconContainer;  // mapImage와 동일 Rect, pivot (0,0)

[Header("아이콘 프리팹")]
[SerializeField] private Image playerIconPrefab;
[SerializeField] private Image allyIconPrefab;
[SerializeField] private Image enemyIconPrefab;

[Header("설정")]
[SerializeField] private int maxTextureResolution  = 256;
[SerializeField] private int textureRefreshInterval = 30;

// 런타임 상태
private ObstacleGrid obstacleGrid;
private FogOfWar     fogOfWar;     // null 허용 — 없으면 모두 Visible

private Texture2D    mapTexture;
private int          texWidth;
private int          texHeight;

private Image              playerIcon;
private readonly List<Image> allyIcons  = new();
private readonly List<Image> enemyIcons = new();

private Vector2 mapOrigin;
private float   mapWorldWidth;
private float   mapWorldHeight;

private int frameCounter;

private static readonly Color ColorWalkable = new Color(0.25f, 0.45f, 0.25f, 1f);
private static readonly Color ColorObstacle = new Color(0.15f, 0.15f, 0.15f, 1f);
private static readonly Color ColorHidden   = new Color(0f,    0f,    0f,    1f);
```

**Initialize:**
```csharp
public void Initialize(ObstacleGrid obstacleGrid, FogOfWar fogOfWar = null)
{
    this.obstacleGrid = obstacleGrid;
    this.fogOfWar     = fogOfWar;

    texWidth  = Mathf.Min(obstacleGrid.Width,  maxTextureResolution);
    texHeight = Mathf.Min(obstacleGrid.Height, maxTextureResolution);

    mapTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, mipChain: false);
    mapTexture.filterMode = FilterMode.Point;
    if (mapImage != null) mapImage.texture = mapTexture;

    mapOrigin      = obstacleGrid.Origin;
    mapWorldWidth  = obstacleGrid.Width  * obstacleGrid.CellSize;
    mapWorldHeight = obstacleGrid.Height * obstacleGrid.CellSize;

    if (playerIconPrefab != null)
        playerIcon = Instantiate(playerIconPrefab, iconContainer);

    PaintTexture();
}
```

**PaintTexture — 지형·안개 텍스처 페인팅:**
```csharp
private void PaintTexture()
{
    if (obstacleGrid == null || mapTexture == null) return;

    for (int x = 0; x < texWidth; x++)
    {
        for (int y = 0; y < texHeight; y++)
        {
            int gx = x * obstacleGrid.Width  / texWidth;
            int gy = y * obstacleGrid.Height / texHeight;

            bool walkable = obstacleGrid.IsWalkableAtGrid(gx, gy);
            var  fog      = fogOfWar?.GetState(gx, gy) ?? FogState.Visible;

            var baseColor = walkable ? ColorWalkable : ColorObstacle;
            Color pixel = fog switch
            {
                FogState.Hidden   => ColorHidden,
                FogState.Explored => Color.Lerp(baseColor, ColorHidden, 0.6f),
                _                 => baseColor   // Visible
            };
            mapTexture.SetPixel(x, y, pixel);
        }
    }
    mapTexture.Apply();
}
```

**Refresh — 서명 변경 + 아이콘 가시성 수정:**
```csharp
public void Refresh(
    Player player,
    IReadOnlyList<SquadMember> allies,
    IReadOnlyList<Monster>     enemies)
{
    if (mapTexture == null) return;

    // 지형·안개 텍스처 주기적 재페인팅
    frameCounter++;
    if (fogOfWar != null && frameCounter >= textureRefreshInterval)
    {
        frameCounter = 0;
        PaintTexture();
    }

    RefreshIcons(player, allies, enemies);
}
```

**RefreshIcons — 가시성 로직:**
```csharp
private void RefreshIcons(
    Player player,
    IReadOnlyList<SquadMember> allies,
    IReadOnlyList<Monster>     enemies)
{
    // 플레이어 (1개 고정)
    if (playerIcon != null && player != null)
        playerIcon.rectTransform.anchoredPosition = WorldToMinimapPos(player.Transform.position);

    // 아군 — 항상 표시
    AdjustPool(allyIcons, allies.Count, allyIconPrefab);
    for (int i = 0; i < allies.Count; i++)
    {
        allyIcons[i].gameObject.SetActive(true);
        allyIcons[i].rectTransform.anchoredPosition =
            WorldToMinimapPos(allies[i].Transform.position);
    }
    HideExcess(allyIcons, allies.Count);

    // 적 — Visible 셀에 있는 적만 표시 (이동하므로 Explored는 의미 없음)
    int shown = 0;
    foreach (var enemy in enemies)
    {
        var g = WorldToGrid(enemy.Transform.position);
        bool visible = fogOfWar == null
            || fogOfWar.GetState(g.x, g.y) == FogState.Visible;
        if (!visible) continue;

        AdjustPool(enemyIcons, shown + 1, enemyIconPrefab);
        enemyIcons[shown].gameObject.SetActive(true);
        enemyIcons[shown].rectTransform.anchoredPosition =
            WorldToMinimapPos(enemy.Transform.position);
        shown++;
    }
    HideExcess(enemyIcons, shown);
}
```

**좌표 변환 헬퍼:**
```csharp
private Vector2 WorldToMinimapPos(Vector2 worldPos)
{
    float u = Mathf.Clamp01((worldPos.x - mapOrigin.x) / mapWorldWidth);
    float v = Mathf.Clamp01((worldPos.y - mapOrigin.y) / mapWorldHeight);
    var size = iconContainer.rect.size;
    return new Vector2(u * size.x, v * size.y);
}

private Vector2Int WorldToGrid(Vector2 worldPos)
{
    return new Vector2Int(
        Mathf.FloorToInt((worldPos.x - mapOrigin.x) / obstacleGrid.CellSize),
        Mathf.FloorToInt((worldPos.y - mapOrigin.y) / obstacleGrid.CellSize));
}

private void AdjustPool(List<Image> pool, int needed, Image prefab)
{
    while (pool.Count < needed && prefab != null)
        pool.Add(Instantiate(prefab, iconContainer));
}

private void HideExcess(List<Image> pool, int activeCount)
{
    for (int i = activeCount; i < pool.Count; i++)
        if (pool[i] != null) pool[i].gameObject.SetActive(false);
}
```

> **기존 `Refresh(Transform, IReadOnlyList<SquadMember>, IReadOnlyList<Monster>)` → `Refresh(Player, ...)` 로 변경.**
> `Player.Transform`으로 접근하므로 InPlayState 호출부도 함께 수정.

---

### Step 4 — GameController: ActiveMonsters 프로퍼티 [병렬 가능: Step 2, 3과]

**수정 파일:** `Assets/Scripts/04.Game/02.System/Game/GameController.cs`

```csharp
// entitySpawner.ActiveMonsters를 외부에 노출
public IReadOnlyList<Monster> ActiveMonsters => entitySpawner.ActiveMonsters;
```

---

### Step 5 — PlayPage: Minimap 레퍼런스 추가 [병렬 가능: Step 2, 3과]

**수정 파일:** `Assets/Scripts/02.Page/PlayPage.cs`

```csharp
[SerializeField] private Minimap minimap;
public Minimap Minimap => minimap;
```

---

### Step 6 — InPlayState: Initialize + Update 연결 (Step 2~5 완료 후)

**수정 파일:** `Assets/Scripts/01.Scene/PlayScene/States/InPlayState.cs`

```csharp
// OnExecuteAsync() — GameController 생성 직후

// FogOfWar 초기화 (맵 크기·원점을 ObstacleGrid에서 받음)
var obstacleGrid = playPage.WorldMap.MapGenerator.ObstacleGrid;
playPage.FogOfWar?.Initialize(obstacleGrid);

// Minimap 초기화 (FogOfWar와 동일한 그리드 공유)
playPage.Minimap?.Initialize(obstacleGrid, playPage.FogOfWar);
```

```csharp
// Update()
private void Update()
{
    gameController?.Update();

    // FogOfWar: 플레이어 이동마다 시야 갱신
    if (gameController != null && playPage != null)
    {
        playPage.FogOfWar?.RevealAround(gameController.Player.Transform.position);
        playPage.Minimap?.Refresh(
            gameController.Player,
            gameController.Squad.Members,
            gameController.ActiveMonsters);
    }

    HandleCheatInput();
}
```

> `playPage` 필드를 `InPlayState`의 멤버 변수로 캐싱한다.
> 현재 `playPage`가 `OnExecuteAsync()` 지역 변수이므로 `private PlayPage playPage` 멤버 필드로 승격시킨다.

---

### Step 7 — PlayPage 프리팹: FogOfWar 게임오브젝트 설정 (Step 2 완료 후)

Unity 에디터 작업.

```
WorldMap (or PlayPage 하위)
└── FogOfWar (GameObject)
    ├── FogOfWar 컴포넌트
    │   ├── fogRenderer: 하위 SpriteRenderer 연결
    │   └── viewRadius: 5 (기본값)
    └── FogRenderer (GameObject)
        └── SpriteRenderer
            └── sortingLayer: Fog (또는 Default), sortingOrder: 높은 값 (지형 위)
```

`PlayPage.cs`에 `[SerializeField] private FogOfWar fogOfWar` 및 `public FogOfWar FogOfWar => fogOfWar` 추가가 필요하다.

---

### Step 8 — PlayPage 프리팹: MinimapPanel UI 계층 구성 (Step 3 완료 후)

Unity 에디터 작업.

```
PlayPage (Canvas)
└── MinimapPanel (RectTransform — 우상단 앵커)
    ├── MinimapFrame (Image — 테두리/배경)
    ├── MapImage (RawImage — Texture2D 표시, pivot 0,0)
    │   └── Minimap 컴포넌트
    │       ├── mapImage: MapImage 연결
    │       ├── iconContainer: IconContainer 연결
    │       ├── playerIconPrefab: PlayerIcon 프리팹
    │       ├── allyIconPrefab: AllyIcon 프리팹
    │       └── enemyIconPrefab: EnemyIcon 프리팹
    └── IconContainer (RectTransform — MapImage와 동일 Rect, pivot 0,0)
```

`MinimapPanel` 설정:
- anchorMin/anchorMax: `(1, 1)` (우상단)
- pivot: `(1, 1)`
- 크기: `160 × 160` px

`PlayPage.cs`의 `Minimap` SerializedField에 Minimap 컴포넌트 연결.

---

## 검증 체크리스트

### Step 1 — ObstacleGrid
- [ ] `Width` / `Height` / `CellSize` / `Origin` 프로퍼티 컴파일 통과
- [ ] `IsWalkableAtGrid(-1, 0)` → false (경계 외 처리)
- [ ] `IsWalkableAtGrid(gx, gy)` → `IsWalkable(worldPos)` 결과와 동일

### Step 2 — FogOfWar
- [ ] `Awake()` 제거 후 컴파일 오류 없음
- [ ] `Initialize(obstacleGrid)` 호출 후 fogGrid, fogTexture 할당됨
- [ ] FogRenderer 위치·크기가 맵 전체와 일치함 (에디터에서 Gizmo 확인)
- [ ] filterMode = Point (픽셀 선명)
- [ ] 플레이어 이동 시 안개가 원형으로 걷힘
- [ ] 맵 경계에서 IndexOutOfRange 없음

### Step 3 — Minimap
- [ ] `Initialize(obstacleGrid)` 호출 후 mapTexture 할당됨
- [ ] PaintTexture — 보행 가능 셀은 초록(Visible), 장애물은 회색(Visible)
- [ ] PaintTexture — Hidden 셀은 검정, Explored 셀은 어두운 기본색
- [ ] 플레이어 아이콘이 이동에 따라 미니맵에서 움직임
- [ ] 아군 아이콘은 FogState와 관계없이 항상 표시
- [ ] 적 아이콘은 `GetState() == Visible`일 때만 표시 (Explored 영역 적은 미표시)
- [ ] `fogOfWar = null`일 때 전체 지형이 표시되고 적 아이콘도 항상 표시

### Step 4 — GameController
- [ ] `ActiveMonsters` 프로퍼티가 `entitySpawner.ActiveMonsters`를 반환

### Step 5 — PlayPage
- [ ] `Minimap` 프로퍼티 컴파일 통과

### Step 6 — InPlayState
- [ ] `playPage` 멤버 변수 캐싱 후 Update에서 접근 가능
- [ ] `FogOfWar.Initialize()` — MapGenerator.Generate() 이후, GameController 생성 전 호출
- [ ] `Minimap.Initialize()` — FogOfWar.Initialize() 이후 호출
- [ ] `Update()` — FogOfWar.RevealAround 매 프레임 호출
- [ ] `Update()` — Minimap.Refresh 매 프레임 호출

### Step 7 — FogOfWar 프리팹
- [ ] FogOfWar 컴포넌트에 FogRenderer 연결됨
- [ ] 씬 실행 시 맵 전체가 검정으로 덮임
- [ ] 플레이어 이동 시 주변이 밝아짐

### Step 8 — MinimapPanel 프리팹
- [ ] MinimapPanel이 우상단에 고정됨
- [ ] iconContainer.pivot = (0, 0)
- [ ] MapImage.pivot = (0, 0)
- [ ] Minimap 컴포넌트 직렬화 레퍼런스 모두 연결됨

---

## 작업 분류

| Step | 내용 | 선행 조건 | 병렬 여부 |
|------|------|----------|----------|
| Step 1 | ObstacleGrid 프로퍼티 + IsWalkableAtGrid | 없음 | [병렬 가능] Step 4, 5와 |
| Step 2 | FogOfWar Initialize(ObstacleGrid) 리팩토링 | Step 1 완료 | [병렬 가능] Step 3과 |
| Step 3 | Minimap Initialize + PaintTexture 구현 | Step 1 완료 | [병렬 가능] Step 2와 |
| Step 4 | GameController ActiveMonsters 프로퍼티 | 없음 | [병렬 가능] Step 1~3과 |
| Step 5 | PlayPage Minimap 레퍼런스 추가 | 없음 | [병렬 가능] Step 1~4와 |
| Step 6 | InPlayState Initialize + Update 연결 | Step 2~5 완료 | 단독 |
| Step 7 | PlayPage 프리팹 FogOfWar GO 설정 | Step 2, 6 완료 | [병렬 가능] Step 8과 |
| Step 8 | PlayPage 프리팹 MinimapPanel UI 구성 | Step 3, 6 완료 | [병렬 가능] Step 7과 |

### 병렬 실행 웨이브

```
Wave 1 ── 병렬
  Step 1: ObstacleGrid 프로퍼티
  Step 4: GameController ActiveMonsters
  Step 5: PlayPage Minimap 레퍼런스
          ↓ Wave 1 완료
Wave 2 ── 병렬
  Step 2: FogOfWar 리팩토링
  Step 3: Minimap 리팩토링
          ↓ Wave 2 완료
Wave 3 ── 단독
  Step 6: InPlayState 연결
          ↓ Wave 3 완료
Wave 4 ── 병렬
  Step 7: FogOfWar 프리팹 설정
  Step 8: MinimapPanel UI 구성
```

---

## 관련 파일

| 파일 | 구분 | 역할 |
|------|------|------|
| `04.Game/02.System/Map/ObstacleGrid.cs` | 수정 | Width/Height/CellSize/Origin + IsWalkableAtGrid |
| `04.Game/02.System/Map/FogOfWar.cs` | 수정 | Awake 제거, Initialize(ObstacleGrid) 추가 |
| `04.Game/02.System/Map/Minimap.cs` | 수정 | Initialize + PaintTexture + 가시성 로직 수정 |
| `04.Game/02.System/Game/GameController.cs` | 수정 | ActiveMonsters 프로퍼티 |
| `02.Page/PlayPage.cs` | 수정 | FogOfWar + Minimap 레퍼런스 |
| `01.Scene/PlayScene/States/InPlayState.cs` | 수정 | playPage 캐싱, FogOfWar/Minimap 연결 |
| `PlayPage.prefab` | Unity 작업 | FogOfWar GO + MinimapPanel UI |
