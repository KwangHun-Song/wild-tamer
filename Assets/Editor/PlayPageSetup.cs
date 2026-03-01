using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PlayPage 프리팹의 settingButton, worldMapRoot SerializeField를 자동 연결.
/// Menu: Tools/WorldMap/Setup PlayPage SerializeFields
/// </summary>
public static class PlayPageSetup
{
    [MenuItem("Tools/WorldMap/Setup PlayPage SerializeFields")]
    public static void SetupPlayPage()
    {
        var prefabPath = "Assets/Resources/PlayPage.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[PlayPageSetup] Prefab not found: {prefabPath}");
            return;
        }

        using var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
        var root = scope.prefabContentsRoot;

        var playPage = root.GetComponent<PlayPage>();
        if (playPage == null)
        {
            Debug.LogError("[PlayPageSetup] PlayPage component not found on root.");
            return;
        }

        // SettingButton 찾기 (Button 컴포넌트를 가진 첫 번째 자식)
        var button = root.GetComponentInChildren<Button>(true);
        if (button == null)
        {
            Debug.LogError("[PlayPageSetup] Button component not found.");
            return;
        }

        // WorldMapRoot 찾기
        var worldMapRoot = root.transform.Find("WorldMapRoot");
        if (worldMapRoot == null)
        {
            Debug.LogError("[PlayPageSetup] WorldMapRoot not found.");
            return;
        }

        // SerializedObject로 필드 연결
        var so = new SerializedObject(playPage);
        so.FindProperty("settingButton").objectReferenceValue = button;
        so.FindProperty("worldMapRoot").objectReferenceValue = worldMapRoot;
        so.ApplyModifiedProperties();

        // 버튼 오브젝트 이름 정리
        if (button.name == "Image")
            button.gameObject.name = "SettingButton";

        Debug.Log($"[PlayPageSetup] settingButton → {button.name}, worldMapRoot → {worldMapRoot.name}");
        Debug.Log("[PlayPageSetup] 완료.");
    }
}
