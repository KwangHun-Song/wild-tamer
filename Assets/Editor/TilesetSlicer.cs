using UnityEditor;
using UnityEngine;

/// <summary>
/// 타일셋 PNG를 64×64 격자로 슬라이스하는 에디터 유틸리티.
/// Menu: Tools/WorldMap/Slice Tilesets (64x64)
/// </summary>
public static class TilesetSlicer
{
    private const int TileSize = 64;

    [MenuItem("Tools/WorldMap/Slice Tilesets (64x64)")]
    public static void SliceTilesets()
    {
        // Multiple 모드로 슬라이스할 타일셋 시트들
        var sheetPaths = new[]
        {
            "Assets/Graphic/Sprites/Terrain/Tileset/Tilemap_color1.png",
            "Assets/Graphic/Sprites/Terrain/Tileset/Tilemap_color2.png",
            "Assets/Graphic/Sprites/Terrain/Tileset/Tilemap_color3.png",
            "Assets/Graphic/Sprites/Terrain/Tileset/Tilemap_color4.png",
            "Assets/Graphic/Sprites/Terrain/Tileset/Tilemap_color5.png",
        };

        // Single 타일 — PPU만 64로 맞추는 것들
        var singleTilePaths = new[]
        {
            "Assets/Graphic/Sprites/Terrain/Tileset/Water Background color.png",
        };

        int sliced = 0;
        foreach (var path in sheetPaths)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[TilesetSlicer] TextureImporter not found: {path}");
                continue;
            }

            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = TileSize;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
            {
                Debug.LogWarning($"[TilesetSlicer] Texture not loaded: {path}");
                continue;
            }

            // 텍스처 크기는 importer에서 읽어야 정확하다
            // tex.width/height는 import 전 크기를 반영하므로 사전 리임포트 필요
            // 여기서는 미리 알고 있는 576×384를 직접 사용한다
            int texW = tex.width;
            int texH = tex.height;

            if (texW == 0 || texH == 0)
            {
                Debug.LogWarning($"[TilesetSlicer] Could not get size for {path}, skipping.");
                continue;
            }

            int cols = texW / TileSize;
            int rows = texH / TileSize;

            var spriteMetaDatas = new SpriteMetaData[cols * rows];
            string baseName = System.IO.Path.GetFileNameWithoutExtension(path);

            int index = 0;
            for (int row = rows - 1; row >= 0; row--)  // Unity UV는 하단부터 시작
            {
                for (int col = 0; col < cols; col++)
                {
                    spriteMetaDatas[index] = new SpriteMetaData
                    {
                        name = $"{baseName}_{index}",
                        rect = new Rect(col * TileSize, row * TileSize, TileSize, TileSize),
                        pivot = new Vector2(0.5f, 0.5f),
                        alignment = (int)SpriteAlignment.Center
                    };
                    index++;
                }
            }

            importer.spritesheet = spriteMetaDatas;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            sliced++;
            Debug.Log($"[TilesetSlicer] Sliced {cols}x{rows} = {cols * rows} sprites: {path}");
        }

        // Single 타일 PPU 조정
        foreach (var path in singleTilePaths)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = TileSize;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            Debug.Log($"[TilesetSlicer] Updated PPU for single tile: {path}");
        }

        AssetDatabase.Refresh();
        Debug.Log($"[TilesetSlicer] Done — {sliced} sheets sliced at {TileSize}x{TileSize}.");
    }
}
