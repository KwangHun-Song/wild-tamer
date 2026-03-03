using Base;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class SettingPopup : Popup
{
    public override string PopupName => "Popups/SettingPopup";

    public void OnClickSave()
    {
        GameSaveManager.RequestSave();
    }

    public void OnClickCollection()
    {
        OpenCollectionPopupAsync().Forget();

        async UniTaskVoid OpenCollectionPopupAsync()
        {
            await Facade.PopupManager.ShowAsync<bool>("Popups/CollectionPopup");
        }
    }

    // ── 치트 ──────────────────────────────────────────────────────────

    public void OnClickSpawnWarrior()
    {
        GlobalNotifier.Notify<ICheatListener>(l => l.OnCheatSpawnSquadMember("MonsterData_Warrior"));
    }

    public void OnClickSpawnArcher()
    {
        GlobalNotifier.Notify<ICheatListener>(l => l.OnCheatSpawnSquadMember("MonsterData_Archer"));
    }

    public void OnClickSpawnLancer()
    {
        GlobalNotifier.Notify<ICheatListener>(l => l.OnCheatSpawnSquadMember("MonsterData_Lancer"));
    }

    public void OnClickSetBossTimer10()
    {
        GlobalNotifier.Notify<ICheatListener>(l => l.OnCheatSetBossTimer(10f));
    }
}
