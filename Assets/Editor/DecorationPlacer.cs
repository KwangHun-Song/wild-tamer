using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// Obstacles 타일맵의 타일 위치를 읽어 건물/바위/나무 오브젝트를 순환 배치.
/// Menu: Tools/WorldMap/Place Decorations from Obstacles
/// </summary>
public static class DecorationPlacer
{
    [MenuItem("Tools/WorldMap/Place Decorations from Obstacles")]
    public static void PlaceDecorations()
    {
        var obstaclesGO = GameObject.Find("WorldMap/Grid/Obstacles");
        if (obstaclesGO == null) { Debug.LogError("[DecorationPlacer] 'WorldMap/Grid/Obstacles' not found."); return; }

        var tilemap = obstaclesGO.GetComponent<Tilemap>();
        if (tilemap == null) { Debug.LogError("[DecorationPlacer] No Tilemap component on Obstacles."); return; }

        var treesParent     = GameObject.Find("WorldMap/Decorations/Trees")?.transform;
        var rocksParent     = GameObject.Find("WorldMap/Decorations/Rocks")?.transform;
        var buildingsParent = GameObject.Find("WorldMap/Buildings")?.transform;

        if (treesParent == null || rocksParent == null || buildingsParent == null)
        {
            Debug.LogError("[DecorationPlacer] Parent transforms (Trees/Rocks/Buildings) not found.");
            return;
        }

        // 스프라이트 로드
        var buildingSprites = new[]
        {
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Graphic/Sprites/Buildings/Blue Buildings/House1.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Graphic/Sprites/Buildings/Blue Buildings/House2.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Graphic/Sprites/Buildings/Blue Buildings/House3.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Graphic/Sprites/Buildings/Blue Buildings/Castle.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Graphic/Sprites/Buildings/Blue Buildings/Tower.png"),
        };
        var rockSprites = new[]
        {
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Graphic/Sprites/Terrain/Decorations/Rocks/Rock1.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Graphic/Sprites/Terrain/Decorations/Rocks/Rock2.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Graphic/Sprites/Terrain/Decorations/Rocks/Rock3.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Graphic/Sprites/Terrain/Decorations/Rocks/Rock4.png"),
        };
        var treeSprites = new[]
        {
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Graphic/Sprites/Terrain/Resources/Wood/Trees/Tree1.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Graphic/Sprites/Terrain/Resources/Wood/Trees/Tree2.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Graphic/Sprites/Terrain/Resources/Wood/Trees/Tree3.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Graphic/Sprites/Terrain/Resources/Wood/Trees/Tree4.png"),
        };

        // 타일 위치 수집
        tilemap.CompressBounds();
        var bounds = tilemap.cellBounds;
        var positions = new List<Vector3Int>();

        for (int x = bounds.xMin; x < bounds.xMax; x++)
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                var pos = new Vector3Int(x, y, 0);
                if (tilemap.HasTile(pos))
                    positions.Add(pos);
            }

        if (positions.Count == 0)
        {
            Debug.LogWarning("[DecorationPlacer] Obstacles 타일맵에 타일이 없습니다.");
            return;
        }

        // 기존 오브젝트 정리
        ClearChildren(treesParent);
        ClearChildren(rocksParent);
        ClearChildren(buildingsParent);

        // 건물 → 바위 → 나무 순환 배치
        int bIdx = 0, rIdx = 0, tIdx = 0;

        for (int i = 0; i < positions.Count; i++)
        {
            var worldPos = tilemap.GetCellCenterWorld(positions[i]);
            int type = i % 3;

            string goName;
            Sprite sprite;
            Transform parent;

            switch (type)
            {
                case 0:
                    sprite = buildingSprites[bIdx % buildingSprites.Length];
                    goName = $"Building_{bIdx + 1}";
                    parent = buildingsParent;
                    bIdx++;
                    break;
                case 1:
                    sprite = rockSprites[rIdx % rockSprites.Length];
                    goName = $"Rock_{rIdx + 1}";
                    parent = rocksParent;
                    rIdx++;
                    break;
                default:
                    sprite = treeSprites[tIdx % treeSprites.Length];
                    goName = $"Tree_{tIdx + 1}";
                    parent = treesParent;
                    tIdx++;
                    break;
            }

            var go = new GameObject(goName);
            go.transform.SetParent(parent);
            go.transform.position = worldPos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            // Y가 클수록(화면 위쪽) 뒤에 그려짐 → 양수로 유지해 타일맵 위에 렌더링
            sr.sortingOrder = 1000 - Mathf.RoundToInt(worldPos.y * 10);

            Undo.RegisterCreatedObjectUndo(go, $"Place {goName}");
            Debug.Log($"[DecorationPlacer] {goName} → {worldPos}");
        }

        Debug.Log($"[DecorationPlacer] 완료 — {positions.Count}개 오브젝트 배치됨.");
    }

    private static void ClearChildren(Transform parent)
    {
        var children = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in parent)
            children.Add(child.gameObject);
        foreach (var child in children)
        {
            Undo.DestroyObjectImmediate(child);
        }
    }
}
