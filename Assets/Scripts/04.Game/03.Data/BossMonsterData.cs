using UnityEngine;

[CreateAssetMenu(menuName = "Data/BossMonsterData")]
public class BossMonsterData : ScriptableObject
{
    [Header("기본 정보")]
    public string id;
    public string displayName;
    public Sprite icon;

    [Header("스탯")]
    public int   maxHp          = 600;
    public float moveSpeed      = 1.5f;
    public int   attackDamage   = 10;
    public float attackRange    = 1.2f;
    [Tooltip("맵 전체를 커버하도록 충분히 크게 설정 권장 (예: 999)")]
    public float detectionRange = 999f;
    public float radius         = 0.5f;
    public float attackCooldown = 1.5f;

    [Header("패턴")]
    public BossPatternData[] patterns;
    [Tooltip("패턴 사이 휴지 시간(초)")]
    public float patternInterval = 1.5f;

    [Header("인레이지 (HP 임계값 이하)")]
    [Range(0f, 1f)] public float enrageThreshold          = 0.5f;
    public float                 enrageCooldownMultiplier  = 0.8f;
    public float                 enrageSpeedMultiplier     = 1.3f;

    [Header("프리팹")]
    public GameObject viewPrefab;
}
