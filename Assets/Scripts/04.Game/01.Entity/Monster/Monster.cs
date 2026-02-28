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
        view.Health.Initialize(data.maxHp);
        view.Movement.MoveSpeed = data.moveSpeed;
        view.Health.OnDamaged += _ => monsterView.PlayHitEffect();
        view.Health.OnDeath += monsterView.PlayDeathEffect;
        view.Subscribe(this);

        ai = new MonsterAI(this, unitGrid);
        ai.SetUp();
    }

    /// <summary>EntitySpawner.Update()에서 매 프레임 호출. MonsterAI에 위임한다.</summary>
    public void Update() => ai.Update();

    public void Move(Vector2 direction) => OnMoveRequested?.Invoke(direction);

    public void PlayTamingEffect() => monsterView.PlayTamingEffect();

    private static UnitCombat CreateCombat(MonsterData d)
        => new UnitCombat(d.attackDamage, d.attackRange, d.detectionRange, d.attackCooldown);
}
