using System;
using UnityEngine;

public class Monster : Character
{
    public override UnitTeam Team => UnitTeam.Enemy;
    public MonsterData Data { get; }

    private readonly MonsterView monsterView;
    private readonly MonsterAI ai;

    public event Action<Vector2> OnMoveRequested;

    public Monster(MonsterView view, MonsterData data, SpatialGrid<IUnit> unitGrid)
        : base(view, CreateCombat(data))
    {
        Data = data;
        monsterView = view;
        Health.Initialize(data.maxHp);
        view.Movement.MoveSpeed = data.moveSpeed;
        Health.OnDamaged += OnHealthDamaged;
        Health.OnDeath   += OnHealthDeath;
        view.Subscribe(this);

        ai = new MonsterAI(this, unitGrid);
        ai.SetUp();
    }

    private void OnHealthDamaged(int _) => monsterView.PlayHitEffect();
    private void OnHealthDeath()        => monsterView.PlayDeathEffect();

    /// <summary>EntitySpawner.DespawnMonster()에서 호출. Health 이벤트 구독을 해제한다.</summary>
    public void Cleanup()
    {
        Health.OnDamaged -= OnHealthDamaged;
        Health.OnDeath   -= OnHealthDeath;
    }

    /// <summary>EntitySpawner.Update()에서 매 프레임 호출. MonsterAI에 위임한다.</summary>
    public void Update() => ai.Update();

    public void Move(Vector2 direction) => OnMoveRequested?.Invoke(direction);

    public void PlayTamingEffect() => monsterView.PlayTamingEffect();

    private static UnitCombat CreateCombat(MonsterData d)
        => new UnitCombat(d.attackDamage, d.attackRange, d.detectionRange, d.attackCooldown);
}
