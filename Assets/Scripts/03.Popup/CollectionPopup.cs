using System.Linq;
using Base;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class CollectionPopup : Popup
{
    public override string PopupName => "Popups/CollectionPopup";

    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private CollectionItem itemPrefab;

    public override UniTask ShowAsync(object enterParam = null)
    {
        LoadCollection();
        return base.ShowAsync(enterParam);
    }

    private void LoadCollection()
    {
        var content = scrollRect.content;

        // 기존 아이템 전체 제거
        foreach (Transform child in content)
            Destroy(child.gameObject);

        var userData = UserData.Load();
        foreach (var id in userData.tamedMonsterIds)
        {
            // 에셋명으로 먼저 조회, 실패 시 id 필드로 폴백 (구 저장 데이터 호환)
            if (!Facade.DB.TryGet<MonsterData>(id, out var monsterData))
                monsterData = Facade.DB.GetAll<MonsterData>().FirstOrDefault(d => d.id == id);
            if (monsterData == null)
            {
                Facade.Logger?.Log($"[CollectionPopup] MonsterData '{id}' 조회 실패. 스킵.", LogLevel.Warning);
                continue;
            }
            var item = Instantiate(itemPrefab, content);
            item.Initialize(monsterData);
        }
    }
}
