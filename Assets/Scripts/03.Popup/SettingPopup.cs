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
}
