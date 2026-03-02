using System;
using FiniteStateMachine;
using UnityEngine;

public enum MonsterRole { Standalone, Leader, Follower }

public class Monster : Character
{
    public override UnitTeam Team => UnitTeam.Enemy;
    public override float Radius => Data.radius;
    public MonsterData Data { get; }

    /// <summary>MonsterDestroyState에서 발생. EntitySpawner가 구독하여 Despawn을 처리한다.</summary>
    public event Action<Monster> OnReadyToDespawn;

    /// <summary>테이밍된 몬스터의 DestroyState에서 발생. TamingSystem이 구독하여 SquadMember를 스폰한다.</summary>
    public event Action<Monster> OnReadyToSpawnTamed;

    public bool IsTamed { get; private set; }
    public Vector2 TamingSpawnPos { get; private set; }

    private readonly MonsterView monsterView;
    private readonly ObstacleGrid obstacleGrid;
    private StateMachine<Monster, MonsterTrigger> fsm;

    private bool isFollowerMoving;

    /// <summary>
    /// 피격 어그로 대상. 피해를 입힌 유닛을 추적하며, 살아있는 동안 Chase 우선 목표로 사용한다.
    /// </summary>
    public IUnit AggroTarget { get; private set; }

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
        OnAttackFired    += View.PlayAttackAnimation;

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
        fsm = new MonsterLeaderFSM(this, unitGrid, obstacleGrid, skipCreate: true);
        fsm.SetUp();
    }

    private void OnHealthDamaged(int _) => monsterView.PlayHitEffect();

    /// <summary>
    /// 팔로워 멤버가 적을 탐지했을 때 리더(자신)에게 호출한다.
    /// 이미 어그로가 활성 중이면 무시한다.
    /// </summary>
    public void NotifyEnemyDetected(IUnit enemy)
    {
        if (enemy == null || enemy.Team == Team || !Health.IsAlive) return;
        if (AggroTarget?.IsAlive == true) return;
        AggroTarget = enemy;
        fsm?.ExecuteCommand(MonsterTrigger.DetectEnemy);
    }

    /// <summary>
    /// CombatSystem이 데미지를 입힌 후 호출한다.
    /// 적 팀 공격자를 AggroTarget으로 설정하고, 인식 범위 밖이어도 Chase를 강제 시작한다.
    /// </summary>
    public void NotifyDamagedBy(IUnit attacker)
    {
        if (attacker == null || attacker.Team == Team || !Health.IsAlive) return;
        AggroTarget = attacker;
        fsm?.ExecuteCommand(MonsterTrigger.DetectEnemy);
    }

    private void OnHealthDeath()
    {
        monsterView.PlayDeathEffect();
        if (fsm != null)
        {
            fsm.ExecuteCommand(MonsterTrigger.Die);
        }
        else
        {
            // 팔로워: FSM 없음, 직접 애니메이션 재생
            View.Movement.Move(Vector2.zero);
            View.PlayDeathSequence(NotifyDestroyReady);
        }
    }

    /// <summary>TamingSystem에서 호출. 테이밍 성공 시 위치와 플래그를 설정한다.</summary>
    public void MarkAsTamed(Vector2 pos)
    {
        IsTamed = true;
        TamingSpawnPos = pos;
    }

    /// <summary>CreateState에서 연출 완료 후 호출. Idle/Wander 전환을 트리거한다.</summary>
    public void SignalCreated()
    {
        fsm?.ExecuteCommand(MonsterTrigger.Created);
    }

    /// <summary>DeadState에서 DeathSequence 완료 후 호출. DestroyState로 전환을 트리거한다.</summary>
    public void RequestDestroy()
    {
        fsm?.ExecuteCommand(MonsterTrigger.Destroy);
    }

    /// <summary>DestroyState(또는 팔로워 직접 호출)에서 호출. 테이밍 스폰 및 Despawn을 트리거한다.</summary>
    public void NotifyDestroyReady()
    {
        if (IsTamed) OnReadyToSpawnTamed?.Invoke(this);
        OnReadyToDespawn?.Invoke(this);
    }

    /// <summary>EntitySpawner.DespawnMonster()에서 호출.</summary>
    public void Cleanup()
    {
        Health.OnDamaged      -= OnHealthDamaged;
        Health.OnDeath        -= OnHealthDeath;
        OnAttackFired         -= View.PlayAttackAnimation;
        OnReadyToDespawn       = null;
        OnReadyToSpawnTamed    = null;
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
