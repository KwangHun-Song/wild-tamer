using System;

public class UnitHealth
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

    /// <summary>저장 데이터 복원 전용. CurrentHp를 직접 설정한다.</summary>
    public void SetHp(int hp)
    {
        CurrentHp = Math.Max(0, Math.Min(hp, MaxHp));
    }

    public void TakeDamage(int damage)
    {
        if (!IsAlive || damage <= 0) return;

        CurrentHp = Math.Max(0, CurrentHp - damage);

        OnDamaged?.Invoke(damage);

        if (!IsAlive)
            OnDeath?.Invoke();
    }
}
