using FiniteStateMachine;
using UnityEngine;

/// <summary>보스 대기 상태. detectionRange가 999f이므로 첫 Update에서 즉시 Detected 트리거 발화.</summary>
public class BossIdleState : State<BossMonster, BossTrigger>
{
    public override void OnEnter() => Owner.View.PlayIdleAnimation();

    public override void OnUpdate()
    {
        var pos = (Vector2)Owner.Transform.position;
        foreach (var u in Owner.UnitGrid.Query(pos, Owner.BossData.detectionRange))
        {
            if (u.Team == Owner.Team || !u.IsAlive) continue;
            StateMachine.ExecuteCommand(BossTrigger.Detected);
            return;
        }
    }
}
