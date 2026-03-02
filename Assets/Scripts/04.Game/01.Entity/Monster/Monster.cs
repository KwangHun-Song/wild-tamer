using FiniteStateMachine;
using UnityEngine;

public enum MonsterRole { Standalone, Leader, Follower }

public class Monster : Character
{
    public override UnitTeam Team => UnitTeam.Enemy;
    public MonsterData Data { get; }

    private readonly MonsterView monsterView;
    private readonly ObstacleGrid obstacleGrid;
    private StateMachine<Monster, MonsterTrigger> fsm;

    private bool isFollowerMoving;

    /// <summary>스탠드얼론 몬스터용.</summary>
    public Monster(MonsterView view, MonsterData data, SpatialGrid<IUnit> unitGrid)
        : this(view, data, unitGrid, MonsterRole.Standalone) { }

    public Monster(MonsterView view, MonsterData data, SpatialGrid<IUnit> unitGrid, MonsterRole role, ObstacleGrid obstacleGrid = null)
        : base(view, CreateCombat(data))
    {
        Data = data;
        monsterView = view;
        this.obstacleGrid = obstacleGrid;
        Health.Initialize(data.maxHp);
        view.Movement.MoveSpeed = data.moveSpeed;
        Health.OnDamaged += OnHealthDamaged;
        Health.OnDeath   += OnHealthDeath;

        fsm = role switch
        {
            MonsterRole.Leader   => new MonsterLeaderFSM(this, unitGrid, obstacleGrid),
            MonsterRole.Follower => null,
            _                    => new MonsterStandaloneFSM(this, unitGrid),
        };
        fsm?.SetUp();
    }

    /// <summary>리더 승계 시 호출. 팔로워에게 리더 AI를 부여한다.</summary>
    public void PromoteToLeader(SpatialGrid<IUnit> unitGrid)
    {
        fsm = new MonsterLeaderFSM(this, unitGrid, obstacleGrid);
        fsm.SetUp();
    }

    private void OnHealthDamaged(int _) => monsterView.PlayHitEffect();

    private void OnHealthDeath()
    {
        monsterView.PlayDeathEffect();
        fsm?.ExecuteCommand(MonsterTrigger.Die);
    }

    /// <summary>EntitySpawner.DespawnMonster()에서 호출.</summary>
    public void Cleanup()
    {
        Health.OnDamaged -= OnHealthDamaged;
        Health.OnDeath   -= OnHealthDeath;
    }

    /// <summary>EntitySpawner.Update() 또는 MonsterSquad.Update()에서 매 프레임 호출.</summary>
    public void Update()
    {
        fsm?.Update();
#if UNITY_EDITOR
        View.SetGizmoLabel(fsm?.CurrentState?.GetType().Name ?? "None");
#endif
    }

    /// <summary>
    /// 팔로워 몬스터가 MonsterSquad에 의해 이동 방향을 받을 때 호출된다.
    /// FSM이 없는 팔로워는 이 메서드에서 애니메이션과 이동을 직접 처리한다.
    /// </summary>
    public void Move(Vector2 direction)
    {
        if (fsm == null)
        {
            bool moving = direction.magnitude > 0.01f;
            if (moving != isFollowerMoving)
            {
                if (moving) monsterView.PlayMoveAnimation();
                else monsterView.PlayIdleAnimation();
                isFollowerMoving = moving;
            }
        }

        View.Movement.Move(direction);
        if (direction.magnitude > 0.01f)
            View.UpdateFacing(direction);
    }

    public void PlayTamingEffect() => monsterView.PlayTamingEffect();

    private static UnitCombat CreateCombat(MonsterData d)
        => new UnitCombat(d.attackDamage, d.attackRange, d.detectionRange, d.attackCooldown);
}
