public class UnitCombat
{
    public int AttackDamage { get; set; }
    public float AttackRange { get; set; }
    public float DetectionRange { get; set; }

    private readonly float cooldown;
    private float elapsed;

    public UnitCombat(int attackDamage, float attackRange, float detectionRange, float cooldown)
    {
        AttackDamage = attackDamage;
        AttackRange = attackRange;
        DetectionRange = detectionRange;
        this.cooldown = cooldown;
        elapsed = cooldown; // 시작 시 바로 공격 가능
    }

    public bool CanAttack => elapsed >= cooldown;

    public void ResetCooldown() => elapsed = 0f;

    public void Tick(float deltaTime) => elapsed += deltaTime;
}
