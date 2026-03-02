using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 몬스터 프리팹 세 종류에 UnitHpBarView 계층 구조를 자동으로 추가하고
/// 직렬화 레퍼런스를 연결하는 에디터 유틸리티.
///
/// SmallBar_Base.png 는 Multiple 스프라이트(왼쪽 캡 / 중앙 / 오른쪽 캡)이므로
/// HpBarBase 아래에 L, C, R 세 자식 SpriteRenderer로 조립한다.
/// C는 Fill 폭에 맞게 X 스케일되고, L/R은 그 양 끝에 정렬된다.
/// </summary>
public static class MonsterHpBarWirer
{
    private static readonly string[] PrefabPaths =
    {
        "Assets/Prefabs/Monster/MonsterWarriorView.prefab",
        "Assets/Prefabs/Monster/MonsterArcherView.prefab",
        "Assets/Prefabs/Monster/MonsterLancerView.prefab",
    };

    private const string BasePath = "Assets/Graphic/UI/Bars/SmallBar_Base.png";
    private const string FillPath = "Assets/Graphic/UI/Bars/SmallBar_Fill.png";
    private const float HpBarYOffset = 0.6f;

    [MenuItem("Tools/Wire Monster HpBar References")]
    public static void WireAll()
    {
        // SmallBar_Base 에서 3개 서브 스프라이트 로드
        var baseAll = AssetDatabase.LoadAllAssetsAtPath(BasePath).OfType<Sprite>().ToArray();
        var leftSprite   = baseAll.FirstOrDefault(s => s.name == "SmallBar_Base_0");
        var centerSprite = baseAll.FirstOrDefault(s => s.name == "SmallBar_Base_1");
        var rightSprite  = baseAll.FirstOrDefault(s => s.name == "SmallBar_Base_2");

        if (leftSprite == null || centerSprite == null || rightSprite == null)
        {
            Debug.LogError("[MonsterHpBarWirer] SmallBar_Base 서브 스프라이트(0/1/2)를 로드할 수 없습니다. " +
                           "Sprite Editor에서 슬라이싱이 되어 있는지 확인하세요.");
            return;
        }

        var fillSprite = AssetDatabase.LoadAllAssetsAtPath(FillPath).OfType<Sprite>().FirstOrDefault();
        if (fillSprite == null)
        {
            Debug.LogError("[MonsterHpBarWirer] SmallBar_Fill 스프라이트를 로드할 수 없습니다.");
            return;
        }

        foreach (var path in PrefabPaths)
            WirePrefab(path, leftSprite, centerSprite, rightSprite, fillSprite);

        AssetDatabase.SaveAssets();
        Debug.Log("[MonsterHpBarWirer] 모든 몬스터 프리팹 HP 바 연결 완료.");
    }

    private static void WirePrefab(
        string path,
        Sprite leftSprite, Sprite centerSprite, Sprite rightSprite,
        Sprite fillSprite)
    {
        using var scope = new PrefabUtility.EditPrefabContentsScope(path);
        var root = scope.prefabContentsRoot;

        // ── HpBarRoot ──────────────────────────────────────────────────────────
        var hpBarRootT = root.transform.Find("HpBarRoot");
        if (hpBarRootT == null)
        {
            hpBarRootT = new GameObject("HpBarRoot").transform;
            hpBarRootT.SetParent(root.transform, worldPositionStays: false);
        }
        hpBarRootT.localPosition = new Vector3(0f, HpBarYOffset, 0f);

        var hpBarView = hpBarRootT.GetComponent<UnitHpBarView>()
                        ?? hpBarRootT.gameObject.AddComponent<UnitHpBarView>();

        // ── Fill ───────────────────────────────────────────────────────────────
        var fillT = hpBarRootT.Find("Fill");
        if (fillT == null)
        {
            fillT = new GameObject("Fill").transform;
            fillT.SetParent(hpBarRootT, worldPositionStays: false);
        }
        fillT.localPosition = Vector3.zero;
        var fillSr = fillT.GetComponent<SpriteRenderer>()
                     ?? fillT.gameObject.AddComponent<SpriteRenderer>();
        fillSr.sprite       = fillSprite;
        fillSr.sortingOrder = SortingOrder.Unit + 2;

        // ── HpBarBase 컨테이너 재생성 ───────────────────────────────────────────
        // (이전 실행에서 단일 SpriteRenderer로 만들어진 경우를 포함해 재구성)
        var oldBase = hpBarRootT.Find("HpBarBase");
        if (oldBase != null)
            Object.DestroyImmediate(oldBase.gameObject);

        var baseContainer = new GameObject("HpBarBase").transform;
        baseContainer.SetParent(hpBarRootT, worldPositionStays: false);
        baseContainer.localPosition = Vector3.zero;

        // ── 크기 계산 (모두 센터 피벗, 단위: Unity units = px / PPU) ─────────────
        float fillW   = fillSprite.bounds.size.x;   // Fill 스프라이트 폭
        float centerW = centerSprite.bounds.size.x;
        float leftW   = leftSprite.bounds.size.x;
        float rightW  = rightSprite.bounds.size.x;

        // C: Fill과 동일한 폭이 되도록 X 스케일, 중심을 (0,0)에 맞춤
        var centerT = CreateSpriteChild("C", baseContainer, centerSprite, SortingOrder.Unit + 1);
        float scaleX = centerW > 0f ? fillW / centerW : 1f;
        centerT.localScale    = new Vector3(scaleX, 1f, 1f);
        centerT.localPosition = Vector3.zero;

        // L: C의 왼쪽 끝(-fillW/2)에 오른쪽 끝이 붙도록 배치
        var leftT = CreateSpriteChild("L", baseContainer, leftSprite, SortingOrder.Unit + 1);
        leftT.localPosition = new Vector3(-fillW / 2f - leftW / 2f, 0f, 0f);

        // R: C의 오른쪽 끝(+fillW/2)에 왼쪽 끝이 붙도록 배치
        var rightT = CreateSpriteChild("R", baseContainer, rightSprite, SortingOrder.Unit + 1);
        rightT.localPosition = new Vector3(fillW / 2f + rightW / 2f, 0f, 0f);

        // ── 레퍼런스 연결 ──────────────────────────────────────────────────────
        var hpBarViewSo = new SerializedObject(hpBarView);
        hpBarViewSo.FindProperty("fill").objectReferenceValue = fillT;
        hpBarViewSo.ApplyModifiedPropertiesWithoutUndo();

        var monsterView = root.GetComponent<MonsterView>();
        if (monsterView != null)
        {
            var monsterViewSo = new SerializedObject(monsterView);
            monsterViewSo.FindProperty("hpBar").objectReferenceValue = hpBarView;
            monsterViewSo.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning($"[MonsterHpBarWirer] MonsterView 컴포넌트를 찾을 수 없음: {path}");
        }

        Debug.Log($"[MonsterHpBarWirer] 완료: {path}");
    }

    private static Transform CreateSpriteChild(
        string name, Transform parent, Sprite sprite, int sortingOrder)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, worldPositionStays: false);
        t.localPosition = Vector3.zero;
        var sr = t.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite       = sprite;
        sr.sortingOrder = sortingOrder;
        return t;
    }
}
