using Base;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class InPlayState : SceneState
{
    protected override async UniTask OnExecuteAsync()
    {
        Debug.Log("[PlayScene] InPlayState: 게임 플레이 진입");
        await UniTask.WaitUntil(() => false, cancellationToken: this.GetCancellationTokenOnDestroy());
    }
}
