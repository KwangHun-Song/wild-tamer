using UnityEngine;

public enum FogState
{
    Hidden,     // 미탐색 — 완전히 어두움
    Explored,   // 탐색 완료 — 반투명 처리
    Visible     // 현재 시야 내 — 완전히 표시
}

public class FogOfWar : MonoBehaviour
{
    [SerializeField] private int gridWidth = 100;
    [SerializeField] private int gridHeight = 100;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private int viewRadius = 5;
    [SerializeField] private SpriteRenderer fogRenderer;

    private FogState[,] fogGrid;
    private Texture2D fogTexture;
    private bool isDirty;

    // 월드 원점 (그리드 좌하단 기준)
    [SerializeField] private Vector2 origin = Vector2.zero;

    private void Awake()
    {
        InitializeGrid();
        InitializeTexture();
    }

    private void InitializeGrid()
    {
        fogGrid = new FogState[gridWidth, gridHeight];
        for (var x = 0; x < gridWidth; x++)
            for (var y = 0; y < gridHeight; y++)
                fogGrid[x, y] = FogState.Hidden;
    }

    private void InitializeTexture()
    {
        fogTexture = new Texture2D(gridWidth, gridHeight, TextureFormat.RGBA32, false);
        fogTexture.filterMode = FilterMode.Bilinear;
        UpdateTexture();
        if (fogRenderer != null)
            fogRenderer.sprite = Sprite.Create(fogTexture, new Rect(0, 0, gridWidth, gridHeight), Vector2.zero, 1f);
    }

    /// <summary>플레이어 위치 주변 viewRadius 셀을 Visible로 설정한다.</summary>
    public void RevealAround(Vector2 worldPos)
    {
        var center = WorldToGrid(worldPos);

        // 이전 Visible → Explored로 전환
        for (var x = 0; x < gridWidth; x++)
            for (var y = 0; y < gridHeight; y++)
                if (fogGrid[x, y] == FogState.Visible)
                {
                    fogGrid[x, y] = FogState.Explored;
                    isDirty = true;
                }

        // viewRadius 내 셀 → Visible
        for (var dx = -viewRadius; dx <= viewRadius; dx++)
        {
            for (var dy = -viewRadius; dy <= viewRadius; dy++)
            {
                var gx = center.x + dx;
                var gy = center.y + dy;
                if (gx < 0 || gx >= gridWidth || gy < 0 || gy >= gridHeight) continue;
                if (dx * dx + dy * dy > viewRadius * viewRadius) continue;
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
        var g = WorldToGrid(worldPos);
        if (g.x < 0 || g.x >= gridWidth || g.y < 0 || g.y >= gridHeight) return false;
        return fogGrid[g.x, g.y] != FogState.Hidden;
    }

    public FogState GetState(int x, int y)
    {
        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return FogState.Hidden;
        return fogGrid[x, y];
    }

    public FogState[,] CopyFogGrid()
    {
        var copy = new FogState[gridWidth, gridHeight];
        System.Array.Copy(fogGrid, copy, fogGrid.Length);
        return copy;
    }

    public void RestoreFogGrid(FogState[,] grid)
    {
        if (grid == null) return;
        System.Array.Copy(grid, fogGrid, fogGrid.Length);
        isDirty = true;
        UpdateTexture();
    }

    private void UpdateTexture()
    {
        for (var x = 0; x < gridWidth; x++)
        {
            for (var y = 0; y < gridHeight; y++)
            {
                var color = fogGrid[x, y] switch
                {
                    FogState.Hidden   => new Color(0f, 0f, 0f, 1f),
                    FogState.Explored => new Color(0f, 0f, 0f, 0.5f),
                    FogState.Visible  => new Color(0f, 0f, 0f, 0f),
                    _                 => Color.black
                };
                fogTexture.SetPixel(x, y, color);
            }
        }
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
