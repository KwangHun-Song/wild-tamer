using UnityEngine;
using Base;

/// <summary>
/// P6 투사체 MonoBehaviour.
/// Initialize() 호출 후 스스로 이동하며, 최대 거리 도달 또는 적 충돌 시 Destroy된다.
/// </summary>
public class BossProjectile : MonoBehaviour
{
    private int     damage;
    private float   maxDistance;
    private float   speed;
    private Vector2 direction;
    private Vector2 startPos;
    private Notifier notifier;
    private IUnit    owner;

    public void Initialize(IUnit owner, Vector2 dir, BossPatternData data, Notifier notifier)
    {
        this.owner       = owner;
        this.damage      = data.damage;
        this.maxDistance = data.maxDistance;
        this.speed       = data.projectileSpeed;
        this.direction   = dir.normalized;
        this.notifier    = notifier;
        startPos         = transform.position;
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        if (Vector2.Distance(startPos, transform.position) >= maxDistance)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<CharacterView>(out var cv)) return;

        var unit = cv.UnitOwner;
        if (unit == null || unit.Team == owner.Team || !unit.IsAlive) return;

        DamageProcessor.ProcessDamage(owner, unit, damage, notifier);
        Destroy(gameObject);
    }
}
