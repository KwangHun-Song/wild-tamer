using System;
using UnityEngine;

public class UnitHealth : MonoBehaviour
{
    public int MaxHp { get; private set; }
    public int CurrentHp { get; private set; }
    public bool IsAlive => CurrentHp > 0;

    public event Action<int> OnDamaged;
    public event Action OnDeath;

    public void Initialize(int maxHp)
    {
        MaxHp = maxHp;
        CurrentHp = maxHp;
    }

    public void TakeDamage(int damage)
    {
        if (!IsAlive || damage <= 0) return;

        CurrentHp = Mathf.Max(0, CurrentHp - damage);
        OnDamaged?.Invoke(damage);

        if (!IsAlive)
            OnDeath?.Invoke();
    }
}
