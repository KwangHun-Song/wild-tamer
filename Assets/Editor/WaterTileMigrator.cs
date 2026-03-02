using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

/// <summary>
/// Water 타일맵 관련 에디터 유틸리티.
/// Tools > Clear Water Tilemap: 잘못 마이그레이션된 타일 전체 제거.
/// </summary>
public static class WaterTileMigrator
{
    private const string PrefabPath = "Assets/Prefabs/WorldMap/WorldMap.prefab";

    [MenuItem("Tools/Clear Water Tilemap")]
    public static void ClearWaterTilemap()
    {
        using var scope = new PrefabUtility.EditPrefabContentsScope(PrefabPath);
        var root = scope.prefabContentsRoot;

        var mapGenerator = root.GetComponent<MapGenerator>();
        if (mapGenerator == null)
        {
            Debug.LogError("[WaterTileMigrator] WorldMap 프리팹에서 MapGenerator를 찾을 수 없습니다.");
            return;
        }

        var so = new SerializedObject(mapGenerator);
        var waterTilemap = so.FindProperty("waterTilemap").objectReferenceValue as Tilemap;

        if (waterTilemap == null)
        {
            Debug.LogError("[WaterTileMigrator] waterTilemap이 연결되지 않았습니다.");
            return;
        }

        waterTilemap.ClearAllTiles();

        Debug.Log("[WaterTileMigrator] Water 타일맵을 초기화했습니다. 이제 Tile Palette에서 원하는 영역에 직접 칠해주세요.");
    }
}
