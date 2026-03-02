using Base;
using UnityEngine;

public enum FogState
{
    Hidden,     // 미탐색 — 완전히 어두움
    Explored,   // 탐색 완료 — 반투명 처리
    Visible     // 현재 시야 내 — 완전히 표시
}

public class FogOfWar : MonoBehaviour
{
    [Header("렌더링")]
    [SerializeField] private SpriteRenderer fogRenderer;

    // 런타임 — Initialize() 이후 확정
    private FogOfWarData fogData;
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
        fogData  = Facade.DB.Get<FogOfWarData>("FogOfWarData");
        width    = obstacleGrid.Width;
        height   = obstacleGrid.Height;
        cellSize = obstacleGrid.CellSize;
        origin   = obstacleGrid.Origin;

        // 그리드 초기화 (전부 Hidden)
        fogGrid     = new FogState[width, height];
        fogTexture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false)
        {
            filterMode = FilterMode.Point
        };
        colorBuffer = new Color[width * height];

        // FogRenderer를 맵 전체에 맞게 배치
        if (fogRenderer != null)
        {
            fogRenderer.transform.position = (Vector3)(origin + new Vector2(
                width  * cellSize * 0.5f,
                height * cellSize * 0.5f));
            fogRenderer.transform.localScale = new Vector3(
                cellSize,
                cellSize,
                1f);
            fogRenderer.sprite = Sprite.Create(
                fogTexture,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                1f);
            fogRenderer.sortingOrder = SortingOrder.Fog;
        }

        UpdateTexture();
    }

    /// <summary>플레이어 위치 주변 viewRadius 셀을 Visible로 설정한다.</summary>
    public void RevealAround(Vector2 worldPos)
    {
        if (fogGrid == null) return;
        var center = WorldToGrid(worldPos);

        // Pass 1: 기존 Visible → Explored
        for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                if (fogGrid[x, y] == FogState.Visible)
                {
                    fogGrid[x, y] = FogState.Explored;
                    isDirty = true;
                }

        // Pass 2: viewRadius 내 셀 → Visible
        var radius = fogData?.viewRadius ?? 5;
        for (var dx = -radius; dx <= radius; dx++)
        {
            for (var dy = -radius; dy <= radius; dy++)
            {
                var gx = center.x + dx;
                var gy = center.y + dy;
                if (gx < 0 || gx >= width || gy < 0 || gy >= height) continue;
                if (dx * dx + dy * dy > radius * radius) continue;
                if (fogGrid[gx, gy] != FogState.Visible)
                {
                    fogGrid[gx, gy] = FogState.Visible;
                    isDirty = true;
                }
            }
        }

        if (isDirty) UpdateTexture();
    }

    public bool IsRevealed(Vector2 worldPos)
    {
        if (fogGrid == null) return false;
        var g = WorldToGrid(worldPos);
        if (g.x < 0 || g.x >= width || g.y < 0 || g.y >= height) return false;
        return fogGrid[g.x, g.y] != FogState.Hidden;
    }

    public FogState GetState(int x, int y)
    {
        if (fogGrid == null) return FogState.Hidden;
        if (x < 0 || x >= width || y < 0 || y >= height) return FogState.Hidden;
        return fogGrid[x, y];
    }

    public FogState[,] CopyFogGrid()
    {
        if (fogGrid == null) return null;
        var copy = new FogState[width, height];
        System.Array.Copy(fogGrid, copy, fogGrid.Length);
        return copy;
    }

    public void RestoreFogGrid(FogState[,] grid)
    {
        if (fogGrid == null || grid == null) return;
        if (grid.GetLength(0) != width || grid.GetLength(1) != height)
        {
            Debug.LogWarning("[FogOfWar] RestoreFogGrid: 크기 불일치로 복원을 건너뜁니다.");
            return;
        }
        System.Array.Copy(grid, fogGrid, fogGrid.Length);
        isDirty = true;
        UpdateTexture();
    }

    private void UpdateTexture()
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                colorBuffer[y * width + x] = fogGrid[x, y] switch
                {
                    FogState.Hidden   => fogData?.hiddenColor   ?? new Color(0f, 0f, 0f, 0.95f),
                    FogState.Explored => fogData?.exploredColor ?? new Color(0f, 0f, 0f, 0.5f),
                    FogState.Visible  => fogData?.visibleColor  ?? new Color(0f, 0f, 0f, 0f),
                    _                 => Color.black
                };
            }
        }
        fogTexture.SetPixels(colorBuffer);
        fogTexture.Apply();
        isDirty = false;
    }

    private Vector2Int WorldToGrid(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt((worldPos.x - origin.x) / cellSize),
            Mathf.FloorToInt((worldPos.y - origin.y) / cellSize)
        );
    }
}
