using Base;

/// <summary>
/// 데미지 계산과 이벤트 발행을 담당하는 순수 정적 클래스.
/// TakeDamage 호출 후 IOnHitListener와 IOnUnitDeathListener를 발행한다.
/// </summary>
public static class DamageProcessor
{
    public static void ProcessDamage(IUnit attacker, IUnit target, Notifier notifier)
    {
        ProcessDamage(attacker, target, attacker.Combat.AttackDamage, notifier);
    }

    public static void ProcessDamage(IUnit attacker, IUnit target, int damage, Notifier notifier)
    {
        target.Health.TakeDamage(damage);
        attacker.Combat.ResetCooldown();

        notifier.Notify<IOnHitListener>(l => l.OnHit(attacker, target, damage));

        if (!target.IsAlive)
            notifier.Notify<IOnUnitDeathListener>(l => l.OnUnitDeath(target, attacker));
    }
}
