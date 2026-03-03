using Base;
using Cysharp.Threading.Tasks;

public class EnterPageState : SceneState
{
    protected override async UniTask OnExecuteAsync()
    {
        await Facade.PageChanger.ChangePageAsync("PlayPage");
    }
}
