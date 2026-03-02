using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// WorldMap.prefab에 나무·바위·덤불을 시드 기반으로 자동 산포하는 에디터 도구.
/// Menu → Tools/WorldMap/Scatter Random Obstacles
///
/// 동작:
/// 1. GeneratedObstacles Tilemap(Grid 자식)을 초기화하고 새 장애물 타일을 기록한다.
/// 2. Decorations/Trees, Decorations/Rocks 하위에 Gen_ 접두사 오브젝트를 배치한다.
/// 3. Decorations/Bushes(없으면 생성)에 덤불을 배치한다 (시각 전용, 통행 가능).
/// 4. 수작업으로 배치한 Obstacles 타일맵에는 영향을 주지 않는다.
/// </summary>
public static class MapScatterGenerator
{
    private const string PrefabPath  = "Assets/Prefabs/WorldMap/WorldMap.prefab";
    private const int    Seed        = 42;
    private const float  TreeDensity = 0.05f;
    private const float  RockDensity = 0.05f;
    private const float  BushDensity = 0.06f;
    private const float  ClearRadius = 6f;

    [MenuItem("Tools/WorldMap/Scatter Random Obstacles")]
    public static void ScatterObstacles()
    {
        var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Run(prefabRoot);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            AssetDatabase.Refresh();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MapScatterGenerator] 실패: {e}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void Run(GameObject prefabRoot)
    {
        // ── 타일맵 탐색 ──────────────────────────────────────
        var grid            = prefabRoot.transform.Find("Grid");
        var groundTilemap   = grid?.Find("Ground")?.GetComponent<Tilemap>();
        var obstacleTilemap = grid?.Find("Obstacles")?.GetComponent<Tilemap>();

        if (groundTilemap == null || obstacleTilemap == null || grid == null)
        {
            Debug.LogError("[MapScatterGenerator] Grid/Ground 또는 Grid/Obstacles를 찾을 수 없습니다.");
            return;
        }

        // ── GeneratedObstacles 타일맵 준비 (없으면 생성) ──────
        var genObsTrans = grid.Find("GeneratedObstacles");
        if (genObsTrans == null)
        {
            var go = new GameObject("GeneratedObstacles");
            go.transform.SetParent(grid, false);
            go.AddComponent<Tilemap>();
            var rend = go.AddComponent<TilemapRenderer>();
            rend.enabled = false; // 렌더링 없음 — 장애물 마커 전용
            genObsTrans = go.transform;
        }
        var genObstacleTilemap = genObsTrans.GetComponent<Tilemap>();

        // 이전 자동 생성 타일을 Obstacles 타일맵에서 먼저 제거한다 (수작업 타일은 보존).
        // genObstacleTilemap이 이전 산포에서 마킹한 위치만 알고 있으므로 안전하게 식별 가능.
        foreach (var pos in genObstacleTilemap.cellBounds.allPositionsWithin)
            if (genObstacleTilemap.HasTile(pos))
                obstacleTilemap.SetTile(pos, null);
        genObstacleTilemap.ClearAllTiles(); // 이전 생성분 초기화

        // ── 장애물 마커 타일 결정 ─────────────────────────────
        // Ground 타일맵에서 잔디 타일을 가져와 GeneratedObstacles 마커로 사용한다.
        // HasTile() 유무만 검사하므로 타일 종류는 관계없으나, 에디터에서 확인하기 쉽도록
        // 잔디(Ground) 타일을 사용한다. 없으면 Shadow.asset 폴백.
        TileBase obstacleTile = null;
        foreach (var pos in groundTilemap.cellBounds.allPositionsWithin)
        {
            if (groundTilemap.HasTile(pos)) { obstacleTile = groundTilemap.GetTile(pos); break; }
        }
        if (obstacleTile == null)
            obstacleTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Prefabs/WorldMap/Tiles/Shadow.asset");

        // ── 장식 부모 GO 탐색 ─────────────────────────────────
        var decorations = prefabRoot.transform.Find("Decorations");
        if (decorations == null)
        {
            decorations = new GameObject("Decorations").transform;
            decorations.SetParent(prefabRoot.transform, false);
        }
        var treesParent  = GetOrCreate(decorations, "Trees");
        var rocksParent  = GetOrCreate(decorations, "Rocks");
        var bushesParent = GetOrCreate(decorations, "Bushes");

        // Gen_ 접두사 오브젝트만 삭제 (수작업 배치 오브젝트 보존)
        ClearGenChildren(treesParent);
        ClearGenChildren(rocksParent);
        ClearGenChildren(bushesParent);

        // ── 에셋 로드 ─────────────────────────────────────────
        var treePrefabs = LoadPrefabs(
            "Assets/Prefabs/WorldMap/Trees/Tree1View.prefab",
            "Assets/Prefabs/WorldMap/Trees/Tree2View.prefab",
            "Assets/Prefabs/WorldMap/Trees/Tree3View.prefab",
            "Assets/Prefabs/WorldMap/Trees/Tree4View.prefab"
        );
        var rockSprites = LoadSprites(
            "Assets/Graphic/Sprites/Terrain/Decorations/Rocks/Rock1.png",
            "Assets/Graphic/Sprites/Terrain/Decorations/Rocks/Rock2.png",
            "Assets/Graphic/Sprites/Terrain/Decorations/Rocks/Rock3.png",
            "Assets/Graphic/Sprites/Terrain/Decorations/Rocks/Rock4.png"
        );
        var bushPrefabs = LoadPrefabs(
            "Assets/Prefabs/WorldMap/Bushes/Bush1View.prefab",
            "Assets/Prefabs/WorldMap/Bushes/Bush2View.prefab",
            "Assets/Prefabs/WorldMap/Bushes/Bush3View.prefab",
            "Assets/Prefabs/WorldMap/Bushes/Bush4View.prefab"
        );

        // ── 클리어 반경 중심 ──────────────────────────────────
        var spawnGO  = prefabRoot.transform.Find("SpawnPoints")?.Find("PlayerSpawn");
        Vector2 clearCenter;
        groundTilemap.CompressBounds();
        var bounds = groundTilemap.cellBounds;
        if (spawnGO != null)
        {
            clearCenter = spawnGO.position;
        }
        else
        {
            var mid = new Vector3Int(bounds.min.x + bounds.size.x / 2, bounds.min.y + bounds.size.y / 2, 0);
            clearCenter = (Vector2)groundTilemap.CellToWorld(mid) + Vector2.one * 0.5f;
        }

        // ── 산포 ──────────────────────────────────────────────
        var rng = new System.Random(Seed);
        int trees = 0, rocks = 0, bushes = 0;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                var cellPos = new Vector3Int(x, y, 0);
                if (!groundTilemap.HasTile(cellPos))              continue;
                if (obstacleTilemap.HasTile(cellPos))             continue; // 기존 장애물 셀 스킵
                if (genObstacleTilemap.HasTile(cellPos))          continue; // 이미 생성된 셀 스킵

                var worldPos = (Vector2)groundTilemap.CellToWorld(cellPos) + Vector2.one * 0.5f;
                if (Vector2.Distance(worldPos, clearCenter) < ClearRadius) continue;

                double roll = rng.NextDouble();

                if (treePrefabs.Length > 0 && roll < TreeDensity)
                {
                    PlaceTree(cellPos, worldPos, treePrefabs, treesParent, genObstacleTilemap, obstacleTile, rng);
                    trees++;
                }
                else if (rockSprites.Length > 0 && roll < TreeDensity + RockDensity)
                {
                    PlaceRock(cellPos, worldPos, rockSprites, rocksParent, genObstacleTilemap, obstacleTile, rng);
                    rocks++;
                }
                else if (bushPrefabs.Length > 0 && roll < TreeDensity + RockDensity + BushDensity)
                {
                    PlaceBush(cellPos, worldPos, bushPrefabs, bushesParent, rng);
                    bushes++;
                }
            }
        }

        Debug.Log($"[MapScatterGenerator] 완료 — 나무:{trees}  바위:{rocks}  덤불:{bushes}");

        // 자동 생성 장애물 타일을 Obstacles 타일맵에도 반영한다.
        // 이로써 에디터에서 Obstacles 레이어를 선택하면 나무·바위 위치를 바로 확인할 수 있다.
        genObstacleTilemap.CompressBounds();
        foreach (var pos in genObstacleTilemap.cellBounds.allPositionsWithin)
            if (genObstacleTilemap.HasTile(pos))
                obstacleTilemap.SetTile(pos, genObstacleTilemap.GetTile(pos));
    }

    // ── 배치 헬퍼 ────────────────────────────────────────────

    private static void PlaceTree(Vector3Int cell, Vector2 worldPos,
        GameObject[] prefabs, Transform parent,
        Tilemap genTilemap, TileBase tile, System.Random rng)
    {
        var prefab = prefabs[rng.Next(prefabs.Length)];
        if (prefab == null) return;

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = $"Gen_Tree_{cell.x}_{cell.y}";
        go.transform.position = new Vector3(worldPos.x, worldPos.y, worldPos.y);

        if (tile != null) genTilemap.SetTile(cell, tile);
    }

    private static void PlaceRock(Vector3Int cell, Vector2 worldPos,
        Sprite[] sprites, Transform parent,
        Tilemap genTilemap, TileBase tile, System.Random rng)
    {
        var sprite = sprites[rng.Next(sprites.Length)];
        if (sprite == null) return;

        var go = new GameObject($"Gen_Rock_{cell.x}_{cell.y}");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(worldPos.x, worldPos.y, worldPos.y);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 2000 - Mathf.RoundToInt(worldPos.y * 10);

        if (tile != null) genTilemap.SetTile(cell, tile);
    }

    private static void PlaceBush(Vector3Int cell, Vector2 worldPos, GameObject[] prefabs, Transform parent, System.Random rng)
    {
        var prefab = prefabs[rng.Next(prefabs.Length)];
        if (prefab == null) return;

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = $"Gen_Bush_{cell.x}_{cell.y}";
        go.transform.position = new Vector3(worldPos.x, worldPos.y, worldPos.y);
    }

    // ── 유틸리티 ─────────────────────────────────────────────

    private static Transform GetOrCreate(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) return t;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void ClearGenChildren(Transform parent)
    {
        var toDelete = new List<GameObject>();
        foreach (Transform child in parent)
            if (child.name.StartsWith("Gen_")) toDelete.Add(child.gameObject);
        foreach (var go in toDelete) Object.DestroyImmediate(go);
    }

    private static GameObject[] LoadPrefabs(params string[] paths)
    {
        var list = new List<GameObject>();
        foreach (var p in paths)
        {
            var a = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (a != null) list.Add(a);
            else Debug.LogWarning($"[MapScatterGenerator] 프리팹 없음: {p}");
        }
        return list.ToArray();
    }

    private static Sprite[] LoadSprites(params string[] paths)
    {
        var list = new List<Sprite>();
        foreach (var p in paths)
        {
            var a = AssetDatabase.LoadAssetAtPath<Sprite>(p);
            if (a != null) list.Add(a);
            else Debug.LogWarning($"[MapScatterGenerator] 스프라이트 없음: {p}");
        }
        return list.ToArray();
    }
}
