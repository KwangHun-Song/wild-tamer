using UnityEngine;

[CreateAssetMenu(menuName = "Data/BossPatternData")]
public class BossPatternData : ScriptableObject
{
    public BossPatternType type;

    [Header("타이밍")]
    public float warningDuration;
    public float activeDuration;
    public float cooldown;

    [Header("데미지")]
    public int damage;

    [Header("P1/P3/P4/P5 — 장판 범위")]
    public float range;
    public float width = 1f;

    [Header("P2 — 돌진")]
    public float chargeDistance = 7f;
    public float chargeWidth    = 1.5f;
    public float chargeSpeed    = 12f;

    [Header("P6 — 투사체")]
    public float projectileSpeed = 3f;
    public int   projectileCount = 3;
    public float spreadAngle     = 15f;
    public float fireInterval    = 0.5f;
    public float maxDistance     = 8f;

    [Header("P7 — 소환")]
    public int        summonCount  = 3;
    public MonsterData summonData;
    public float      summonRadius = 2f;
}

public enum BossPatternType
{
    TrackingZone,
    Charge,
    CrossZone,
    XZone,
    CurseMark,
    ProjectileBarrage,
    SummonMinions,
}
