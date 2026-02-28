using Base;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class InitState : SceneState
{
    protected override async UniTask OnExecuteAsync()
    {
        Debug.Log("[PlayScene] InitState: 씬 초기화 완료");
        await UniTask.CompletedTask;
    }
}
