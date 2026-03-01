using Base;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 게임 플레이 상태의 오케스트레이터.
/// GameController를 생성·관리하고, Unity Update()로 매 프레임 구동한다.
/// PlayPage Canvas에 UICamera를 연결한다.
/// </summary>
public class InPlayState : SceneState
{
    // SceneState는 MonoBehaviour이므로 Update()가 매 프레임 호출된다.
    // 이전 상태(Init, Load, EnterPage) 동안에는 null이므로 no-op으로 안전하다.
    private GameController gameController;

    protected override async UniTask OnExecuteAsync()
    {
        var playStates = (PlayStates)StateMachine;
        var playPage = playStates.PageChanger.CurrentPage as PlayPage;

        if (playPage == null)
        {
            Facade.Logger?.Log("[InPlayState] PlayPage를 찾을 수 없습니다.", LogLevel.Warning);
            return;
        }

        // Canvas에 UICamera 연결 (Screen Space - Camera)
        playPage.Canvas.worldCamera = playStates.UICamera;

        // 맵 생성
        playPage.WorldMap.MapGenerator.Generate();

        // GameController 생성 → Update()가 즉시 구동 시작
        gameController = new GameController(
            playPage.PlayerView,
            playStates.PlayerInput,
            playPage.WorldMap.MapGenerator.ObstacleGrid);

        try
        {
            await UniTask.WaitUntil(() => false, cancellationToken: this.GetCancellationTokenOnDestroy());
        }
        finally
        {
            gameController.Cleanup();
            gameController = null;
        }
    }

    private void Update() => gameController?.Update();
}
