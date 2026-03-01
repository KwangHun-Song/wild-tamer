using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 타일셋 스프라이트에서 Tile 에셋을 생성하는 에디터 유틸리티.
/// Menu: Tools > WorldMap > Create Terrain Tiles
/// </summary>
public static class TileAssetCreator
{
    private const string TileOutputPath = "Assets/Prefabs/WorldMap/Tiles";

    [MenuItem("Tools/WorldMap/Create Terrain Tiles")]
    public static void CreateTerrainTiles()
    {
        var entries = new (string spritePath, string tileName)[]
        {
            ("Assets/Graphic/Sprites/Terrain/Tileset/Tilemap_color1.png", "Ground_1"),
            ("Assets/Graphic/Sprites/Terrain/Tileset/Tilemap_color2.png", "Ground_2"),
            ("Assets/Graphic/Sprites/Terrain/Tileset/Tilemap_color3.png", "Ground_3"),
            ("Assets/Graphic/Sprites/Terrain/Tileset/Tilemap_color4.png", "Ground_4"),
            ("Assets/Graphic/Sprites/Terrain/Tileset/Tilemap_color5.png", "Ground_5"),
            ("Assets/Graphic/Sprites/Terrain/Tileset/Water Background color.png", "Water"),
            ("Assets/Graphic/Sprites/Terrain/Tileset/Water Foam.png", "WaterFoam"),
            ("Assets/Graphic/Sprites/Terrain/Tileset/Shadow.png", "Shadow"),
        };

        int created = 0;
        foreach (var (spritePath, tileName) in entries)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                Debug.LogWarning($"[TileAssetCreator] Sprite not found: {spritePath}");
                continue;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;

            var assetPath = $"{TileOutputPath}/{tileName}.asset";
            AssetDatabase.CreateAsset(tile, assetPath);
            created++;
            Debug.Log($"[TileAssetCreator] Created: {assetPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TileAssetCreator] Done — {created} tiles created in {TileOutputPath}");
    }
}
