using System.Collections.Generic;
using FiniteStateMachine;
using UnityEngine;

public class SquadMemberAttackState : State<SquadMember, SquadMemberTrigger>
{
    private SpatialGrid<IUnit>   unitGrid;
    private readonly List<IUnit> queryBuffer = new();

    protected override void OnSetUp()
    {
        unitGrid = (StateMachine as SquadMemberFSM)?.UnitGrid;
    }

    public override void OnEnter()
    {
        Owner.View.PlayAttackAnimation();

        var pos = (Vector2)Owner.Transform.position;
        var target = FindClosestEnemy(pos, Owner.Combat.AttackRange);
        if (target != null)
        {
            var toTarget = ((Vector2)target.Transform.position - pos).normalized;
            Owner.View.SetFacingImmediate(toTarget);
        }
    }

    public override void OnUpdate()
    {
        // StopAttack: FSM Transition이 공격 범위 내 적 없음 조건으로 자동 처리

        var pos = (Vector2)Owner.Transform.position;

        // 공격 대상 방향으로 Flip 업데이트
        var target = FindClosestEnemy(pos, Owner.Combat.AttackRange);
        if (target != null)
        {
            var toTarget = ((Vector2)target.Transform.position - pos).normalized;
            Owner.View.UpdateFacing(toTarget);
        }

        // 공격 중에도 flock 방향으로 이동 가능
        var dir = Owner.DesiredMoveDirection;
        if (dir.magnitude > 0.01f)
            Owner.View.Movement.Move(dir);

        // 데미지/쿨타임 리셋은 CombatSystem.ProcessCombat()에서 처리
    }

    private IUnit FindClosestEnemy(Vector2 pos, float range)
    {
        if (unitGrid == null) return null;
        IUnit closest = null;
        float minDist = float.MaxValue;
        queryBuffer.Clear();
        unitGrid.Query(pos, range, queryBuffer);
        foreach (var u in queryBuffer)
        {
            if (u.Team == Owner.Team || !u.IsAlive) continue;
            float d = Vector2.Distance(pos, (Vector2)u.Transform.position);
            if (d > range || d >= minDist) continue;
            minDist = d; closest = u;
        }
        return closest;
    }
}
