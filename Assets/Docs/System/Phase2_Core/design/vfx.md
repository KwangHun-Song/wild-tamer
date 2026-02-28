# 2.7 전투 연출 (VFX)

> 상위 문서: [Phase 2 설계](../design.md)

타격 이벤트를 수신하여 역경직, 카메라 흔들림, 이펙트/사운드를 처리한다. 모두 pure C# 클래스로, `Facade.Coroutine`으로 타이밍을 처리한다.

---

## HitStop (pure C#)

동시에 여러 히트가 발생해도 `isActive` 플래그로 중첩 코루틴을 방지한다.

```csharp
public class HitStop : IOnHitListener
{
    private readonly float duration;
    private readonly Notifier notifier;
    private bool isActive;

    public HitStop(float duration, Notifier notifier)
    {
        this.duration = duration;
        this.notifier = notifier;
        notifier.Subscribe(this);
    }

    public void Dispose() => notifier.Unsubscribe(this);

    public void OnHit(IUnit attacker, IUnit target, int damage)
    {
        if (isActive) return;       // 진행 중이면 무시
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
```

---

## CameraShake (pure C#)

히트 이벤트 수신 시 카메라 Transform을 흔든다.

```csharp
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
        this.duration  = duration;
        this.notifier  = notifier;
        notifier.Subscribe(this);
    }

    public void Dispose() => notifier.Unsubscribe(this);

    public void OnHit(IUnit attacker, IUnit target, int damage)
        => Facade.Coroutine.StartCoroutine(Shake());

    private IEnumerator Shake() { ... }
}
```

---

## HitEffectPlayer (pure C#)

히트 이벤트 수신 시 타격 이펙트를 스폰하고 사운드를 재생한다.

```csharp
public class HitEffectPlayer : IOnHitListener
{
    private readonly GameObject effectPrefab;
    private readonly string sfxName;
    private readonly Notifier notifier;

    public HitEffectPlayer(GameObject effectPrefab, string sfxName, Notifier notifier)
    {
        this.effectPrefab = effectPrefab;
        this.sfxName  = sfxName;
        this.notifier = notifier;
        notifier.Subscribe(this);
    }

    public void Dispose() => notifier.Unsubscribe(this);

    public void OnHit(IUnit attacker, IUnit target, int damage)
    {
        Facade.Pool.Spawn(effectPrefab, target.Transform.position);
        Facade.Sound.PlaySFX(sfxName);
    }
}
```
