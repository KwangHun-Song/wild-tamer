using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PlayPage 프리팹에 FogOfWar GO와 MinimapPanel UI 계층을 자동으로 추가하고
/// 직렬화 레퍼런스를 연결하는 에디터 유틸리티.
///
/// Tools/Wire FogOfWar &amp; Minimap 메뉴로 실행한다.
/// </summary>
public static class PlayPageFogMinimapWirer
{
    private const string PlayPagePath = "Assets/Resources/PlayPage.prefab";

    [MenuItem("Tools/Wire FogOfWar & Minimap")]
    public static void WireAll()
    {
        using var scope = new PrefabUtility.EditPrefabContentsScope(PlayPagePath);
        var root = scope.prefabContentsRoot;

        var playPageComp = root.GetComponent<PlayPage>();
        if (playPageComp == null)
        {
            Debug.LogError("[PlayPageFogMinimapWirer] PlayPage 컴포넌트를 찾을 수 없습니다.");
            return;
        }

        // 참조 대상 찾기
        var worldMapRoot = root.transform.Find("WorldMapRoot");
        var canvas       = root.transform.Find("Canvas");

        if (worldMapRoot == null || canvas == null)
        {
            Debug.LogError("[PlayPageFogMinimapWirer] WorldMapRoot 또는 Canvas를 찾을 수 없습니다.");
            return;
        }

        var fogOfWarComp = SetupFogOfWar(worldMapRoot);
        var minimapComp  = SetupMinimap(canvas);

        // PlayPage 레퍼런스 연결
        var so = new SerializedObject(playPageComp);
        so.FindProperty("fogOfWar").objectReferenceValue = fogOfWarComp;
        so.FindProperty("minimap").objectReferenceValue  = minimapComp;
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.SaveAssets();
        Debug.Log("[PlayPageFogMinimapWirer] FogOfWar + Minimap 연결 완료.");
    }

    // ── FogOfWar ────────────────────────────────────────────────────────────

    private static FogOfWar SetupFogOfWar(Transform worldMapRoot)
    {
        // FogOfWar GO 재생성 (멱등)
        var existing = worldMapRoot.Find("FogOfWar");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        var fogGO = new GameObject("FogOfWar");
        fogGO.transform.SetParent(worldMapRoot, worldPositionStays: false);

        var fogOfWar = fogGO.AddComponent<FogOfWar>();

        // FogRenderer 자식 — SpriteRenderer
        var rendererGO = new GameObject("FogRenderer");
        rendererGO.transform.SetParent(fogGO.transform, worldPositionStays: false);
        var sr = rendererGO.AddComponent<SpriteRenderer>();
        sr.color        = Color.white;
        sr.sortingOrder = SortingOrder.Fog;

        // FogOfWar.fogRenderer 연결
        var fogSo = new SerializedObject(fogOfWar);
        fogSo.FindProperty("fogRenderer").objectReferenceValue = sr;
        fogSo.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[PlayPageFogMinimapWirer] FogOfWar GO 생성 완료.");
        return fogOfWar;
    }

    // ── MinimapPanel ─────────────────────────────────────────────────────────

    private static Minimap SetupMinimap(Transform canvas)
    {
        // MinimapPanel 재생성 (멱등)
        var existing = canvas.Find("MinimapPanel");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        // MinimapPanel — 우상단 앵커
        var panelGO = new GameObject("MinimapPanel");
        panelGO.transform.SetParent(canvas, worldPositionStays: false);
        var panelRT = panelGO.AddComponent<RectTransform>();
        SetAnchor(panelRT, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        panelRT.sizeDelta        = new Vector2(160f, 160f);
        panelRT.anchoredPosition = new Vector2(-10f, -10f);

        // MinimapFrame — 배경 Image
        var frameGO = new GameObject("MinimapFrame");
        frameGO.transform.SetParent(panelGO.transform, worldPositionStays: false);
        var frameRT = frameGO.AddComponent<RectTransform>();
        StretchFull(frameRT);
        var frameImg = frameGO.AddComponent<Image>();
        frameImg.color = new Color(0f, 0f, 0f, 0.7f);

        // MapImage — RawImage (지형·안개 Texture2D 표시)
        var mapImgGO = new GameObject("MapImage");
        mapImgGO.transform.SetParent(panelGO.transform, worldPositionStays: false);
        var mapImgRT = mapImgGO.AddComponent<RectTransform>();
        StretchFull(mapImgRT);
        SetPivot(mapImgRT, Vector2.zero);
        var rawImg = mapImgGO.AddComponent<RawImage>();
        rawImg.color = Color.white;

        // IconContainer — 아이콘 배치용 (MapImage와 동일 Rect, pivot 0,0)
        var iconContGO = new GameObject("IconContainer");
        iconContGO.transform.SetParent(panelGO.transform, worldPositionStays: false);
        var iconContRT = iconContGO.AddComponent<RectTransform>();
        StretchFull(iconContRT);
        SetPivot(iconContRT, Vector2.zero);

        // Minimap 컴포넌트 — MinimapPanel에 부착
        var minimapComp = panelGO.AddComponent<Minimap>();

        // 아이콘 프리팹 생성 (인라인 Image GO 사용)
        var playerIconPrefab = CreateIconPrefab("PlayerIcon",  new Color(0.2f, 0.6f, 1f,  1f), 8f);
        var allyIconPrefab   = CreateIconPrefab("AllyIcon",    new Color(0.3f, 1f,   0.3f, 1f), 6f);
        var enemyIconPrefab  = CreateIconPrefab("EnemyIcon",   new Color(1f,   0.3f, 0.3f, 1f), 6f);

        // 아이콘 프리팹을 IconContainer 자식으로 배치 (숨김)
        playerIconPrefab.transform.SetParent(iconContGO.transform, worldPositionStays: false);
        allyIconPrefab.transform.SetParent(iconContGO.transform,   worldPositionStays: false);
        enemyIconPrefab.transform.SetParent(iconContGO.transform,  worldPositionStays: false);
        playerIconPrefab.SetActive(false);
        allyIconPrefab.SetActive(false);
        enemyIconPrefab.SetActive(false);

        // Minimap 레퍼런스 연결
        var minimapSo = new SerializedObject(minimapComp);
        minimapSo.FindProperty("mapImage").objectReferenceValue      = rawImg;
        minimapSo.FindProperty("iconContainer").objectReferenceValue = iconContRT;
        minimapSo.FindProperty("playerIconPrefab").objectReferenceValue =
            playerIconPrefab.GetComponent<Image>();
        minimapSo.FindProperty("allyIconPrefab").objectReferenceValue =
            allyIconPrefab.GetComponent<Image>();
        minimapSo.FindProperty("enemyIconPrefab").objectReferenceValue =
            enemyIconPrefab.GetComponent<Image>();
        minimapSo.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[PlayPageFogMinimapWirer] MinimapPanel 생성 완료.");
        return minimapComp;
    }

    // ── 유틸 헬퍼 ────────────────────────────────────────────────────────────

    private static void SetAnchor(
        RectTransform rt,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot     = pivot;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
    }

    private static void SetPivot(RectTransform rt, Vector2 pivot)
    {
        rt.pivot = pivot;
    }

    private static GameObject CreateIconPrefab(string name, Color color, float size)
    {
        var go = new GameObject(name);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }
}
