using Base;
using Cysharp.Threading.Tasks;

public class EnterPageState : SceneState
{
    protected override async UniTask OnExecuteAsync()
    {
        var playStates = (PlayStates)StateMachine;
        await playStates.PageChanger.ChangePageAsync("PlayPage");
    }
}
