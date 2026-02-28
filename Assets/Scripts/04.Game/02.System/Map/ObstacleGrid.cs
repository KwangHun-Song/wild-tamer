using UnityEngine;

public class ObstacleGrid
{
    private readonly bool[,] walkable;
    private readonly float cellSize;
    private readonly Vector2 origin;
    private readonly int width;
    private readonly int height;

    public ObstacleGrid(int width, int height, float cellSize, Vector2 origin)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.origin = origin;
        walkable = new bool[width, height];

        // 기본값: 전부 보행 가능
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                walkable[x, y] = true;
    }

    public bool IsWalkable(Vector2 worldPos)
    {
        var grid = WorldToGrid(worldPos);
        if (grid.x < 0 || grid.x >= width || grid.y < 0 || grid.y >= height)
            return false;
        return walkable[grid.x, grid.y];
    }

    public void SetWalkable(Vector2Int gridPos, bool value)
    {
        if (gridPos.x < 0 || gridPos.x >= width || gridPos.y < 0 || gridPos.y >= height)
            return;
        walkable[gridPos.x, gridPos.y] = value;
    }

    public Vector2Int WorldToGrid(Vector2 worldPos)
    {
        var local = worldPos - origin;
        return new Vector2Int(
            Mathf.FloorToInt(local.x / cellSize),
            Mathf.FloorToInt(local.y / cellSize)
        );
    }

    public Vector2 GridToWorld(Vector2Int gridPos)
    {
        return origin + new Vector2(
            gridPos.x * cellSize + cellSize * 0.5f,
            gridPos.y * cellSize + cellSize * 0.5f
        );
    }
}
