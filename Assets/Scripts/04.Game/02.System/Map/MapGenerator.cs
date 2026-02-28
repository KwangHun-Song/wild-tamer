using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap obstacleTilemap;
    [SerializeField] private float cellSize = 1f;

    public ObstacleGrid ObstacleGrid { get; private set; }

    public void Generate()
    {
        if (obstacleTilemap == null)
        {
            Debug.LogWarning("[MapGenerator] obstacleTilemap이 설정되지 않았습니다.");
            ObstacleGrid = new ObstacleGrid(100, 100, cellSize, Vector2.zero);
            return;
        }

        obstacleTilemap.CompressBounds();
        var bounds = obstacleTilemap.cellBounds;

        int width = bounds.size.x;
        int height = bounds.size.y;
        var origin = (Vector2)obstacleTilemap.CellToWorld(bounds.min);

        ObstacleGrid = new ObstacleGrid(width, height, cellSize, origin);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var cellPos = new Vector3Int(bounds.min.x + x, bounds.min.y + y, 0);
                bool hasObstacle = obstacleTilemap.HasTile(cellPos);
                ObstacleGrid.SetWalkable(new Vector2Int(x, y), !hasObstacle);
            }
        }
    }
}
