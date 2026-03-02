using FiniteStateMachine;

public enum BossTrigger { Detected, PatternReady, PatternComplete, Die }

/// <summary>
/// 보스 FSM. Idle → Chase ↔ PatternCast → Dead 상태 전이를 관리한다.
/// BossMonster 생성 후 SetUp()을 반드시 호출해야 한다.
/// </summary>
public class BossFSM : StateMachine<BossMonster, BossTrigger>
{
    public SpatialGrid<IUnit> UnitGrid      { get; }
    public ObstacleGrid       ObstacleGrid  { get; }
    public EntitySpawner      EntitySpawner { get; }

    public readonly BossChaseState       ChaseState;
    public readonly BossPatternCastState CastState;

    private readonly BossIdleState idle = new();
    private readonly BossDeadState dead = new();

    public BossFSM(BossMonster owner, SpatialGrid<IUnit> unitGrid, ObstacleGrid obstacleGrid,
                   EntitySpawner entitySpawner) : base(owner)
    {
        UnitGrid      = unitGrid;
        ObstacleGrid  = obstacleGrid;
        EntitySpawner = entitySpawner;
        ChaseState    = new BossChaseState();
        CastState     = new BossPatternCastState();
    }

    protected override State<BossMonster, BossTrigger> InitialState => idle;

    protected override State<BossMonster, BossTrigger>[] States
        => new State<BossMonster, BossTrigger>[] { idle, ChaseState, CastState, dead };

    protected override StateTransition<BossMonster, BossTrigger>[] Transitions => new[]
    {
        StateTransition<BossMonster, BossTrigger>.Generate(idle,       ChaseState, BossTrigger.Detected),
        StateTransition<BossMonster, BossTrigger>.Generate(ChaseState, CastState,  BossTrigger.PatternReady),
        StateTransition<BossMonster, BossTrigger>.Generate(ChaseState, dead,       BossTrigger.Die),
        StateTransition<BossMonster, BossTrigger>.Generate(CastState,  ChaseState, BossTrigger.PatternComplete),
        StateTransition<BossMonster, BossTrigger>.Generate(CastState,  dead,       BossTrigger.Die),
    };
}
