using FiniteStateMachine;
using UnityEngine;

public class MonsterWanderState : State<Monster, MonsterTrigger>
{
    private ObstacleGrid obstacleGrid;

    private Vector2 wanderDirection;
    private float wanderTimer;
    private const float WanderChangeInterval = 3f;

    protected override void OnSetUp()
    {
        if (StateMachine is MonsterLeaderFSM leaderFsm)
            obstacleGrid = leaderFsm.ObstacleGrid;
    }

    public override void OnEnter()
    {
        Owner.View.PlayIdleAnimation();
        PickNewWanderDirection();
    }

    public override void OnUpdate()
    {
        // DetectEnemy: FSM Transition이 탐지 범위 내 적 조건으로 자동 처리
        var pos = (Vector2)Owner.Transform.position;

        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f)
            PickNewWanderDirection();

        var resolved = ResolveDirection(pos, wanderDirection);
        Owner.View.Movement.Move(resolved);
        if (resolved.magnitude > 0.01f)
            Owner.View.UpdateFacing(resolved);
    }

    private void PickNewWanderDirection()
    {
        wanderDirection = Random.insideUnitCircle.normalized;
        wanderTimer = WanderChangeInterval;
    }

    private Vector2 ResolveDirection(Vector2 pos, Vector2 dir)
    {
        if (obstacleGrid == null) return dir;
        return new Vector2(
            obstacleGrid.IsWalkable(new Vector2(pos.x + dir.x * 0.5f, pos.y)) ? dir.x : 0f,
            obstacleGrid.IsWalkable(new Vector2(pos.x, pos.y + dir.y * 0.5f)) ? dir.y : 0f
        );
    }
}
