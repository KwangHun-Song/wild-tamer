using Base;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class LoadState : SceneState
{
    protected override async UniTask OnExecuteAsync()
    {
        Debug.Log("[PlayScene] LoadState: 리소스 로딩 완료");
        await UniTask.CompletedTask;
    }
}
