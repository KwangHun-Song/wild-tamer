using Base;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class LoadState : SceneState
{
    protected override async UniTask OnExecuteAsync()
    {
        WarmupPopupsAsync().Forget();
        Debug.Log("[PlayScene] LoadState: 리소스 로딩 완료");
        await UniTask.CompletedTask;
    }

    private static async UniTaskVoid WarmupPopupsAsync()
    {
        await UniTask.WhenAll(
            Resources.LoadAsync<GameObject>("Popups/CommonPopup").ToUniTask(),
            Resources.LoadAsync<GameObject>("Popups/SettingPopup").ToUniTask(),
            Resources.LoadAsync<GameObject>("Popups/CollectionPopup").ToUniTask()
        );
    }
}
