using UnityEngine;

public enum UnitTeam { Player, Enemy }

public interface IUnit
{
    UnitTeam Team { get; }
    Transform Transform { get; }
    UnitHealth Health { get; }
    UnitCombat Combat { get; }
    bool IsAlive { get; }
    float Radius { get; }
}
