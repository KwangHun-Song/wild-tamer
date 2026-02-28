using System.Collections;
using Base;
using UnityEngine;

public class HitStop : IOnHitListener
{
    private readonly float duration;
    private readonly Notifier notifier;
    private bool isActive;

    public HitStop(float duration, Notifier notifier)
    {
        this.duration = duration;
        this.notifier = notifier;
        this.isActive = false;
        notifier.Subscribe(this);
    }

    public void Dispose()
    {
        notifier.Unsubscribe(this);
    }

    public void OnHit(IUnit attacker, IUnit target, int damage)
    {
        if (isActive)
        {
            return;
        }

        Facade.Coroutine.StartCoroutine(ApplyHitStop());
    }

    private IEnumerator ApplyHitStop()
    {
        isActive = true;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
        isActive = false;
    }
}
