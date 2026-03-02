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
        ResolveOverlaps();
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

    /// <summary>
    /// 등록된 유닛 간 물리 반경 겹침을 해소한다.
    /// 두 유닛의 Radius 합보다 거리가 가까울 경우 각각 절반씩 밀어낸다.
    /// </summary>
    private void ResolveOverlaps()
    {
        for (var i = 0; i < registeredUnits.Count; i++)
        {
            var a = registeredUnits[i];
            if (!a.IsAlive) continue;

            for (var j = i + 1; j < registeredUnits.Count; j++)
            {
                var b = registeredUnits[j];
                if (!b.IsAlive) continue;

                // Player 팀끼리는 Squad.Update()의 정지 로직과 FlockBehavior가 담당하므로 건너뛴다
                if (a.Team == UnitTeam.Player && b.Team == UnitTeam.Player) continue;

                var posA = (Vector2)a.Transform.position;
                var posB = (Vector2)b.Transform.position;
                var diff = posA - posB;
                var dist = diff.magnitude;
                var minDist = a.Radius + b.Radius;

                if (dist >= minDist || dist < 0.001f) continue;

                // 플레이어는 인풋 외 이동 금지 — 상대 유닛만 전체 거리를 밀어낸다
                var pushVec = diff.normalized * (minDist - dist);
                bool aIsPlayer = a is Player;
                bool bIsPlayer = b is Player;

                if (!aIsPlayer)
                    a.Transform.position = (Vector3)(posA + pushVec * (bIsPlayer ? 1f : 0.5f));
                if (!bIsPlayer)
                    b.Transform.position = (Vector3)(posB - pushVec * (aIsPlayer ? 1f : 0.5f));
            }
        }
    }

    private void ProcessCombat()
    {
        // 순회 중 RegisterUnit/UnregisterUnit 호출(테이밍 등)에 의한 컬렉션 변경을 방지하기 위해 스냅샷 복사
        var snapshot = registeredUnits.ToArray();
        foreach (var unit in snapshot)
        {
            if (!unit.IsAlive || !unit.Combat.CanAttack) continue;

            var pos = (Vector2)unit.Transform.position;
            var nearby = unitGrid.Query(pos, unit.Combat.AttackRange);
            foreach (var target in nearby)
            {
                if (target.Team == unit.Team || !target.IsAlive) continue;
                if (Vector2.Distance(pos, (Vector2)target.Transform.position) > unit.Combat.AttackRange) continue;
                var attackDir = ((Vector2)target.Transform.position - pos).normalized;
                (unit as Character)?.View.SetFacingImmediate(attackDir);
                DamageProcessor.ProcessDamage(unit, target, notifier);
                (unit as Character)?.FireAttack();
                (target as Monster)?.NotifyDamagedBy(unit); // 피격 어그로
                break; // 한 틱에 한 대상만 공격
            }
        }
    }
}
