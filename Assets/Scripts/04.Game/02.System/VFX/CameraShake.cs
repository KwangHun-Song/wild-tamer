using System.Collections;
using Base;
using UnityEngine;

public class CameraShake : IOnHitListener
{
    private readonly QuarterViewCamera camera;
    private readonly float intensity;
    private readonly float duration;
    private readonly Notifier notifier;
    private bool isShaking;

    public CameraShake(QuarterViewCamera camera, float intensity, float duration, Notifier notifier)
    {
        this.camera = camera;
        this.intensity = intensity;
        this.duration = duration;
        this.notifier = notifier;
        notifier.Subscribe(this);
    }

    public void Dispose()
    {
        notifier.Unsubscribe(this);
    }

    public void OnHit(IUnit attacker, IUnit target, int damage)
    {
        if (isShaking) return;
        Facade.Coroutine.StartCoroutine(Shake());
    }

    private IEnumerator Shake()
    {
        isShaking = true;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            camera.ShakeOffset = (Vector3)(Random.insideUnitCircle * intensity);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        camera.ShakeOffset = Vector3.zero;
        isShaking = false;
    }
}
