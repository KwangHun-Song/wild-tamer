# 2.3 월드맵

> 상위 문서: [Phase 2 설계](../design.md)

타일맵 기반 맵 생성과 장애물 정보 관리를 담당한다. 생성된 `ObstacleGrid`는 FlockBehavior(장애물 회피)와 MonsterAI(이동 경로)에서 참조한다.

---

## MapGenerator (MonoBehaviour)

타일맵을 생성하고 `ObstacleGrid`를 빌드한다. `GameLoop.Start()`에서 가장 먼저 호출된다.

```csharp
public class MapGenerator : MonoBehaviour
{
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap obstacleTilemap;

    public ObstacleGrid ObstacleGrid { get; private set; }

    public void Generate() { ... }
}
```

---

## ObstacleGrid (pure C#)

월드 좌표 ↔ 그리드 좌표 변환과 보행 가능 여부 조회를 제공한다. `walkable[x, y]`로 O(1) 조회한다.

```csharp
public class ObstacleGrid
{
    private readonly bool[,] walkable;
    private readonly float cellSize;
    private readonly Vector2 origin;

    public ObstacleGrid(int width, int height, float cellSize, Vector2 origin) { ... }

    public bool IsWalkable(Vector2 worldPos) { ... }
    public Vector2Int WorldToGrid(Vector2 worldPos) { ... }
    public Vector2 GridToWorld(Vector2Int gridPos) { ... }
}
```
