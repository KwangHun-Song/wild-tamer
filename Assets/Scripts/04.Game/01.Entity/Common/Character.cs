using System;
using UnityEngine;

public abstract class Character : IUnit
{
    public abstract UnitTeam Team { get; }
    public Transform Transform => View.transform;
    public UnitHealth Health { get; }
    public UnitCombat Combat { get; }
    public bool IsAlive => Health.IsAlive;

    public CharacterView View { get; }

    public event Action OnAttackFired;

    protected Character(CharacterView view, UnitCombat combat)
    {
        View = view;
        Combat = combat;
        Health = new UnitHealth();
    }

    public void FireAttack() => OnAttackFired?.Invoke();

    /// <summary>
    /// View의 위치를 직접 설정한다. 스냅샷 복원 등 외부 제어가 필요한 경우 사용한다.
    /// </summary>
    public void SetPosition(Vector2 position) => View.transform.position = (Vector3)position;
}
