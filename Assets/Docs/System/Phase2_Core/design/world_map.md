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
    [SerializeField] private Tilemap waterTilemap;
    [SerializeField] private Tilemap obstacleTilemap;
    /// <summary>
    /// MapScatterGenerator / MapDecorationGenerator가 자동 생성한 장애물 전용 타일맵.
    /// null이면 Grid 하위에서 "GeneratedObstacles" 오브젝트를 자동 탐색한다.
    /// </summary>
    [SerializeField] private Tilemap generatedObstacleTilemap;

    public ObstacleGrid ObstacleGrid { get; private set; }

    public void Generate() { ... }
}
```

- **Ground + Water 합산 경계**: `waterTilemap`이 있으면 두 cellBounds를 합산해 맵 전체 크기를 결정한다.
- **GeneratedObstacles 자동 탐색**: `generatedObstacleTilemap`이 미설정이면 `Grid` 하위에서 `"GeneratedObstacles"` 오브젝트를 자동 탐색한다. 에디터 툴이 생성한 장애물을 런타임에서도 인식하기 위한 폴백이다.
- 보행 가능 판정: `hasGround && !(obstacleTilemap || generatedObstacleTilemap)`

---

## MapDecorationGenerator (MonoBehaviour)

씬 내 `WorldMap` 오브젝트에 붙이는 에디터 전용 도구. Inspector 컨텍스트 메뉴(우클릭)에서 "장식 생성" / "장식 초기화"를 실행한다.

- **나무**: `TreePrefabs` 풀에서 랜덤 선택, 프리팹 인스턴스 배치 + `GeneratedObstacleTilemap`에 마킹.
- **바위**: `RockSprites` 풀에서 랜덤 선택, SpriteRenderer GO 배치 + `GeneratedObstacleTilemap`에 마킹.
- **덤불**: `BushSprites` 풀에서 랜덤 선택, SpriteRenderer GO 배치. 통행 가능(장애물 미등록).
- **클리어 반경**: `clearCenter` Transform 기준 `clearRadius` 이내 셀 스킵(플레이어 스폰 주변 보호).
- `decorationRoot` 하위에 생성된 GO를 모아 관리하며, 초기화 시 전체 삭제.

---

## MapScatterGenerator (Editor static class)

메뉴 `Tools/WorldMap/Scatter Random Obstacles`에서 실행하는 에디터 도구. `WorldMap.prefab`을 직접 수정한다.

- 시드 기반 결정론적 배치 (Seed = 42).
- `Grid/GeneratedObstacles` 타일맵에 마킹 후, `Grid/Obstacles` 타일맵에도 동기화하여 에디터에서 즉시 확인 가능.
- 재실행 시 이전 자동 생성 타일만 Obstacles에서 제거 후 재배치(수작업 타일 보존).
- 나무(`Bush#View.prefab`)·덤불(`Bush#View.prefab`)은 Prefab 인스턴스로 배치, 바위는 SpriteRenderer GO.
- 덤불은 장애물 미등록(통행 가능).

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
