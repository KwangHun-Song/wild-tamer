# 보스 시스템 설계

> 상위 문서: [Phase 2 설계](../design.md)
> 컨셉 문서: [boss_monster.md](boss_monster.md)

보스 스폰 타이밍, 패턴 실행, HP 바 UI, 경고 연출을 포함한 기술 설계.

---

## 파일 구조

```
Assets/Scripts/04.Game/
├── 01.Entity/Boss/
│   ├── BossMonster.cs              # pure C# — Monster 상속
│   ├── BossMonsterView.cs          # MonoBehaviour — MonsterView 상속
│   ├── BossFSM.cs                  # pure C# — StateMachine<BossMonster, BossTrigger>
│   ├── States/
│   │   ├── BossIdleState.cs        # 스폰 직후 대기
│   │   ├── BossChaseState.cs       # 추적 이동 + 패턴 쿨다운 관리
│   │   ├── BossPatternCastState.cs # Warning → Active 타이머
│   │   └── BossDeadState.cs        # 사망 연출
│   └── Patterns/
│       ├── IBossPattern.cs         # 패턴 인터페이스
│       ├── TrackingZonePattern.cs  # P1
│       ├── ChargePattern.cs        # P2
│       ├── CrossZonePattern.cs     # P3
│       ├── XZonePattern.cs         # P4
│       ├── CurseMarkPattern.cs     # P5
│       ├── ProjectileBarragePattern.cs # P6
│       └── SummonMinionsPattern.cs # P7
├── 02.System/Boss/
│   └── BossSpawnSystem.cs          # pure C# — 타이머 + 스폰 관리
└── 03.UI/Boss/
    ├── BossHpBarView.cs            # MonoBehaviour — PlayPage 상단 HP 바
    └── BossWarningView.cs          # MonoBehaviour — 스폰 경고 UI

Assets/ScriptableObjects/
├── BossMonsterData.asset           # YellowMonk 데이터
├── BossMonsterData_Pawn.asset      # YellowPawn 데이터
└── Patterns/
    ├── P1_TrackingZone.asset
    ├── P2_Charge.asset
    └── ...

Assets/Prefabs/Boss/
├── BossIndicators/
│   ├── CircleIndicator.prefab      # P1, P5
│   ├── CrossIndicator.prefab       # P3
│   ├── XIndicator.prefab           # P4
│   └── LineIndicator.prefab        # P2 돌진 예고
├── Projectile.prefab               # P6 투사체
├── YellowMonkBossView.prefab
└── YellowPawnBossView.prefab
```

---

## BossMonsterData (ScriptableObject)

보스 1종당 1개의 에셋. 모든 수치를 인스펙터에서 조정 가능.

```csharp
[CreateAssetMenu(menuName = "Data/BossMonsterData")]
public class BossMonsterData : ScriptableObject
{
    [Header("기본 정보")]
    public string id;
    public string displayName;
    public Sprite icon;

    [Header("스탯")]
    public int maxHp;
    public float moveSpeed;
    public int attackDamage;
    public float attackRange;
    public float detectionRange;
    public float radius;
    public float attackCooldown;

    [Header("패턴")]
    public BossPatternData[] patterns;
    [Tooltip("패턴 사이 휴지 시간(초)")]
    public float patternInterval = 1.5f;

    [Header("인레이지 (HP 임계값 이하)")]
    [Range(0f, 1f)] public float enrageThreshold = 0.5f;
    public float enrageCooldownMultiplier = 0.8f;
    public float enrageSpeedMultiplier = 1.0f;

    [Header("프리팹")]
    public GameObject viewPrefab;
}
```

---

## BossPatternData (ScriptableObject)

패턴 1개당 1개의 에셋. 모든 패턴이 공유하는 공통 필드 + 패턴별 선택 사용 필드.

```csharp
[CreateAssetMenu(menuName = "Data/BossPatternData")]
public class BossPatternData : ScriptableObject
{
    public BossPatternType type;

    [Header("타이밍")]
    public float warningDuration;    // 예고(인디케이터) 표시 시간
    public float activeDuration;     // 데미지 판정 지속 시간
    public float cooldown;

    [Header("데미지")]
    public int damage;

    [Header("P1/P3/P4/P5 — 장판 범위")]
    public float range;              // 반지름(원형) 또는 팔 길이(십자/X)
    public float width;              // 십자/X 팔 폭

    [Header("P2 — 돌진")]
    public float chargeDistance;
    public float chargeWidth;
    public float chargeSpeed;

    [Header("P6 — 투사체")]
    public float projectileSpeed;
    public int   projectileCount;    // 연속 발사 수
    public float spreadAngle;        // 좌우 퍼짐 각도
    public float fireInterval;       // 발사 간격(초)
    public float maxDistance;        // 최대 비행 거리

    [Header("P7 — 소환")]
    public int        summonCount  = 3;   // 소환할 몬스터 수
    public MonsterData summonData;        // 소환할 몬스터 종류
    public float      summonRadius = 2f;  // 보스 주변 소환 반경 (타일)
}

public enum BossPatternType
{
    TrackingZone,
    Charge,
    CrossZone,
    XZone,
    CurseMark,
    ProjectileBarrage,
    SummonMinions,     // P7
}
```

---

## BossMonster (pure C#)

`Monster`를 상속. `BossFSM`을 소유하며 이동·추적·패턴 실행을 모두 FSM으로 처리한다.
`MonsterLeaderFSM`을 사용하지 않고 보스 전용 상태 머신으로 대체한다.

```csharp
public class BossMonster : Monster
{
    public BossMonsterData BossData { get; }
    public BossMonsterView BossView { get; }
    public bool IsEnraged { get; private set; }

    private readonly BossFSM bossFSM;

    public BossMonster(BossMonsterView view,
                       BossMonsterData data,
                       SpatialGrid<IUnit> unitGrid,
                       ObstacleGrid obstacleGrid)
        : base(view, BuildMonsterData(data), unitGrid, MonsterRole.Leader, obstacleGrid)
    {
        BossData = data;
        BossView = view;
        bossFSM  = new BossFSM(this, view);
    }

    /// <summary>
    /// BossFSM이 이동·추적·패턴 실행을 모두 담당하므로 base.Update() 호출 없음.
    /// </summary>
    public new void Update()
    {
        bossFSM.Update(Time.deltaTime);
        CheckEnrage();
    }

    private void CheckEnrage()
    {
        if (IsEnraged || Health.CurrentHp > Health.MaxHp * BossData.enrageThreshold) return;
        IsEnraged = true;
        View.Movement.MoveSpeed *= BossData.enrageSpeedMultiplier;
    }

    /// <summary>BossMonsterData → MonsterData 변환 (Monster 생성자 주입용).</summary>
    private static MonsterData BuildMonsterData(BossMonsterData d) { ... }
}
```

---

## BossFSM (pure C#)

`StateMachine<BossMonster, BossTrigger>` 기반. 이동·추적·패턴 실행을 4개 상태로 관리한다.
보스는 `detectionRange`를 맵 크기보다 크게 설정(예: `999f`)하여 스폰 직후 항상 플레이어를 추적한다.

### BossTrigger

```csharp
public enum BossTrigger { Detected, PatternReady, PatternComplete, Die }
```

### 상태 전이

```
BossIdleState ──(Detected)──────────→ BossChaseState
                                              │
                          (PatternReady)──────┘
                                ↓
                       BossPatternCastState
                                │
                    (PatternComplete)──────→ BossChaseState
                                │
                              (Die)──→ BossDeadState
BossChaseState ──(Die)──→ BossDeadState
```

### 상태별 역할

| 상태 | OnEnter | OnUpdate |
|------|---------|---------|
| `BossIdleState` | PlayIdleAnimation | 적 감지 시 `Detected` fire |
| `BossChaseState` | PlayMoveAnimation | A* 추적 이동, 패턴 쿨다운 틱, 준비된 패턴 있으면 `PatternReady` fire |
| `BossPatternCastState` | 이동 정지, 경고 인디케이터 표시 | Warning→Active 타이머 처리, 완료 시 `PatternComplete` fire |
| `BossDeadState` | PlayDeathSequence | — |

### BossChaseState — 쿨다운 관리

패턴 쿨다운 배열 `float[] patternCooldowns`를 내부에 보유하고 매 프레임 틱한다.
준비된 패턴이 있으면 `StateMachine.Fire(BossTrigger.PatternReady)`를 호출하고
선택된 `BossPatternData`를 `BossPatternCastState`에 전달한다.

**감지 범위**: `BossMonsterData.detectionRange`를 맵 크기 이상(예: `999f`)으로 설정하면
`BossIdleState.OnUpdate()`에서 즉시 `Detected`가 발화되어 항상 플레이어를 추적한다.

### BossPatternCastState — Warning → Active 타이머

기존 `BossPatternExecutor`의 Warning·Active 단계를 FSM State로 대체한다.
(Interval 단계 = `BossChaseState`의 쿨다운 틱으로 대체)

```csharp
public class BossPatternCastState : State<BossMonster, BossTrigger>
{
    private BossPatternData pattern;
    private IBossPattern    handler;
    private Vector2         lockedTarget;
    private float           phaseTimer;
    private bool            isWarning = true;
    private bool            chargeComplete;

    public void SetPattern(BossPatternData selected, IBossPattern h)
    {
        pattern  = selected;
        handler  = h;
        isWarning = true;
        chargeComplete = false;
        lockedTarget = Owner.Transform.position;
    }

    public override void OnUpdate()
    {
        phaseTimer -= Time.deltaTime;

        if (isWarning)
        {
            handler.OnWarningTick(Owner, pattern, ref lockedTarget);
            if (phaseTimer <= 0f)
            {
                isWarning  = false;
                phaseTimer = pattern.activeDuration;
                ActivatePattern();
            }
        }
        else
        {
            if (phaseTimer <= 0f || chargeComplete)
                Machine.Fire(BossTrigger.PatternComplete);
        }
    }

    private void ActivatePattern() { ... }   // FlashIndicator + IBossPattern.Activate()
    public void NotifyChargeComplete() => chargeComplete = true;  // P2 콜백용
}
```

### 패턴별 데미지 판정 로직

| 패턴 | 판정 방식 |
|------|----------|
| P1 TrackingZone | `lockedTarget` 중심 반지름 `range` 내 적 유닛 전부 |
| P2 Charge | `BossMonsterView.StartCharge()` 콜백으로 경로 충돌 유닛 판정 |
| P3 CrossZone | 보스 중심 ±X·±Y 방향 `range` 길이 × `width` 폭 4개 박스 |
| P4 XZone | 보스 중심 45°·135°·225°·315° 방향 4개 박스 (P3와 동일 크기) |
| P5 CurseMark | `lockedTarget` 중심 반지름 `range` (1 타일 이하) |
| P6 Projectile | 투사체 GO가 충돌 시 트리거 판정 |
| P7 SummonMinions | 보스 주변 `summonRadius` 내 빈 셀에 `summonCount`마리 소환 |

P2(Charge)는 `BossMonsterView.StartCharge()`를 호출하여 MonoBehaviour가 이동 Coroutine을 처리하고, 완료 시 `BossPatternCastState.NotifyChargeComplete()`를 콜백으로 호출한다.

---

## IBossPattern / 구체 패턴 클래스

각 패턴은 `BossPatternCastState`의 Activate 단계에서 데미지 판정 로직을 담당한다.
`BossChaseState`가 `BossPatternType → IBossPattern` 딕셔너리를 보유하여 패턴 선택 시 전달한다.

```csharp
public interface IBossPattern
{
    /// <summary>경고 단계 매 프레임 — 위치 추적이 필요한 패턴만 구현. 기본 no-op.</summary>
    void OnWarningTick(BossMonster boss, BossPatternData data, ref Vector2 lockedTarget) { }

    /// <summary>활성화 — 인디케이터 확정 + 데미지 판정 시작.</summary>
    void Activate(BossMonster boss, BossPatternData data, Vector2 lockedTarget,
                  SpatialGrid<IUnit> unitGrid, Notifier notifier, BossMonsterView view);
}
```

### P7 — SummonMinionsPattern

보스가 제자리에서 주변에 일반 몬스터를 소환한다. Warning 단계에서는 소환 예고 이펙트(선택),
Active 단계에서 `EntitySpawner`를 통해 실제 소환한다.

```csharp
public class SummonMinionsPattern : IBossPattern
{
    private readonly EntitySpawner spawner;
    private readonly ObstacleGrid  obstacleGrid;

    public void Activate(BossMonster boss, BossPatternData data, Vector2 lockedTarget,
                         SpatialGrid<IUnit> unitGrid, Notifier notifier, BossMonsterView view)
    {
        var origin  = (Vector2)boss.Transform.position;
        int spawned = 0;

        for (int attempt = 0; attempt < 20 && spawned < data.summonCount; attempt++)
        {
            var offset = UnityEngine.Random.insideUnitCircle.normalized * data.summonRadius;
            var pos    = origin + offset;
            if (!obstacleGrid.IsWalkable(pos)) continue;
            spawner.SpawnMonster(data.summonData, pos);
            spawned++;
        }
    }
}
```

> `SummonMinionsPattern`은 생성자 주입으로 `EntitySpawner`와 `ObstacleGrid`를 받는다.
> `BossChaseState` 내 딕셔너리 초기화 시 주입한다.

---

## BossMonsterView (MonoBehaviour)

`MonsterView`를 상속. 인디케이터 프리팹 레퍼런스와 P2 돌진 Coroutine을 소유한다.

```csharp
public class BossMonsterView : MonsterView
{
    [SerializeField] private ZoneIndicatorView circleIndicator;
    [SerializeField] private ZoneIndicatorView crossIndicator;
    [SerializeField] private ZoneIndicatorView xIndicator;
    [SerializeField] private ZoneIndicatorView lineIndicator;

    /// <summary>P1·P3·P4·P5 — 지정 타입의 인디케이터를 표시한다.</summary>
    public void ShowIndicator(BossPatternType type, Vector2 worldPos, float range) { ... }

    /// <summary>활성화 직전 인디케이터를 경고색(빨강)으로 전환.</summary>
    public void FlashIndicatorActive(BossPatternType type) { ... }

    public void HideAllIndicators() { ... }

    /// <summary>P2 전용 — 목표 방향으로 chargeSpeed 속도 직선 이동.</summary>
    public void StartCharge(Vector2 direction, float distance, float speed, Action onComplete) { ... }

    /// <summary>P6 전용 — 투사체 프리팹을 방향·간격에 따라 순차 발사.</summary>
    public void FireProjectiles(Vector2 direction, BossPatternData data) { ... }
}
```

### ZoneIndicatorView

```csharp
public class ZoneIndicatorView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;

    private static readonly Color WarningColor = new Color(1f, 0.85f, 0f, 0.5f); // 반투명 노랑
    private static readonly Color ActiveColor  = new Color(1f, 0.1f, 0.1f, 0.8f); // 진한 빨강

    public void Show(Vector2 worldPos, float scale) { ... }   // 활성화 + 위치·스케일 설정
    public void FlashActive() { ... }                          // 0.3초 DOTween → ActiveColor
    public void Hide() { ... }                                 // DOFade 0 후 비활성화
}
```

---

## BossSpawnSystem (pure C#)

`GameController`가 소유하고 `Update(deltaTime)`으로 구동.

```csharp
public class BossSpawnSystem
{
    private readonly BossMonsterData[] bossPool;       // YellowMonk, YellowPawn
    private readonly EntitySpawner entitySpawner;
    private readonly BossWarningView warningView;
    private readonly BossHpBarView hpBarView;

    private float elapsedTime;
    private float respawnTimer;
    private BossMonster activeBoss;

    private const float SpawnTime    = 180f;
    private const float RespawnDelay = 240f;
    private const float WarnDuration = 2.5f;           // 경고 UI 표시 시간

    public event Action<BossMonster> OnBossSpawned;
    public event Action<BossMonster> OnBossDied;

    public void Update(float deltaTime)
    {
        if (activeBoss != null) return;

        elapsedTime += deltaTime;
        respawnTimer -= deltaTime;

        if (ShouldSpawn()) StartSpawnSequence();
    }

    private bool ShouldSpawn() =>
        elapsedTime >= SpawnTime && respawnTimer <= 0f;

    private void StartSpawnSequence()
    {
        var data = bossPool[UnityEngine.Random.Range(0, bossPool.Length)];
        warningView.Show(data.displayName, data.icon, WarnDuration, () => SpawnBoss(data));
    }

    private void SpawnBoss(BossMonsterData data) { ... }
    private void OnBossDefeated(BossMonster boss) { ... }  // hpBarView 숨김, respawnTimer 시작
}
```

---

## UI 컴포넌트

### BossHpBarView (PlayPage 상단)

```csharp
public class BossHpBarView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image          iconImage;
    [SerializeField] private Image          hpFillImage;

    public void Bind(BossMonster boss)
    {
        nameText.text = boss.BossData.displayName;
        iconImage.sprite = boss.BossData.icon;
        boss.Health.OnDamaged += _ => Refresh(boss.Health);
        boss.Health.OnDeath   += () => Hide();
        gameObject.SetActive(true);
        Refresh(boss.Health);
    }

    private void Refresh(UnitHealth h) =>
        hpFillImage.fillAmount = (float)h.CurrentHp / h.MaxHp;

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
```

### BossWarningView

```csharp
public class BossWarningView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private Image          bossIcon;
    [SerializeField] private CanvasGroup    canvasGroup;

    /// <summary>경고 UI를 duration초 표시 후 onComplete 호출.</summary>
    public void Show(string bossName, Sprite icon, float duration, Action onComplete)
    {
        bossNameText.text = bossName;
        bossIcon.sprite   = icon;
        gameObject.SetActive(true);

        canvasGroup.alpha = 0f;
        DOTween.Sequence()
            .Append(canvasGroup.DOFade(1f, 0.3f))
            .AppendInterval(duration - 0.6f)
            .Append(canvasGroup.DOFade(0f, 0.3f))
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            });
    }
}
```

---

## GameController 연동

```csharp
// GameController 생성자
bossSpawnSystem = new BossSpawnSystem(
    bossPool, entitySpawner, bossWarningView, bossHpBarView
);
bossSpawnSystem.OnBossSpawned += boss =>
{
    combatSystem.RegisterUnit(boss);
    bossHpBarView.Bind(boss);
};
bossSpawnSystem.OnBossDied += boss =>
{
    combatSystem.UnregisterUnit(boss);
};

// GameController.Update()
bossSpawnSystem.Update(Time.deltaTime);
if (activeBoss != null) activeBoss.Update();
```

---

## P6 투사체 (Projectile)

```csharp
public class BossProjectile : MonoBehaviour
{
    private int damage;
    private float maxDistance;
    private Vector2 startPos;
    private Vector2 direction;
    private float speed;
    private Notifier notifier;
    private IUnit owner;

    public void Initialize(IUnit owner, Vector2 dir, BossPatternData data, Notifier notifier)
    {
        this.owner    = owner;
        this.damage   = data.damage;
        this.maxDistance = data.maxDistance;
        this.direction = dir;
        this.speed    = data.projectileSpeed;
        this.notifier = notifier;
        startPos      = transform.position;
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        if (Vector2.Distance(startPos, transform.position) >= maxDistance)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<CharacterView>(out var view)) return;
        // IUnit 탐색 후 DamageProcessor.ProcessDamage 호출
        Destroy(gameObject);
    }
}
```

---

## 씬 계층구조 (PlayScene)

```
PlayScene
├── WorldMap
├── UnitRoot
│   └── BossRoot        ← BossMonster View 프리팹 인스턴스
├── UI (Canvas)
│   └── PlayPage
│       ├── BossHpBarView   (평소 비활성, 상단 고정)
│       └── BossWarningView (평소 비활성, 중앙 오버레이)
└── ...
```
