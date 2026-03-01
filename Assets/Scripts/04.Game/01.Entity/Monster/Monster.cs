using System;
using UnityEngine;

public enum MonsterRole { Standalone, Leader, Follower }

public class Monster : Character
{
    public override UnitTeam Team => UnitTeam.Enemy;
    public MonsterData Data { get; }

    private readonly MonsterView monsterView;
    private IMonsterBehavior behavior;

    public event Action<Vector2> OnMoveRequested;

    /// <summary>스탠드얼론 몬스터용 (기존 코드 호환).</summary>
    public Monster(MonsterView view, MonsterData data, SpatialGrid<IUnit> unitGrid)
        : this(view, data, unitGrid, MonsterRole.Standalone) { }

    public Monster(MonsterView view, MonsterData data, SpatialGrid<IUnit> unitGrid, MonsterRole role)
        : base(view, CreateCombat(data))
    {
        Data = data;
        monsterView = view;
        Health.Initialize(data.maxHp);
        view.Movement.MoveSpeed = data.moveSpeed;
        Health.OnDamaged += OnHealthDamaged;
        Health.OnDeath   += OnHealthDeath;
        view.Subscribe(this);

        behavior = role switch
        {
            MonsterRole.Leader   => new MonsterLeaderAI(this, unitGrid),
            MonsterRole.Follower => null,
            _                    => new MonsterAI(this, unitGrid),
        };
        behavior?.SetUp();
    }

    /// <summary>리더 승계 시 호출. 팔로워에게 리더 AI를 부여한다.</summary>
    public void PromoteToLeader(SpatialGrid<IUnit> unitGrid)
    {
        behavior = new MonsterLeaderAI(this, unitGrid);
        behavior.SetUp();
    }

    private void OnHealthDamaged(int _) => monsterView.PlayHitEffect();
    private void OnHealthDeath()        => monsterView.PlayDeathEffect();

    /// <summary>EntitySpawner.DespawnMonster()에서 호출. Health 이벤트 구독을 해제한다.</summary>
    public void Cleanup()
    {
        Health.OnDamaged -= OnHealthDamaged;
        Health.OnDeath   -= OnHealthDeath;
    }

    /// <summary>EntitySpawner.Update() 또는 MonsterSquad.Update()에서 매 프레임 호출.</summary>
    public void Update() => behavior?.Update();

    public void Move(Vector2 direction) => OnMoveRequested?.Invoke(direction);

    public void PlayTamingEffect() => monsterView.PlayTamingEffect();

    private static UnitCombat CreateCombat(MonsterData d)
        => new UnitCombat(d.attackDamage, d.attackRange, d.detectionRange, d.attackCooldown);
}
