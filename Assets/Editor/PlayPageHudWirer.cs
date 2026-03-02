using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PlayPage 프리팹의 HUD 레퍼런스를 자동으로 연결하는 에디터 유틸리티.
/// </summary>
public static class PlayPageHudWirer
{
    [MenuItem("Tools/Wire PlayPage HUD References")]
    public static void WireReferences()
    {
        const string path = "Assets/Resources/PlayPage.prefab";
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefabAsset == null)
        {
            Debug.LogError("[PlayPageHudWirer] PlayPage.prefab을 찾을 수 없습니다.");
            return;
        }

        using var scope = new PrefabUtility.EditPrefabContentsScope(path);
        var root = scope.prefabContentsRoot;

        // HpBarArea RectTransform 설정 (수평 스트레치, 좌측 Portrait 영역 제외)
        var hpBarAreaGo = FindByPath(root, "Canvas/BottomHud/HpBarArea");
        if (hpBarAreaGo != null)
        {
            var rt = hpBarAreaGo.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin        = new Vector2(0f, 0.5f);
                rt.anchorMax        = new Vector2(1f, 0.5f);
                rt.pivot            = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(80f, 0f);
                rt.sizeDelta        = new Vector2(-200f, 54f);
                Debug.Log("[PlayPageHudWirer] HpBarArea RectTransform 설정 완료");
            }
        }

        // PlayerHpBarView.fill → HpBarFill Image
        var hpBarView = root.GetComponentInChildren<PlayerHpBarView>(includeInactive: true);
        var hpBarFillGo = FindByPath(root, "Canvas/BottomHud/HpBarArea/HpBarFill");
        if (hpBarView != null && hpBarFillGo != null)
        {
            var fillImage = hpBarFillGo.GetComponent<Image>();
            var so = new SerializedObject(hpBarView);
            so.FindProperty("fill").objectReferenceValue = fillImage;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[PlayPageHudWirer] PlayerHpBarView.fill 연결 완료");
        }
        else
        {
            Debug.LogWarning($"[PlayPageHudWirer] hpBarView={hpBarView}, hpBarFillGo={hpBarFillGo}");
        }

        // PlayPage.playerHpBar → PlayerHpBarView on BottomHud
        var playPage = root.GetComponent<PlayPage>();
        var bottomHudGo = FindByPath(root, "Canvas/BottomHud");
        if (playPage != null && bottomHudGo != null)
        {
            var view = bottomHudGo.GetComponent<PlayerHpBarView>();
            var so = new SerializedObject(playPage);
            so.FindProperty("playerHpBar").objectReferenceValue = view;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[PlayPageHudWirer] PlayPage.playerHpBar 연결 완료");
        }
        else
        {
            Debug.LogWarning($"[PlayPageHudWirer] playPage={playPage}, bottomHudGo={bottomHudGo}");
        }
    }

    private static GameObject FindByPath(GameObject root, string path)
    {
        var t = root.transform.Find(path);
        return t != null ? t.gameObject : null;
    }
}
