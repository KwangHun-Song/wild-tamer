using FiniteStateMachine;
using UnityEngine;

public enum MonsterTrigger
{
    DetectEnemy,
    LoseEnemy,
    InAttackRange,
    OutOfAttackRange
}

/// <summary>
/// Idle → Chase → Attack 상태 기계. StateMachine.Update()는 TryTransition만 실행하므로
/// 트리거 기반 전이와 퍼프레임 이동 로직을 Update()를 숨겨서(new) 직접 처리한다.
/// </summary>
public class MonsterAI : StateMachine<Monster, MonsterTrigger>
{
    public SpatialGrid<IUnit> UnitGrid { get; }

    private readonly MonsterIdleState idle = new();
    private readonly MonsterChaseState chase = new();
    private readonly MonsterAttackState attack = new();

    protected override State<Monster, MonsterTrigger> InitialState => idle;

    protected override State<Monster, MonsterTrigger>[] States
        => new State<Monster, MonsterTrigger>[] { idle, chase, attack };

    protected override StateTransition<Monster, MonsterTrigger>[] Transitions => new[]
    {
        StateTransition<Monster, MonsterTrigger>.Generate(idle,   chase,  MonsterTrigger.DetectEnemy),
        StateTransition<Monster, MonsterTrigger>.Generate(chase,  idle,   MonsterTrigger.LoseEnemy),
        StateTransition<Monster, MonsterTrigger>.Generate(chase,  attack, MonsterTrigger.InAttackRange),
        StateTransition<Monster, MonsterTrigger>.Generate(attack, chase,  MonsterTrigger.OutOfAttackRange),
    };

    public MonsterAI(Monster owner, SpatialGrid<IUnit> unitGrid) : base(owner)
    {
        UnitGrid = unitGrid;
    }

    /// <summary>Monster.Update()에서 매 프레임 호출. 상태 전이 판정과 이동을 처리한다.</summary>
    public new void Update()
    {
        var pos = (Vector2)Owner.Transform.position;

        switch (CurrentState)
        {
            case MonsterIdleState _:
                if (HasEnemyInRange(pos, Owner.Combat.DetectionRange))
                    ExecuteCommand(MonsterTrigger.DetectEnemy);
                break;

            case MonsterChaseState _:
                if (!HasEnemyInRange(pos, Owner.Combat.DetectionRange))
                {
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
                    {
                        var dir = ((Vector2)target.Transform.position - pos).normalized;
                        Owner.Move(dir);
                    }
                }
                break;

            case MonsterAttackState _:
                if (!HasEnemyInRange(pos, Owner.Combat.AttackRange))
                    ExecuteCommand(MonsterTrigger.OutOfAttackRange);
                else if (Owner.Combat.CanAttack)
                    Owner.Combat.ResetCooldown(); // DamageProcessor는 Step 8에서 연결
                break;
        }
    }

    private bool HasEnemyInRange(Vector2 pos, float range)
    {
        if (UnitGrid == null) return false;
        var units = UnitGrid.Query(pos, range);
        foreach (var u in units)
        {
            if (u.Team != Owner.Team && u.IsAlive) return true;
        }
        return false;
    }

    private IUnit FindClosestEnemy(Vector2 pos, float range)
    {
        if (UnitGrid == null) return null;
        var units = UnitGrid.Query(pos, range);
        IUnit closest = null;
        var minDist = float.MaxValue;
        foreach (var u in units)
        {
            if (u.Team == Owner.Team || !u.IsAlive) continue;
            var dist = Vector2.Distance(pos, u.Transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = u;
            }
        }
        return closest;
    }
}
