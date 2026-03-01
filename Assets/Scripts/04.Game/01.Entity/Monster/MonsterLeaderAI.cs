using FiniteStateMachine;
using UnityEngine;

/// <summary>
/// 몬스터 스쿼드 리더 전용 AI.
/// Wander(배회) ↔ Chase(추적) → Attack 상태 기계로 동작한다.
/// 배회 중 DetectionRange 내 적 발견 시 Chase로 전환하고,
/// 적이 범위를 벗어나면 다시 Wander로 복귀한다.
/// </summary>
public class MonsterLeaderAI : StateMachine<Monster, MonsterTrigger>, IMonsterBehavior
{
    public SpatialGrid<IUnit> UnitGrid { get; }

    private readonly MonsterWanderState wander = new();
    private readonly MonsterChaseState  chase  = new();
    private readonly MonsterAttackState attack = new();

    private Vector2 wanderDirection;
    private float   wanderTimer;

    public float WanderChangeInterval = 3f;

    protected override State<Monster, MonsterTrigger> InitialState => wander;

    protected override State<Monster, MonsterTrigger>[] States
        => new State<Monster, MonsterTrigger>[] { wander, chase, attack };

    protected override StateTransition<Monster, MonsterTrigger>[] Transitions => new[]
    {
        StateTransition<Monster, MonsterTrigger>.Generate(wander, chase,  MonsterTrigger.DetectEnemy),
        StateTransition<Monster, MonsterTrigger>.Generate(chase,  wander, MonsterTrigger.LoseEnemy),
        StateTransition<Monster, MonsterTrigger>.Generate(chase,  attack, MonsterTrigger.InAttackRange),
        StateTransition<Monster, MonsterTrigger>.Generate(attack, chase,  MonsterTrigger.OutOfAttackRange),
    };

    public MonsterLeaderAI(Monster owner, SpatialGrid<IUnit> unitGrid) : base(owner)
    {
        UnitGrid = unitGrid;
    }

    void IMonsterBehavior.SetUp()
    {
        SetUp();
        PickNewWanderDirection();
    }

    void IMonsterBehavior.Update() => Update();

    public new void Update()
    {
        var pos = (Vector2)Owner.Transform.position;

        switch (CurrentState)
        {
            case MonsterWanderState _:
                wanderTimer -= Time.deltaTime;
                if (wanderTimer <= 0f)
                    PickNewWanderDirection();
                Owner.Move(wanderDirection);
                if (HasEnemyInRange(pos, Owner.Combat.DetectionRange))
                    ExecuteCommand(MonsterTrigger.DetectEnemy);
                break;

            case MonsterChaseState _:
                if (!HasEnemyInRange(pos, Owner.Combat.DetectionRange))
                {
                    Owner.Move(Vector2.zero);
                    ExecuteCommand(MonsterTrigger.LoseEnemy);
                }
                else if (HasEnemyInRange(pos, Owner.Combat.AttackRange))
                {
                    ExecuteCommand(MonsterTrigger.InAttackRange);
                }
                else
                {
                    var target = FindClosestEnemy(pos, Owner.Combat.DetectionRange);
                    if (target != null)
                        Owner.Move(((Vector2)target.Transform.position - pos).normalized);
                }
                break;

            case MonsterAttackState _:
                if (!HasEnemyInRange(pos, Owner.Combat.AttackRange))
                    ExecuteCommand(MonsterTrigger.OutOfAttackRange);
                else if (Owner.Combat.CanAttack)
                    Owner.Combat.ResetCooldown();
                break;
        }
    }

    private void PickNewWanderDirection()
    {
        wanderDirection = Random.insideUnitCircle.normalized;
        wanderTimer     = WanderChangeInterval;
    }

    private bool HasEnemyInRange(Vector2 pos, float range)
    {
        if (UnitGrid == null) return false;
        foreach (var u in UnitGrid.Query(pos, range))
            if (u.Team != Owner.Team && u.IsAlive) return true;
        return false;
    }

    private IUnit FindClosestEnemy(Vector2 pos, float range)
    {
        if (UnitGrid == null) return null;
        IUnit closest = null;
        float minDist = float.MaxValue;
        foreach (var u in UnitGrid.Query(pos, range))
        {
            if (u.Team == Owner.Team || !u.IsAlive) continue;
            float d = Vector2.Distance(pos, u.Transform.position);
            if (d < minDist) { minDist = d; closest = u; }
        }
        return closest;
    }
}
