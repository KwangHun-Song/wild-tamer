using Base;
using UnityEngine;

public class HitEffectPlayer : IOnHitListener
{
    private readonly GameObject effectPrefab;
    private readonly string sfxName;
    private readonly Notifier notifier;

    public HitEffectPlayer(GameObject effectPrefab, string sfxName, Notifier notifier)
    {
        this.effectPrefab = effectPrefab;
        this.sfxName = sfxName;
        this.notifier = notifier;
        notifier.Subscribe(this);
    }

    public void Dispose()
    {
        notifier.Unsubscribe(this);
    }

    public void OnHit(IUnit attacker, IUnit target, int damage)
    {
        Facade.Pool.Spawn(effectPrefab).transform.position = target.Transform.position;
        Facade.Sound.PlaySFX(sfxName);
    }
}
