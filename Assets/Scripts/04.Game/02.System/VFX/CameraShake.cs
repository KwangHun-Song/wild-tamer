using System.Collections;
using Base;
using UnityEngine;

public class CameraShake : IOnHitListener
{
    private readonly QuarterViewCamera camera;
    private readonly IUnit             player;
    private readonly float             intensity;
    private readonly float             duration;
    private readonly float             bossIntensity;
    private readonly float             bossDuration;
    private readonly Notifier          notifier;
    private Coroutine                  currentShake;

    public CameraShake(QuarterViewCamera camera, IUnit player, float intensity, float duration,
                       Notifier notifier, float bossIntensity = 0f, float bossDuration = 0f)
    {
        this.camera        = camera;
        this.player        = player;
        this.intensity     = intensity;
        this.duration      = duration;
        this.bossIntensity = bossIntensity > 0f ? bossIntensity : intensity * 3f;
        this.bossDuration  = bossDuration  > 0f ? bossDuration  : duration  * 2f;
        this.notifier      = notifier;
        notifier.Subscribe(this);
    }

    public void Dispose()
    {
        notifier.Unsubscribe(this);
    }

    public void OnHit(IUnit attacker, IUnit target, int damage)
    {
        if (target != player) return;

        bool isBossHit = attacker is BossMonster;

        // 보스 피격은 진행 중인 셰이크를 덮어쓰고, 일반 피격은 셰이크 중이면 무시
        if (!isBossHit && currentShake != null) return;

        if (currentShake != null)
            Facade.Coroutine.StopCoroutine(currentShake);

        float useIntensity = isBossHit ? bossIntensity : intensity;
        float useDuration  = isBossHit ? bossDuration  : duration;
        currentShake = Facade.Coroutine.StartCoroutine(Shake(useIntensity, useDuration));
    }

    private IEnumerator Shake(float shakeIntensity, float shakeDuration)
    {
        var elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            camera.ShakeOffset = (Vector3)(Random.insideUnitCircle * shakeIntensity);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        camera.ShakeOffset = Vector3.zero;
        currentShake = null;
    }
}
