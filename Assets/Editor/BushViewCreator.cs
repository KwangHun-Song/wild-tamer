using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Bush1View~Bush4View 프리팹과 Idle 스케일 애니메이션 에셋을 생성하는 에디터 도구.
/// Menu → Tools/WorldMap/Create Bush Prefabs
///
/// 생성 결과:
/// - Assets/Animations/Clips/Bush/Bush1_Idle.anim ~ Bush4_Idle.anim
/// - Assets/Animations/Clips/Bush/Bush1_Idle.controller ~ Bush4_Idle.controller
/// - Assets/Prefabs/WorldMap/Bushes/Bush1View.prefab ~ Bush4View.prefab
///
/// 구조 (Tree#View 와 동일):
///   BushNView  ← Animator (BushN_Idle.controller)
///   └── Visual ← SpriteRenderer (BusheN.png, SortingOrder 1900)
///
/// 애니메이션: 미세 스케일 호흡 (1.0 → 1.04 → 1.0, 1.6 초 루프, 보간 Smooth)
/// </summary>
public static class BushViewCreator
{
    private const string SpritePath = "Assets/Graphic/Sprites/Terrain/Decorations/Bushes/";
    private const string AnimPath   = "Assets/Animations/Clips/Bush/";
    private const string PrefabPath = "Assets/Prefabs/WorldMap/Bushes/";
    private const int    Order      = 1900;

    [MenuItem("Tools/WorldMap/Create Bush Prefabs")]
    public static void Create()
    {
        EnsureDir("Assets/Animations/Clips", "Bush");
        EnsureDir("Assets/Prefabs/WorldMap",  "Bushes");

        int created = 0;
        for (int i = 1; i <= 4; i++)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritePath}Bushe{i}.png");
            if (sprite == null)
            {
                Debug.LogWarning($"[BushViewCreator] 스프라이트 없음: Bushe{i}.png — 건너뜀");
                continue;
            }

            // ── Animation Clip ───────────────────────────────────────────
            var clip = MakeClip(i);
            var clipPath = $"{AnimPath}Bush{i}_Idle.anim";
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) != null)
                AssetDatabase.DeleteAsset(clipPath);
            AssetDatabase.CreateAsset(clip, clipPath);
            var savedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

            // ── Animator Controller ───────────────────────────────────────
            var ctrlPath = $"{AnimPath}Bush{i}_Idle.controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null)
                AssetDatabase.DeleteAsset(ctrlPath);

            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            var sm   = ctrl.layers[0].stateMachine;
            var st   = sm.AddState("Idle");
            st.motion       = savedClip;
            sm.defaultState = st;
            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();

            // ── Prefab ────────────────────────────────────────────────────
            MakePrefab(i, sprite, ctrl);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BushViewCreator] 완료 — Bush 프리팹 {created}개 생성.");
    }

    // ── 내부 헬퍼 ─────────────────────────────────────────────────────────

    /// <summary>스케일 호흡 애니메이션 클립을 생성한다.</summary>
    private static AnimationClip MakeClip(int index)
    {
        var clip = new AnimationClip { name = $"Bush{index}_Idle" };

        var cfg = AnimationUtility.GetAnimationClipSettings(clip);
        cfg.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, cfg);

        // 1.0 → 1.04 → 1.0, Smooth 보간
        var keys = new[]
        {
            new Keyframe(0f,   1f)    { inTangent = 0, outTangent = 0 },
            new Keyframe(0.8f, 1.04f) { inTangent = 0, outTangent = 0 },
            new Keyframe(1.6f, 1f)    { inTangent = 0, outTangent = 0 },
        };
        var curve = new AnimationCurve(keys);

        clip.SetCurve("Visual", typeof(Transform), "localScale.x", curve);
        clip.SetCurve("Visual", typeof(Transform), "localScale.y", curve);

        return clip;
    }

    /// <summary>Bush#View 프리팹을 생성하고 에셋으로 저장한다.</summary>
    private static void MakePrefab(int index, Sprite sprite, RuntimeAnimatorController ctrl)
    {
        var savePath = $"{PrefabPath}Bush{index}View.prefab";

        var root     = new GameObject($"Bush{index}View");
        var animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = ctrl;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        var sr          = visual.AddComponent<SpriteRenderer>();
        sr.sprite       = sprite;
        sr.sortingOrder = Order;

        PrefabUtility.SaveAsPrefabAsset(root, savePath);
        Object.DestroyImmediate(root);
    }

    private static void EnsureDir(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
            AssetDatabase.CreateFolder(parent, child);
    }
}
