using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap waterTilemap;
    [SerializeField] private Tilemap obstacleTilemap;
    [Tooltip("MapDecorationGenerator가 자동 생성한 장애물 타일맵. null이면 무시한다.")]
    [SerializeField] private Tilemap generatedObstacleTilemap;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private SpriteRenderer waterBackground;

    public ObstacleGrid ObstacleGrid { get; private set; }

    public void Generate()
    {
        if (groundTilemap == null)
        {
            Debug.LogWarning("[MapGenerator] groundTilemap이 설정되지 않았습니다.");
            ObstacleGrid = new ObstacleGrid(100, 100, cellSize, Vector2.zero);
            return;
        }

        // Ground + Water 타일맵 합산으로 전체 맵 경계 결정
        groundTilemap.CompressBounds();
        var bounds = groundTilemap.cellBounds;

        if (waterTilemap != null && waterTilemap.cellBounds.size != Vector3Int.zero)
        {
            waterTilemap.CompressBounds();
            var waterBounds = waterTilemap.cellBounds;
            bounds.SetMinMax(
                Vector3Int.Min(bounds.min, waterBounds.min),
                Vector3Int.Max(bounds.max, waterBounds.max)
            );
        }

        int width = bounds.size.x;
        int height = bounds.size.y;
        var origin = (Vector2)groundTilemap.CellToWorld(bounds.min);

        // generatedObstacleTilemap이 미설정이면 Grid 아래 "GeneratedObstacles"를 자동 탐색
        if (generatedObstacleTilemap == null)
        {
            var gridParent = groundTilemap.transform.parent;
            var genGO = gridParent != null ? gridParent.Find("GeneratedObstacles") : null;
            if (genGO != null) generatedObstacleTilemap = genGO.GetComponent<Tilemap>();
        }

        ObstacleGrid = new ObstacleGrid(width, height, cellSize, origin);

        FitWaterBackground(width, height, origin);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var cellPos = new Vector3Int(bounds.min.x + x, bounds.min.y + y, 0);

                // Ground 타일이 있고 Obstacle 타일이 없으면 통행 가능
                // Water 타일맵은 현재 비주얼 전용 — 통행 불가 판단은 Ground 타일 유무로만 결정
                bool hasGround = groundTilemap.HasTile(cellPos);
                bool hasObstacle = (obstacleTilemap          != null && obstacleTilemap.HasTile(cellPos))
                                || (generatedObstacleTilemap != null && generatedObstacleTilemap.HasTile(cellPos));

                ObstacleGrid.SetWalkable(new Vector2Int(x, y), hasGround && !hasObstacle);
            }
        }
    }

    private void FitWaterBackground(int width, int height, Vector2 origin)
    {
        if (waterBackground == null || waterBackground.sprite == null)
            return;

        float mapWidth  = width  * cellSize;
        float mapHeight = height * cellSize;
        var   center    = origin + new Vector2(mapWidth * 0.5f, mapHeight * 0.5f);

        waterBackground.transform.position = new Vector3(center.x, center.y, 0f);

        var spriteSize = waterBackground.sprite.bounds.size;
        waterBackground.transform.localScale = new Vector3(
            mapWidth  / spriteSize.x,
            mapHeight / spriteSize.y,
            1f
        );
    }
}
