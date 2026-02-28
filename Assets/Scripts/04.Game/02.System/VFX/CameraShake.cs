using System.Collections;
using Base;
using UnityEngine;

public class CameraShake : IOnHitListener
{
    private readonly Transform cameraTransform;
    private readonly float intensity;
    private readonly float duration;
    private readonly Notifier notifier;

    public CameraShake(Transform cameraTransform, float intensity, float duration, Notifier notifier)
    {
        this.cameraTransform = cameraTransform;
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
        Facade.Coroutine.StartCoroutine(Shake());
    }

    private IEnumerator Shake()
    {
        var elapsed = 0f;
        var origin = cameraTransform.position;

        while (elapsed < duration)
        {
            cameraTransform.position = origin + (Vector3)(Random.insideUnitCircle * intensity);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        cameraTransform.position = origin;
    }
}
