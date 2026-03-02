using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 에디터에서 WorldMap에 나무·바위·덤불을 자동 산포하는 도구.
/// 인스펙터 컨텍스트 메뉴에서 "장식 생성" 또는 "장식 초기화"를 클릭해 실행한다.
///
/// - 나무  : TreePrefabs에서 랜덤 선택해 프리팹 인스턴스로 배치 (장애물 등록)
/// - 바위  : RockSprites에서 랜덤 선택해 SpriteRenderer GO로 배치 (장애물 등록)
/// - 덤불  : BushSprites에서 랜덤 선택해 SpriteRenderer GO로 배치 (시각 전용, 통행 가능)
///
/// 장애물 타일은 GeneratedObstacleTilemap에만 기록되므로
/// 수작업으로 배치한 ObstacleTilemap에는 영향을 주지 않는다.
/// </summary>
public class MapDecorationGenerator : MonoBehaviour
{
    [Header("타일맵 참조")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap obstacleTilemap;
    [Tooltip("자동 생성 장애물 전용 타일맵. 수작업 장애물 타일맵과 분리한다.")]
    [SerializeField] private Tilemap generatedObstacleTilemap;
    [Tooltip("장애물 마커로 사용할 타일 에셋 (Shadow 등 불투명도 0 타일 권장)")]
    [SerializeField] private TileBase obstacleTile;

    [Header("장식 루트")]
    [Tooltip("생성된 GO를 담을 부모 Transform. 초기화 시 자식이 전부 삭제된다.")]
    [SerializeField] private Transform decorationRoot;

    [Header("나무 (장애물)")]
    [SerializeField] private GameObject[] treePrefabs;
    [Range(0f, 0.2f)]
    [SerializeField] private float treeDensity = 0.04f;

    [Header("바위 (장애물)")]
    [SerializeField] private Sprite[] rockSprites;
    [Range(0f, 0.2f)]
    [SerializeField] private float rockDensity = 0.04f;

    [Header("덤불 (시각 전용)")]
    [SerializeField] private Sprite[] bushSprites;
    [Range(0f, 0.3f)]
    [SerializeField] private float bushDensity = 0.06f;

    [Header("생성 옵션")]
    [SerializeField] private int seed = 42;
    [Tooltip("이 반경 내 셀에는 장식을 배치하지 않는다.")]
    [SerializeField] private float clearRadius = 5f;
    [Tooltip("클리어 중심점. 미설정 시 맵 중심을 사용한다.")]
    [SerializeField] private Transform clearCenter;

#if UNITY_EDITOR
    /// <summary>배치 스크립트나 외부 에디터 도구에서 호출 가능한 공개 진입점.</summary>
    public void RunGeneration() => GenerateDecorations();

    [ContextMenu("장식 생성")]
    private void GenerateDecorations()
    {
        if (groundTilemap == null)
        {
            Debug.LogError("[MapDecorationGenerator] groundTilemap이 설정되지 않았습니다.");
            return;
        }

        // 기존 생성 장식 초기화
        ClearDecorations();

        groundTilemap.CompressBounds();
        var bounds = groundTilemap.cellBounds;
        int width  = bounds.size.x;
        int height = bounds.size.y;

        // 클리어 중심
        Vector2 clearPos;
        if (clearCenter != null)
        {
            clearPos = clearCenter.position;
        }
        else
        {
            var midCell = new Vector3Int(bounds.min.x + width / 2, bounds.min.y + height / 2, 0);
            clearPos = (Vector2)groundTilemap.CellToWorld(midCell) + Vector2.one * 0.5f;
        }

        var rng = new System.Random(seed);
        int placedTrees = 0, placedRocks = 0, placedBushes = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var cellPos = new Vector3Int(bounds.min.x + x, bounds.min.y + y, 0);

                if (!groundTilemap.HasTile(cellPos)) continue;
                if (obstacleTilemap          != null && obstacleTilemap.HasTile(cellPos))          continue;
                if (generatedObstacleTilemap != null && generatedObstacleTilemap.HasTile(cellPos)) continue;

                var worldPos = (Vector2)groundTilemap.CellToWorld(cellPos) + Vector2.one * 0.5f;
                if (Vector2.Distance(worldPos, clearPos) < clearRadius) continue;

                double roll = rng.NextDouble();

                if (treePrefabs != null && treePrefabs.Length > 0 && roll < treeDensity)
                {
                    PlaceTree(cellPos, worldPos, rng);
                    placedTrees++;
                }
                else if (rockSprites != null && rockSprites.Length > 0 && roll < treeDensity + rockDensity)
                {
                    PlaceRock(cellPos, worldPos, rng);
                    placedRocks++;
                }
                else if (bushSprites != null && bushSprites.Length > 0 && roll < treeDensity + rockDensity + bushDensity)
                {
                    PlaceBush(worldPos, rng);
                    placedBushes++;
                }
            }
        }

        if (generatedObstacleTilemap != null)
            UnityEditor.EditorUtility.SetDirty(generatedObstacleTilemap);

        Debug.Log($"[MapDecorationGenerator] 생성 완료 — 나무:{placedTrees} 바위:{placedRocks} 덤불:{placedBushes}");
    }

    [ContextMenu("장식 초기화")]
    private void ClearDecorations()
    {
        // 생성된 GO 삭제
        if (decorationRoot != null)
        {
            var children = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in decorationRoot)
                children.Add(child);

            foreach (var child in children)
                UnityEditor.Undo.DestroyObjectImmediate(child.gameObject);
        }

        // 생성된 장애물 타일 삭제
        if (generatedObstacleTilemap != null)
        {
            generatedObstacleTilemap.ClearAllTiles();
            UnityEditor.EditorUtility.SetDirty(generatedObstacleTilemap);
        }
    }

    private void PlaceTree(Vector3Int cellPos, Vector2 worldPos, System.Random rng)
    {
        if (treePrefabs == null || treePrefabs.Length == 0) return;
        var prefab = treePrefabs[rng.Next(treePrefabs.Length)];
        if (prefab == null) return;

        var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, decorationRoot);
        go.transform.position = new Vector3(worldPos.x, worldPos.y, worldPos.y);
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Place Tree");

        MarkObstacle(cellPos);
    }

    private void PlaceRock(Vector3Int cellPos, Vector2 worldPos, System.Random rng)
    {
        if (rockSprites == null || rockSprites.Length == 0) return;
        var sprite = rockSprites[rng.Next(rockSprites.Length)];
        if (sprite == null) return;

        var go = new GameObject($"Rock_Gen_{x_of(cellPos)}_{y_of(cellPos)}");
        go.transform.SetParent(decorationRoot);
        go.transform.position = new Vector3(worldPos.x, worldPos.y, worldPos.y);
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Place Rock");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = Mathf.RoundToInt(-worldPos.y * 100) + 5000;

        MarkObstacle(cellPos);
    }

    private void PlaceBush(Vector2 worldPos, System.Random rng)
    {
        if (bushSprites == null || bushSprites.Length == 0) return;
        var sprite = bushSprites[rng.Next(bushSprites.Length)];
        if (sprite == null) return;

        var go = new GameObject($"Bush_Gen_{Mathf.RoundToInt(worldPos.x)}_{Mathf.RoundToInt(worldPos.y)}");
        go.transform.SetParent(decorationRoot);
        go.transform.position = new Vector3(worldPos.x, worldPos.y, worldPos.y);
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Place Bush");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = Mathf.RoundToInt(-worldPos.y * 100) + 4000;
    }

    private void MarkObstacle(Vector3Int cellPos)
    {
        if (generatedObstacleTilemap == null || obstacleTile == null) return;
        generatedObstacleTilemap.SetTile(cellPos, obstacleTile);
    }

    private static int x_of(Vector3Int v) => v.x;
    private static int y_of(Vector3Int v) => v.y;
#endif
}
