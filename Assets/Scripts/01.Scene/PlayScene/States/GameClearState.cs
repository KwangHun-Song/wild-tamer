using Base;
using Cysharp.Threading.Tasks;

/// <summary>
/// 보스 처치(게임 승리) 후 처리 상태.
/// 승리 팝업을 표시하고 SceneStateMachine 재시작을 요청한다.
/// </summary>
public class GameClearState : SceneState
{
    public override bool CanEnter()
        => ((PlayStates)StateMachine).LastResult == GameResult.Win;

    protected override async UniTask OnExecuteAsync()
    {
        var param = new CommonPopupParam(
            "승리!",
            "보스를 처치했습니다!",
            hasTwoButtons: false,
            firstButtonText: "재시작");
        await Facade.PopupManager.ShowAsync<bool>("Popups/CommonPopup", param);
        StateMachine.RequestRestart();
    }
}
