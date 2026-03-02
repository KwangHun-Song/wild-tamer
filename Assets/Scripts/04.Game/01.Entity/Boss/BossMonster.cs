using UnityEngine;
using Base;

/// <summary>
/// 보스 몬스터. Monster를 상속하며 BossFSM이 이동·패턴·사망을 모두 담당한다.
/// base Monster의 MonsterLeaderFSM 대신 BossFSM을 단독으로 사용하므로
/// MonsterRole.Follower (fsm=null) 로 기반 클래스를 초기화한다.
/// </summary>
public class BossMonster : Monster
{
    public BossMonsterData    BossData  { get; }
    public BossMonsterView    BossView  { get; }
    public new SpatialGrid<IUnit> UnitGrid { get; }
    public Notifier           Notifier  { get; }
    public bool               IsEnraged { get; private set; }

    public readonly BossFSM BossFSM;
    private readonly float originalSpeed;

    public BossMonster(BossMonsterView view,
                       BossMonsterData data,
                       SpatialGrid<IUnit> unitGrid,
                       ObstacleGrid obstacleGrid,
                       EntitySpawner entitySpawner,
                       Notifier notifier)
        : base(view, BuildMonsterData(data), unitGrid, MonsterRole.Follower, obstacleGrid)
    {
        BossData      = data;
        BossView      = view;
        UnitGrid      = unitGrid;
        Notifier      = notifier;
        originalSpeed = data.moveSpeed;

        BossFSM = new BossFSM(this, unitGrid, obstacleGrid, entitySpawner);
        BossFSM.SetUp();

        Health.OnDeath += OnBossDeath;
    }

    /// <summary>BossFSM이 모든 로직을 담당하므로 base.Update() 호출 없음.</summary>
    public new void Update()
    {
        BossFSM.Update();
        CheckEnrage();
    }

    public new void Cleanup()
    {
        Health.OnDeath -= OnBossDeath;
        base.Cleanup();
    }

    private void CheckEnrage()
    {
        if (IsEnraged) return;
        if (Health.CurrentHp > Health.MaxHp * BossData.enrageThreshold) return;

        IsEnraged = true;
        View.Movement.MoveSpeed = originalSpeed * BossData.enrageSpeedMultiplier;
    }

    private void OnBossDeath()
    {
        BossFSM.ExecuteCommand(BossTrigger.Die);
    }

    /// <summary>
    /// BossMonsterData 필드를 기반으로 Monster 생성자에 주입할 최소 MonsterData를 생성한다.
    /// 실제 AI는 BossFSM이 담당하므로 Combat 수치는 참조용으로만 사용된다.
    /// </summary>
    private static MonsterData BuildMonsterData(BossMonsterData d)
    {
        var md            = ScriptableObject.CreateInstance<MonsterData>();
        md.maxHp          = d.maxHp;
        md.moveSpeed      = d.moveSpeed;
        md.detectionRange = d.detectionRange;
        md.attackDamage   = 0;
        md.attackRange    = 0f;
        md.attackCooldown = float.MaxValue;
        md.radius         = 0.5f;
        return md;
    }
}
