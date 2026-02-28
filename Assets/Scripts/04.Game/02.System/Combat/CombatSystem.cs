using System.Collections.Generic;
using Base;
using UnityEngine;

/// <summary>
/// 등록된 유닛들 사이의 자동 교전을 처리한다.
/// RegisterUnit / UnregisterUnit은 IUnit을 받으므로 Monster, SquadMember, Player 모두 처리 가능하다.
/// </summary>
public class CombatSystem
{
    private readonly SpatialGrid<IUnit> unitGrid;
    private readonly Notifier notifier;
    private readonly List<IUnit> registeredUnits = new();

    /// <summary>MonsterAI 탐지를 위해 읽기 전용으로 unitGrid를 노출한다.</summary>
    public SpatialGrid<IUnit> UnitGrid => unitGrid;

    public CombatSystem(SpatialGrid<IUnit> unitGrid, Notifier notifier)
    {
        this.unitGrid = unitGrid;
        this.notifier = notifier;
    }

    public void RegisterUnit(IUnit unit)
    {
        if (!registeredUnits.Contains(unit))
            registeredUnits.Add(unit);
    }

    public void UnregisterUnit(IUnit unit)
    {
        registeredUnits.Remove(unit);
    }

    public void Update()
    {
        RebuildGrid();
        ProcessCombat();
    }

    private void RebuildGrid()
    {
        unitGrid.Clear();
        foreach (var unit in registeredUnits)
        {
            if (unit.IsAlive)
                unitGrid.Insert(unit, unit.Transform.position);
        }
    }

    private void ProcessCombat()
    {
        foreach (var unit in registeredUnits)
        {
            if (!unit.IsAlive || !unit.Combat.CanAttack) continue;

            var nearby = unitGrid.Query((Vector2)unit.Transform.position, unit.Combat.AttackRange);
            foreach (var target in nearby)
            {
                if (target.Team == unit.Team || !target.IsAlive) continue;
                DamageProcessor.ProcessDamage(unit, target, notifier);
                break; // 한 틱에 한 대상만 공격
            }
        }
    }
}
