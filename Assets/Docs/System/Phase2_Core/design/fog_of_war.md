# 2.8 전장의 안개 (Fog of War)

> 상위 문서: [Phase 2 설계](../design.md)

미탐색 지역을 어둡게 처리하고 플레이어 이동에 따라 시야를 공개한다.
그리드 기반 `FogState[,]`로 상태를 관리하며 월드 공간 `SpriteRenderer`로 렌더링한다.

---

## 설계 원칙

**FogOfWar 그리드는 반드시 ObstacleGrid와 동일한 크기·원점을 공유해야 한다.**

이 원칙이 지켜져야 두 가지가 보장된다.

| 보장 | 이유 |
|------|------|
| 월드맵 안개가 지형과 정확히 정렬 | FogRenderer 크기 = `width * cellSize` × `height * cellSize`, 위치 = `origin` |
| 미니맵과 월드맵이 같은 모양 | Minimap이 `fogOfWar.GetState(gx, gy)` 샘플링 시 동일 그리드 좌표 사용 |

---

## FogOfWar (MonoBehaviour)

직렬화 필드로 그리드 크기를 직접 입력하지 않는다.
`Initialize(ObstacleGrid)`를 통해 MapGenerator가 생성한 ObstacleGrid로부터
크기·원점을 받아 초기화하여 불일치를 원천 차단한다.

```csharp
public class FogOfWar : MonoBehaviour
{
    [Header("렌더링")]
    [SerializeField] private SpriteRenderer fogRenderer;
    [SerializeField] private int viewRadius = 5;   // 시야 반경 (그리드 셀 단위)

    // 런타임 — Initialize() 이후 확정
    private FogState[,] fogGrid;
    private Texture2D   fogTexture;
    private int         width;
    private int         height;
    private float       cellSize;
    private Vector2     origin;

    /// <summary>
    /// MapGenerator.Generate() 직후 InPlayState에서 호출한다.
    /// ObstacleGrid 치수를 그대로 복사하여 그리드 불일치를 방지한다.
    /// </summary>
    public void Initialize(ObstacleGrid obstacleGrid) { ... }

    /// <summary>플레이어가 이동할 때마다 호출하여 시야를 갱신한다.</summary>
    public void RevealAround(Vector2 worldPos) { ... }

    public bool     IsRevealed(Vector2 worldPos) { ... }
    public FogState GetState(int x, int y) { ... }

    // 스냅샷 연동
    public FogState[,] CopyFogGrid()                    { ... }
    public void         RestoreFogGrid(FogState[,] grid) { ... }

    private void UpdateTexture() { ... }
}
```

---

## 초기화 (Initialize)

```
width    = obstacleGrid.Width
height   = obstacleGrid.Height
cellSize = obstacleGrid.CellSize
origin   = obstacleGrid.Origin
↓
fogGrid    = new FogState[width, height]  // 전부 Hidden
fogTexture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false)
fogTexture.filterMode = FilterMode.Point
↓
// FogRenderer를 맵 전체에 맞게 배치
fogRenderer.transform.position = (Vector3)(origin + new Vector2(
    width  * cellSize * 0.5f,
    height * cellSize * 0.5f))             // 맵 중심
fogRenderer.size = new Vector2(
    width  * cellSize,
    height * cellSize)                     // 맵 전체 크기
fogRenderer.sprite = /* fogTexture를 사용하는 런타임 Sprite 생성 */
↓
UpdateTexture()                            // 초기 상태: 전부 Hidden
```

> `SpriteRenderer.size`를 사용하려면 `drawMode = SpriteDrawMode.Sliced` 또는
> transform.localScale로 크기를 조절한다.
> 두 방법 모두 유효하며 구현 시 선택한다.

---

## 시야 공개 (RevealAround)

```
gridCenter = WorldToGrid(worldPos)

for each cell (gx, gy) within viewRadius (원형 범위):
    if fogGrid[gx, gy] == Hidden → Explored
    if fogGrid[gx, gy] == Explored → Visible  (이전 프레임에서 Visible이었던 셀)

// 이전 Visible 셀을 Explored로 강등 후 새 Visible 셀 설정하는 2-pass 방식 채택
// Pass 1: 기존 Visible → Explored
// Pass 2: viewRadius 내 셀 → Visible

UpdateTexture()
```

---

## 그리드 ↔ 월드 좌표 변환

```csharp
private Vector2Int WorldToGrid(Vector2 worldPos)
{
    var local = worldPos - origin;
    return new Vector2Int(
        Mathf.FloorToInt(local.x / cellSize),
        Mathf.FloorToInt(local.y / cellSize));
}
```

ObstacleGrid.WorldToGrid()와 동일한 로직을 사용한다.
두 클래스가 같은 `origin`과 `cellSize`를 공유하므로 그리드 좌표가 1:1 대응한다.

---

## 텍스처 갱신 (UpdateTexture)

```
for each cell (x, y):
    color = fogGrid[x, y] switch
        Hidden   → (0, 0, 0, 1)       // 완전히 어두움
        Explored → (0, 0, 0, 0.6f)    // 반투명 어두움
        Visible  → (0, 0, 0, 0)       // 투명 (지형 표시)

fogTexture.SetPixels(colors)
fogTexture.Apply()
```

SpriteRenderer의 스프라이트는 이 알파값으로 지형을 가린다.

---

## FogState

```csharp
public enum FogState
{
    Hidden,     // 미탐색 — 완전히 어두움
    Explored,   // 탐색 완료 — 반투명 처리
    Visible     // 현재 시야 내 — 완전히 표시
}
```

| 상태 | 설명 |
|------|------|
| Hidden | 플레이어가 아직 방문하지 않은 영역 |
| Explored | 과거에 방문했으나 현재 시야 밖인 영역 |
| Visible | 현재 플레이어 시야 범위 내 영역 |

---

## InPlayState 연동

```csharp
// OnExecuteAsync() — MapGenerator.Generate() 직후
var obstacleGrid = playPage.WorldMap.MapGenerator.ObstacleGrid;
playPage.FogOfWar?.Initialize(obstacleGrid);

// Update() — 플레이어 이동마다
playPage.FogOfWar?.RevealAround(gameController.Player.Transform.position);
```

`PlayPage`에 `[SerializeField] private FogOfWar fogOfWar` 필드를 추가한다.

---

## 미니맵과의 관계

Minimap(2.9)은 **동일한 FogOfWar 인스턴스**의 `GetState(gx, gy)`를 읽어 텍스처를 페인팅한다.
FogOfWar 그리드와 ObstacleGrid가 같은 크기·원점을 공유하므로,
미니맵의 픽셀 좌표 (gx, gy)가 월드맵의 그리드 좌표와 1:1 대응하여 **같은 모양**이 보장된다.

```
ObstacleGrid (width×height, cellSize, origin)
     ├── FogOfWar  — Initialize(obstacleGrid) → 동일 그리드 공유
     │      ├── 월드맵 SpriteRenderer (안개 시각화)
     │      └── GetState(gx, gy) ←── Minimap이 샘플링
     └── Minimap  — Initialize(obstacleGrid, fogOfWar)
            └── 텍스처 픽셀 (gx, gy) = FogState + walkable
```
