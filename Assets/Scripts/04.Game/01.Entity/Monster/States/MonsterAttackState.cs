using FiniteStateMachine;
using UnityEngine;

public class MonsterAttackState : State<Monster, MonsterTrigger>
{
    private SpatialGrid<IUnit> unitGrid;
    private float originalSpeed;

    protected override void OnSetUp()
    {
        unitGrid = (StateMachine as MonsterStandaloneFSM)?.UnitGrid
            ?? (StateMachine as MonsterLeaderFSM)?.UnitGrid;
    }

    public override void OnEnter()
    {
        originalSpeed = Owner.View.Movement.MoveSpeed;
        Owner.View.Movement.MoveSpeed = originalSpeed * 0.5f;
        Owner.View.PlayAttackAnimation();
    }

    public override void OnExit()
    {
        Owner.View.Movement.MoveSpeed = originalSpeed;
        Owner.View.Movement.Move(Vector2.zero);
    }

    public override void OnUpdate()
    {
        // OutOfAttackRange: FSM Transition이 공격 범위 내 적 없음 조건으로 자동 처리
        var pos = (Vector2)Owner.Transform.position;
        var target = FindClosestEnemy(pos, Owner.Combat.AttackRange);

        if (target == null)
        {
            Owner.View.Movement.Move(Vector2.zero);
            return;
        }

        // 공격 중에도 적을 향해 이동
        var dir = ((Vector2)target.Transform.position - pos).normalized;
        Owner.View.Movement.Move(dir);
        Owner.View.UpdateFacing(dir);

        if (Owner.Combat.CanAttack)
            Owner.Combat.ResetCooldown();
    }

    private IUnit FindClosestEnemy(Vector2 pos, float range)
    {
        if (unitGrid == null) return null;
        IUnit closest = null;
        float minDist = float.MaxValue;
        foreach (var u in unitGrid.Query(pos, range))
        {
            if (u.Team == Owner.Team || !u.IsAlive) continue;
            float d = Vector2.Distance(pos, (Vector2)u.Transform.position);
            if (d < minDist) { minDist = d; closest = u; }
        }
        return closest;
    }
}
