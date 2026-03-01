using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap obstacleTilemap;
    [SerializeField] private TileBase waterTile;
    [SerializeField] private float cellSize = 1f;

    public ObstacleGrid ObstacleGrid { get; private set; }

    public void Generate()
    {
        if (groundTilemap == null)
        {
            Debug.LogWarning("[MapGenerator] groundTilemap이 설정되지 않았습니다.");
            ObstacleGrid = new ObstacleGrid(100, 100, cellSize, Vector2.zero);
            return;
        }

        // Ground 타일맵 기준으로 전체 맵 크기 결정
        groundTilemap.CompressBounds();
        var bounds = groundTilemap.cellBounds;

        int width = bounds.size.x;
        int height = bounds.size.y;
        var origin = (Vector2)groundTilemap.CellToWorld(bounds.min);

        ObstacleGrid = new ObstacleGrid(width, height, cellSize, origin);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var cellPos = new Vector3Int(bounds.min.x + x, bounds.min.y + y, 0);

                // Ground 타일이 없으면 빈 공간 → 통행 불가
                bool hasGround = groundTilemap.HasTile(cellPos);
                // Obstacles 타일이 있으면 → 통행 불가
                bool hasObstacle = obstacleTilemap != null && obstacleTilemap.HasTile(cellPos);
                // Water 타일이면 → 통행 불가
                bool isWater = waterTile != null && groundTilemap.GetTile(cellPos) == waterTile;

                ObstacleGrid.SetWalkable(new Vector2Int(x, y), hasGround && !hasObstacle && !isWater);
            }
        }
    }
}
