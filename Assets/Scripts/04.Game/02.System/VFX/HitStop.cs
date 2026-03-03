using System.Collections;
using Base;
using UnityEngine;

public class HitStop : IOnHitListener
{
    private readonly float duration;
    private readonly Notifier notifier;
    private bool isActive;
    private readonly WaitForSecondsRealtime waitRealtime;

    public HitStop(float duration, Notifier notifier)
    {
        this.duration = duration;
        this.notifier = notifier;
        this.isActive = false;
        this.waitRealtime = new WaitForSecondsRealtime(duration);
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
        var previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        yield return waitRealtime;
        Time.timeScale = previousTimeScale;
        isActive = false;
    }
}
