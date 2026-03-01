using Base;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class PlayPage : Page
{
    public override string PageName => "PlayPage";

    [SerializeField] private Button settingButton;
    [SerializeField] private Transform worldMapRoot;

    public Transform WorldMapRoot => worldMapRoot;

    public override UniTask ShowAsync(object param = null)
    {
        settingButton.onClick.AddListener(OnSettingButtonClicked);
        return base.ShowAsync(param);
    }

    public override void Hide()
    {
        settingButton.onClick.RemoveListener(OnSettingButtonClicked);
        base.Hide();
    }

    private void OnSettingButtonClicked()
    {
        // TODO: 세팅 팝업 열기 (Phase 3에서 구현)
        Facade.Logger.Log("Setting button clicked");
    }
}
