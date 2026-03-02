using FiniteStateMachine;
using UnityEngine;

public class MonsterChaseState : State<Monster, MonsterTrigger>
{
    private SpatialGrid<IUnit> unitGrid;
    private ObstacleGrid obstacleGrid;

    protected override void OnSetUp()
    {
        if (StateMachine is MonsterStandaloneFSM standaloneFsm)
            unitGrid = standaloneFsm.UnitGrid;
        else if (StateMachine is MonsterLeaderFSM leaderFsm)
        {
            unitGrid = leaderFsm.UnitGrid;
            obstacleGrid = leaderFsm.ObstacleGrid;
        }
    }

    public override void OnEnter()
    {
        Owner.View.PlayMoveAnimation();
    }

    public override void OnUpdate()
    {
        // LoseEnemy / InAttackRange: FSM Transition이 조건으로 자동 처리
        var pos = (Vector2)Owner.Transform.position;
        var target = FindClosestEnemy(pos, Owner.Combat.DetectionRange);

        if (target == null)
        {
            Owner.View.Movement.Move(Vector2.zero);
            return;
        }

        if (Vector2.Distance(pos, (Vector2)target.Transform.position) <= Owner.Combat.AttackRange)
        {
            Owner.View.Movement.Move(Vector2.zero);
            return;
        }

        var dir = ((Vector2)target.Transform.position - pos).normalized;
        var resolved = ResolveDirection(pos, dir);
        Owner.View.Movement.Move(resolved);
        if (resolved.magnitude > 0.01f)
            Owner.View.UpdateFacing(resolved);
    }

    public override void OnExit()
    {
        Owner.View.Movement.Move(Vector2.zero);
    }

    private Vector2 ResolveDirection(Vector2 pos, Vector2 dir)
    {
        if (obstacleGrid == null) return dir;
        return new Vector2(
            obstacleGrid.IsWalkable(new Vector2(pos.x + dir.x * 0.5f, pos.y)) ? dir.x : 0f,
            obstacleGrid.IsWalkable(new Vector2(pos.x, pos.y + dir.y * 0.5f)) ? dir.y : 0f
        );
    }

    private IUnit FindClosestEnemy(Vector2 pos, float range)
    {
        // 피격 어그로 대상이 살아있으면 최우선 추적 (인식 범위 무시)
        if (Owner.AggroTarget?.IsAlive == true)
            return Owner.AggroTarget;

        if (unitGrid == null) return null;

        // 스쿼드 멤버를 플레이어보다 우선 타겟팅
        IUnit squadTarget = null;
        float squadMinDist = float.MaxValue;
        IUnit otherTarget = null;
        float otherMinDist = float.MaxValue;

        foreach (var u in unitGrid.Query(pos, range))
        {
            if (u.Team == Owner.Team || !u.IsAlive) continue;
            float d = Vector2.Distance(pos, (Vector2)u.Transform.position);
            if (d > range) continue;

            if (u is SquadMember)
            {
                if (d < squadMinDist) { squadMinDist = d; squadTarget = u; }
            }
            else
            {
                if (d < otherMinDist) { otherMinDist = d; otherTarget = u; }
            }
        }

        return squadTarget ?? otherTarget;
    }
}
