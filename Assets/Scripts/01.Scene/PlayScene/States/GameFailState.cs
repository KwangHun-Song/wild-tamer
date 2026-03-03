using Base;
using Cysharp.Threading.Tasks;

/// <summary>
/// 플레이어 사망(게임 패배) 후 처리 상태.
/// 패배 팝업을 표시하고 SceneStateMachine 재시작을 요청한다.
/// </summary>
public class GameFailState : SceneState
{
    public override bool CanEnter()
        => ((PlayStates)StateMachine).LastResult == GameResult.Lose;

    protected override async UniTask OnExecuteAsync()
    {
        var param = new CommonPopupParam(
            "패배",
            "쓰러졌습니다.",
            hasTwoButtons: false,
            firstButtonText: "재시작");
        await Facade.PopupManager.ShowAsync<bool>("Popups/CommonPopup", param);
        StateMachine.RequestRestart();
    }
}
