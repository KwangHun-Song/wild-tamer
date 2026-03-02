using UnityEngine;

[CreateAssetMenu(menuName = "Data/MonsterData")]
public class MonsterData : ScriptableObject
{
    public string id;
    public string displayName;
    public MonsterGrade grade;
    public int maxHp;
    public int attackDamage;
    public float attackRange;
    public float attackCooldown;
    public float detectionRange;
    public float moveSpeed;
    public float squadMoveSpeed;
    public float tamingChance;
    public GameObject prefab;
    public GameObject squadPrefab;
    public FlockSettingsData flockSettings;
    public BossPattern[] bossPatterns;
}

public enum MonsterGrade { Normal, Boss }
