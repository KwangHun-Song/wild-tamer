using Base;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CommonPopup : Popup
{
    public override string PopupName => "Popups/CommonPopup";

    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private Button okButton;
    [SerializeField] private Button cancelButton;

    public override UniTask ShowAsync(object enterParam = null)
    {
        if (enterParam is CommonPopupParam param)
        {
            titleText.text = param.Title;
            contentText.text = param.Content;
            cancelButton.gameObject.SetActive(param.HasTwoButtons);
            okButton.GetComponentInChildren<TextMeshProUGUI>().text = param.FirstButtonText;
            cancelButton.GetComponentInChildren<TextMeshProUGUI>().text = param.SecondButtonText;
        }
        else
        {
            Facade.Logger?.Log("[CommonPopup] enterParam이 CommonPopupParam이 아닙니다.", LogLevel.Warning);
        }

        okButton.onClick.AddListener(OnOkClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);

        return base.ShowAsync(enterParam);
    }

    public override void Close(object leaveParam = null)
    {
        okButton.onClick.RemoveListener(OnOkClicked);
        cancelButton.onClick.RemoveListener(OnCancelClicked);
        base.Close(leaveParam);
    }

    private void OnOkClicked() => Close(true);
    private void OnCancelClicked() => Close(false);
}
