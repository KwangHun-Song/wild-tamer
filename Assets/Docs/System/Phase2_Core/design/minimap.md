# 2.9 미니맵

> 상위 문서: [Phase 2 설계](../design.md)

화면 우상단에 미니맵 UI를 표시하여 지형 탐색 현황과 유닛 위치를 실시간으로 반영한다.

---

## 설계 목표

| 목표 | 설명 |
|------|------|
| 지형 표시 | 보행 가능 / 장애물 영역을 Texture2D로 표현 |
| 안개 연동 | FogOfWar 상태에 따라 픽셀 명암 조절 |
| 유닛 아이콘 | 플레이어·아군·적 위치를 RectTransform 아이콘으로 표시 |
| 적 숨김 | 현재 시야(Visible) 밖의 적 아이콘 숨김 |
| FogOfWar 선택적 의존 | FogOfWar 미제공 시에도 독립 동작 (전체 공개) |

---

## 렌더링 아키텍처

두 레이어를 분리하여 갱신 비용을 최소화한다.

```
MinimapPanel (UI)
├── RawImage (mapImage)          ← Texture2D 표시 — 지형 + 안개
└── RectTransform (iconContainer) ← 유닛 아이콘 풀
    ├── PlayerIcon
    ├── AllyIcon_0, AllyIcon_1 … ← 매 프레임 위치 갱신
    └── EnemyIcon_0 …
```

| 레이어 | 구현 | 갱신 주기 |
|--------|------|-----------|
| 지형·안개 | `Texture2D` → `RawImage` | 초기화 1회 + 매 30프레임 |
| 유닛 아이콘 | `RectTransform` (Image 풀링) | 매 프레임 |

> **RenderTexture 카메라 방식 미채택** — FogOfWar와 동일한 그리드 구조를 활용하면 추가 Camera Draw Call 없이 픽셀 단위 제어가 가능하다.

---

## Minimap (MonoBehaviour)

```csharp
/// <summary>
/// 미니맵 UI를 담당한다.
/// Texture2D로 지형을 페인팅하고, Image 아이콘 풀로 유닛 위치를 매 프레임 갱신한다.
/// PlayPage에 직렬화 참조로 포함되며, InPlayState에서 Initialize/Refresh를 호출한다.
/// </summary>
public class Minimap : MonoBehaviour
{
    [Header("UI 레퍼런스")]
    [SerializeField] private RawImage          mapImage;
    [SerializeField] private RectTransform     iconContainer;

    [Header("아이콘 프리팹")]
    [SerializeField] private Image playerIconPrefab;
    [SerializeField] private Image allyIconPrefab;
    [SerializeField] private Image enemyIconPrefab;

    [Header("설정")]
    [SerializeField] private int maxTextureResolution  = 256;  // 최대 텍스처 해상도
    [SerializeField] private int textureRefreshInterval = 30;  // 안개 재페인팅 주기 (프레임)

    // 런타임 상태
    private ObstacleGrid obstacleGrid;
    private FogOfWar     fogOfWar;     // null 허용 — 없으면 모두 Visible

    private Texture2D    mapTexture;
    private int          texWidth;
    private int          texHeight;

    // 아이콘 풀
    private Image              playerIcon;
    private readonly List<Image> allyIcons  = new();
    private readonly List<Image> enemyIcons = new();

    // 좌표 변환 캐시
    private Vector2 mapOrigin;
    private float   mapWorldWidth;
    private float   mapWorldHeight;

    private int frameCounter;

    /// <summary>GameController 생성 직후 InPlayState에서 호출한다.</summary>
    public void Initialize(ObstacleGrid obstacleGrid, FogOfWar fogOfWar = null) { ... }

    /// <summary>InPlayState.Update()에서 매 프레임 호출한다.</summary>
    public void Refresh(
        Player                   player,
        IReadOnlyList<SquadMember> allies,
        IReadOnlyList<Monster>    enemies) { ... }
}
```

---

## 핵심 알고리즘

### 1. 초기화 (Initialize)

```
obstacleGrid, fogOfWar 저장
↓
texWidth  = Min(obstacleGrid.Width,  maxTextureResolution)
texHeight = Min(obstacleGrid.Height, maxTextureResolution)
↓
mapTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, mipChain: false)
mapTexture.filterMode = FilterMode.Point        ← 픽셀 선명도 유지
mapImage.texture = mapTexture
↓
mapOrigin     = obstacleGrid.Origin
mapWorldWidth  = obstacleGrid.Width  * obstacleGrid.CellSize
mapWorldHeight = obstacleGrid.Height * obstacleGrid.CellSize
↓
playerIcon = Instantiate(playerIconPrefab, iconContainer)
PaintTexture()                                  ← 초기 페인팅
```

### 2. 지형·안개 페인팅 (PaintTexture)

그리드 셀 좌표와 텍스처 픽셀을 1:1 매핑한다. 맵이 maxTextureResolution보다 크면 샘플링 비율을 조정한다.

```
for x in [0, texWidth):
  for y in [0, texHeight):
    // 그리드 좌표 (샘플링 비율 적용)
    gx = x * obstacleGrid.Width  / texWidth
    gy = y * obstacleGrid.Height / texHeight

    walkable = obstacleGrid.IsWalkableAtGrid(gx, gy)
    fog      = fogOfWar?.GetState(gx, gy) ?? FogState.Visible

    baseColor = walkable ? ColorWalkable : ColorObstacle
    pixel = fog switch
        Hidden   → ColorHidden
        Explored → Lerp(baseColor, ColorHidden, 0.6f)
        Visible  → baseColor

    mapTexture.SetPixel(x, y, pixel)

mapTexture.Apply()
```

**색상 표**

| 상태 | 색상 | 비고 |
|------|------|------|
| 보행 가능 (Visible) | `(0.25, 0.45, 0.25, 1)` | 짙은 초록 |
| 장애물 (Visible) | `(0.15, 0.15, 0.15, 1)` | 짙은 회색 |
| Explored | baseColor × 40% | 과거 방문 |
| Hidden | `(0, 0, 0, 1)` | 미탐색 |

### 3. 월드 좌표 → 미니맵 UI 좌표 변환

`iconContainer`의 pivot/anchor를 **(0, 0)** 으로 설정한다.
`mapImage`와 `iconContainer`는 동일한 Rect를 공유한다.

```
u = (worldPos.x - mapOrigin.x) / mapWorldWidth      // [0, 1]
v = (worldPos.y - mapOrigin.y) / mapWorldHeight      // [0, 1]
u, v = Clamp01(u, v)

size = iconContainer.rect.size                        // 미니맵 픽셀 크기
anchoredPosition = new Vector2(u * size.x, v * size.y)
```

### 4. 아이콘 갱신 (RefreshIcons)

매 프레임 아이콘을 재배치하며 부족하면 풀에서 생성, 초과분은 비활성화한다.

```
// 플레이어 (1개 고정)
playerIcon.anchoredPosition = WorldToMinimapPos(player.Transform.position)

// 아군 (부대원 수에 맞게)
for i in [0, allies.Count):
    GetOrCreate(allyIcons, allyIconPrefab, i)
        .anchoredPosition = WorldToMinimapPos(allies[i].Transform.position)
HideExcess(allyIcons, allies.Count)

// 적 (Visible 셀에 있는 적만)
shown = 0
for each enemy in enemies:
    visible = fogOfWar == null
              || fogOfWar.GetState(WorldToGrid(enemy.Transform.position)) == FogState.Visible
    if not visible: skip
    GetOrCreate(enemyIcons, enemyIconPrefab, shown++)
        .anchoredPosition = WorldToMinimapPos(enemy.Transform.position)
HideExcess(enemyIcons, shown)
```

> **적 가시성 기준: Visible만** — 적은 이동하므로 과거 탐색(Explored) 위치 정보는 무의미하다.
> **아군 가시성: 항상 표시** — Squad 멤버는 항상 미니맵에 노출한다.

---

## 의존성 및 변경 필요 클래스

### ObstacleGrid — public 프로퍼티 / 메서드 추가

좌표 변환과 텍스처 페인팅에 맵 경계 정보가 필요하다.

```csharp
// 추가 프로퍼티
public int     Width    => width;
public int     Height   => height;
public float   CellSize => cellSize;
public Vector2 Origin   => origin;

// 추가 메서드 — 그리드 좌표로 직접 조회 (Minimap 전용)
public bool IsWalkableAtGrid(int x, int y)
{
    if (x < 0 || x >= width || y < 0 || y >= height) return false;
    return walkable[x, y];
}
```

### GameController — ActiveMonsters 노출

InPlayState에서 적 아이콘 갱신 시 entitySpawner에 직접 접근할 수 없으므로 프로퍼티를 추가한다.

```csharp
public IReadOnlyList<Monster> ActiveMonsters => entitySpawner.ActiveMonsters;
```

### PlayPage — Minimap 레퍼런스 추가

PlayerHpBar와 동일한 패턴으로 추가한다.

```csharp
[SerializeField] private Minimap minimap;
public Minimap Minimap => minimap;
```

### InPlayState — 초기화 및 Update 연결

```csharp
// OnExecuteAsync() — GameController 생성 직후
playPage.Minimap?.Initialize(
    playPage.WorldMap.MapGenerator.ObstacleGrid,
    fogOfWar: null);   // FogOfWar 구현(Step 12) 후 전달

// Update()
playPage.Minimap?.Refresh(
    gameController.Player,
    gameController.Squad.Members,
    gameController.ActiveMonsters);
```

---

## UI 계층 구조

```
PlayPage (Canvas)
└── MinimapPanel (RectTransform — 우상단 앵커 고정)
    ├── MinimapFrame (Image — 테두리/배경)
    ├── MapImage (RawImage — Texture2D 표시, pivot 0,0)
    └── IconContainer (RectTransform — MapImage와 동일 Rect, pivot 0,0)
        ├── PlayerIcon   (Image — 항상 활성)
        ├── AllyIcon_0 … (Image — 풀링, 부대원 수에 따라 활성/비활성)
        └── EnemyIcon_0 … (Image — 풀링, Visible 적에만 활성)
```

`MinimapPanel` 앵커: 우상단 (`anchorMin/Max = (1,1)`)
`MinimapPanel` 기본 크기: 160 × 160 px (Inspector에서 조정 가능)

---

## FogOfWar 미구현 시 동작

| 조건 | 동작 |
|------|------|
| `fogOfWar == null` | 모든 지형을 Visible로 처리, 전체 맵 표시 |
| 적 아이콘 | 항상 표시 |
| 텍스처 재페인팅 | 초기화 1회로 충분 (안개 변화 없음) |

FogOfWar(Step 12) 구현 후에는 `Initialize(obstacleGrid, fogOfWar)` 인자만 추가하면 연동 완료된다.

---

## 스냅샷 연동 (GameSnapshot)

미니맵 자체는 스냅샷 로직을 갖지 않는다.
FogOfWar가 `CopyFogGrid()` / `RestoreFogGrid()`로 안개 상태를 저장/복원하면,
다음 `Refresh()` 호출 시 Minimap이 자동으로 최신 상태를 반영한다.

---

## 구현 순서

| 순서 | 내용 |
|------|------|
| 1 | `ObstacleGrid` — `Width`, `Height`, `CellSize`, `Origin` 프로퍼티 + `IsWalkableAtGrid()` 추가 |
| 2 | `GameController` — `ActiveMonsters` 프로퍼티 추가 |
| 3 | `Minimap.cs` 스크립트 구현 |
| 4 | `PlayPage.cs` — `Minimap` 레퍼런스 추가 |
| 5 | `InPlayState` — `Initialize` / `Refresh` 호출 추가 |
| 6 | PlayPage 프리팹에 MinimapPanel UI 계층 구성 |
| 7 | 아이콘 프리팹(Player/Ally/Enemy) 제작 및 직렬화 연결 |
| 8 | *(선택)* FogOfWar(Step 12) 구현 후 `Initialize`에 `fogOfWar` 전달 |
