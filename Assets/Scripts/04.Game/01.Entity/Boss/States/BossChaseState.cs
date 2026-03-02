using System.Collections.Generic;
using FiniteStateMachine;
using UnityEngine;

/// <summary>
/// 보스 추적 상태. 플레이어를 추적하며 패턴 쿨다운을 관리한다.
/// 쿨다운이 끝난 패턴 중 하나를 무작위 선택 후 CastState에 전달하고 PatternReady 발화.
/// </summary>
public class BossChaseState : State<BossMonster, BossTrigger>
{
    private SpatialGrid<IUnit> unitGrid;
    private ObstacleGrid       obstacleGrid;
    private float[]            cooldowns;

    private readonly Dictionary<BossPatternType, IBossPattern> handlers = new()
    {
        { BossPatternType.TrackingZone,      new TrackingZonePattern()      },
        { BossPatternType.Charge,            new ChargePattern()            },
        { BossPatternType.CrossZone,         new CrossZonePattern()         },
        { BossPatternType.XZone,             new XZonePattern()             },
        { BossPatternType.CurseMark,         new CurseMarkPattern()         },
        { BossPatternType.ProjectileBarrage, new ProjectileBarragePattern() },
    };

    protected override void OnSetUp()
    {
        var fsm      = (BossFSM)StateMachine;
        unitGrid     = fsm.UnitGrid;
        obstacleGrid = fsm.ObstacleGrid;
        handlers[BossPatternType.SummonMinions] = new SummonMinionsPattern(fsm.EntitySpawner, fsm.ObstacleGrid);
        cooldowns = new float[Owner.BossData.patterns.Length];
    }

    public override void OnEnter() => Owner.View.PlayMoveAnimation();

    public override void OnUpdate()
    {
        MoveTowardPlayer();

        for (int i = 0; i < cooldowns.Length; i++)
            if (cooldowns[i] > 0f) cooldowns[i] -= Time.deltaTime;

        if (!Owner.IsAlive)
        {
            StateMachine.ExecuteCommand(BossTrigger.Die);
            return;
        }

        TryFirePattern();
    }

    public override void OnExit() => Owner.View.Movement.Move(Vector2.zero);

    private void TryFirePattern()
    {
        var patterns = Owner.BossData.patterns;
        var ready    = new List<int>();
        for (int i = 0; i < patterns.Length; i++)
            if (cooldowns[i] <= 0f) ready.Add(i);

        if (ready.Count == 0) return;

        int idx      = ready[Random.Range(0, ready.Count)];
        var selected = patterns[idx];
        float mult   = Owner.IsEnraged ? Owner.BossData.enrageCooldownMultiplier : 1f;
        cooldowns[idx] = selected.cooldown * mult;

        var handler = handlers.TryGetValue(selected.type, out var h) ? h : null;
        ((BossFSM)StateMachine).CastState.SetPattern(selected, handler);
        StateMachine.ExecuteCommand(BossTrigger.PatternReady);
    }

    private void MoveTowardPlayer()
    {
        var pos    = (Vector2)Owner.Transform.position;
        var target = BossPatternUtils.FindNearestEnemy(Owner);

        if (target == null)
        {
            Owner.View.Movement.Move(Vector2.zero);
            return;
        }

        var dir      = ((Vector2)target.Transform.position - pos).normalized;
        var resolved = ResolveDirection(pos, dir);
        Owner.View.Movement.Move(resolved);
        if (resolved.magnitude > 0.01f)
            Owner.View.UpdateFacing(resolved);
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
