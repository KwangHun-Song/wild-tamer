# 보스 시스템 구현 계획

## 개요

설계 문서: [boss_system.md](design/boss_system.md)

180초 이후 랜덤 1종(YellowMonk / YellowPawn) 보스가 스폰되고,
패턴(장판·돌진·투사체)을 순차 실행하며, PlayPage 상단 HP 바와 경고 UI가 연동된다.

### 핵심 기능 요약

| 기능 | 설명 |
|------|------|
| BossMonsterData | 스탯·패턴·인레이지 수치를 ScriptableObject로 외부화 |
| BossFSM | `BossIdleState → BossChaseState → BossPatternCastState → BossDeadState` 4상태 FSM |
| ZoneIndicatorView | 장판 예고 프리팹 (Circle / Cross / X / Line) |
| P2 돌진 | BossMonsterView Coroutine으로 이동 처리 |
| P6 투사체 | 풀 없이 Instantiate / Destroy (보스당 최대 3발) |
| P7 소환 | EntitySpawner로 보스 주변에 일반 몬스터 소환 |
| BossSpawnSystem | 180초 타이머 → 랜덤 선택 → 경고 2.5초 → 스폰 |
| BossHpBarView | PlayPage 상단 고정, Bind() 호출 시 활성화 |
| BossWarningView | 스폰 직전 중앙 오버레이, DOTween FadeIn/Out |

---

## 아키텍처 개요

### 새 파일

| 클래스 | 경로 | 역할 |
|--------|------|------|
| `BossMonsterData` | `ScriptableObjects/BossMonsterData.cs` | 보스 기획 데이터 |
| `BossPatternData` | `ScriptableObjects/BossPatternData.cs` | 패턴별 파라미터 |
| `BossMonster` | `01.Entity/Boss/BossMonster.cs` | Monster 상속, BossFSM 구동 |
| `BossMonsterView` | `01.Entity/Boss/BossMonsterView.cs` | MonsterView 상속, 인디케이터·Coroutine 관리 |
| `BossFSM` | `01.Entity/Boss/BossFSM.cs` | 보스 전용 FSM |
| `BossIdleState` | `01.Entity/Boss/States/BossIdleState.cs` | 스폰 직후 대기 상태 |
| `BossChaseState` | `01.Entity/Boss/States/BossChaseState.cs` | 추적 이동 + 패턴 쿨다운 관리 |
| `BossPatternCastState` | `01.Entity/Boss/States/BossPatternCastState.cs` | Warning→Active 타이머 |
| `BossDeadState` | `01.Entity/Boss/States/BossDeadState.cs` | 사망 연출 |
| `IBossPattern` | `01.Entity/Boss/Patterns/IBossPattern.cs` | 패턴 인터페이스 |
| `TrackingZonePattern` | `01.Entity/Boss/Patterns/TrackingZonePattern.cs` | P1 |
| `ChargePattern` | `01.Entity/Boss/Patterns/ChargePattern.cs` | P2 |
| `CrossZonePattern` | `01.Entity/Boss/Patterns/CrossZonePattern.cs` | P3 |
| `XZonePattern` | `01.Entity/Boss/Patterns/XZonePattern.cs` | P4 |
| `CurseMarkPattern` | `01.Entity/Boss/Patterns/CurseMarkPattern.cs` | P5 |
| `ProjectileBarragePattern` | `01.Entity/Boss/Patterns/ProjectileBarragePattern.cs` | P6 |
| `SummonMinionsPattern` | `01.Entity/Boss/Patterns/SummonMinionsPattern.cs` | P7 |
| `ZoneIndicatorView` | `01.Entity/Boss/ZoneIndicatorView.cs` | 장판 예고 프리팹 스크립트 |
| `BossProjectile` | `01.Entity/Boss/BossProjectile.cs` | P6 투사체 MonoBehaviour |
| `BossSpawnSystem` | `02.System/Boss/BossSpawnSystem.cs` | 타이머 + 스폰 관리 |
| `BossHpBarView` | `03.UI/Boss/BossHpBarView.cs` | PlayPage 상단 HP 바 |
| `BossWarningView` | `03.UI/Boss/BossWarningView.cs` | 스폰 경고 오버레이 |

### 수정 파일

| 파일 | 변경 내용 |
|------|----------|
| `EntitySpawner.cs` | `SpawnBoss()` / `DespawnBoss()` 추가 |
| `GameController.cs` | `BossSpawnSystem` 통합, `Update()` 호출 추가 |
| `GameLoop.cs` | `BossMonsterData[]`, `BossHpBarView`, `BossWarningView` SerializeField 추가 및 GameController 생성자 전달 |

---

## 핵심 설계 결정

### BossFSM — 4상태 FSM

Monster·Player·SquadMember와 동일한 `StateMachine<TOwner, TTrigger>` 모듈을 사용한다.
`BossIdleState → BossChaseState → BossPatternCastState → BossDeadState` 4개 상태로
이동·추적·패턴 실행을 통합 관리한다.
`BossPatternCastState`가 Warning→Active 타이머를 담당하며,
Interval 단계는 `BossChaseState`의 쿨다운 틱으로 대체된다.
예외: P2 돌진 이동은 `BossMonsterView.StartCharge(onComplete)` Coroutine으로 위임하고
완료 시 `BossPatternCastState.NotifyChargeComplete()`를 콜백으로 호출한다.

**감지 범위**: `BossMonsterData.detectionRange`를 맵 크기 이상(예: `999f`)으로 설정하면
스폰 직후 `BossIdleState`에서 즉시 `Detected`가 발화되어 항상 플레이어를 추적한다.

### 패턴 데미지 판정 — SpatialGrid Query

CombatSystem의 `ProcessCombat`과 별개로,
패턴 활성화 시 `unitGrid.Query(pos, range)` 로 범위 내 적을 직접 조회하고
`DamageProcessor.ProcessDamage()` 를 호출한다.
(CombatSystem의 자동 교전과 중복 카운트 없음 — 패턴 데미지는 쿨다운 검사 없이 즉시 적용)

### P2 돌진 — 이동 Coroutine 분리

돌진 중 FSM 이동을 막기 위해 `BossMonsterView`에 `isCharging` 플래그를 두고
`CharacterView.Movement.Move()`를 차단한다.
돌진 완료 후 플래그 해제 + `onComplete()` 콜백으로 `EndPattern()` 호출.

### 보스 Radius — IUnit 구현

`BossMonster.Radius`는 `BossMonsterData.radius` 값을 그대로 반환한다.
`CombatSystem.ResolveOverlaps()`가 보스와 일반 유닛 간 겹침을 처리한다.

---

## 단계별 구현 순서

### Step 1 — ScriptableObject 데이터 클래스 2종 생성 [병렬 가능: Step 2와]

**새 파일:** `Assets/Scripts/ScriptableObjects/BossPatternData.cs`

```csharp
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
    SummonMinions,     // P7
}
```

**새 파일:** `Assets/Scripts/ScriptableObjects/BossMonsterData.cs`

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Data/BossMonsterData")]
public class BossMonsterData : ScriptableObject
{
    [Header("기본 정보")]
    public string id;
    public string displayName;
    public Sprite icon;

    [Header("스탯")]
    public int   maxHp           = 600;
    public float moveSpeed       = 1.5f;
    public int   attackDamage    = 10;
    public float attackRange     = 1.2f;
    public float detectionRange  = 10f;
    public float radius          = 0.5f;
    public float attackCooldown  = 1.5f;

    [Header("패턴")]
    public BossPatternData[] patterns;
    [Tooltip("패턴 사이 휴지 시간(초)")]
    public float patternInterval = 1.5f;

    [Header("인레이지")]
    [Range(0f, 1f)] public float enrageThreshold       = 0.5f;
    public float enrageCooldownMultiplier  = 0.8f;
    public float enrageSpeedMultiplier     = 1.0f;

    [Header("프리팹")]
    public GameObject viewPrefab;
}
```

**검증:** 메뉴 `Data/BossMonsterData`, `Data/BossPatternData` 에셋 생성 가능.

---

### Step 2 — `ZoneIndicatorView` + `BossProjectile` 생성 [병렬 가능: Step 1과]

**새 파일:** `Assets/Scripts/04.Game/01.Entity/Boss/ZoneIndicatorView.cs`

```csharp
using DG.Tweening;
using UnityEngine;

public class ZoneIndicatorView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;

    private static readonly Color WarningColor = new Color(1f, 0.85f, 0f, 0.5f);
    private static readonly Color ActiveColor  = new Color(1f, 0.15f, 0.15f, 0.85f);

    /// <summary>지정 위치·크기로 인디케이터를 표시한다. scaleMultiplier는 range 기반 스케일.</summary>
    public void Show(Vector2 worldPos, float scaleX, float scaleY)
    {
        transform.position = new Vector3(worldPos.x, worldPos.y, worldPos.y);
        transform.localScale = new Vector3(scaleX, scaleY, 1f);
        sr.color = WarningColor;
        gameObject.SetActive(true);
    }

    /// <summary>경고 → 빨간색으로 0.3초 전환. 활성 직전 호출.</summary>
    public void FlashActive()
    {
        sr.DOKill();
        sr.DOColor(ActiveColor, 0.3f);
    }

    public void Hide()
    {
        sr.DOKill();
        sr.DOFade(0f, 0.2f).OnComplete(() => gameObject.SetActive(false));
    }

    public void UpdatePosition(Vector2 worldPos)
    {
        transform.position = new Vector3(worldPos.x, worldPos.y, worldPos.y);
    }
}
```

**새 파일:** `Assets/Scripts/04.Game/01.Entity/Boss/BossProjectile.cs`

```csharp
using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    private int     damage;
    private float   maxDistance;
    private float   speed;
    private Vector2 direction;
    private Vector2 startPos;
    private Notifier notifier;
    private IUnit   owner;

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
        if (!other.TryGetComponent<CharacterView>(out _)) return;

        // CharacterView → IUnit 탐색: Player / SquadMember / Monster
        IUnit target = other.GetComponent<PlayerView>()        != null ? FindUnit<Player>(other)       :
                       other.GetComponent<SquadMemberView>()   != null ? FindUnit<SquadMember>(other)  :
                       null;

        if (target == null || target.Team == owner.Team || !target.IsAlive) return;

        DamageProcessor.ProcessDamage(owner, target, notifier);
        Destroy(gameObject);
    }

    private static IUnit FindUnit<T>(Collider2D col) where T : class, IUnit
        => col.GetComponentInParent<T>();   // View → Character 탐색은 별도 구조에 맞게 조정
}
```

> 주의: `BossProjectile`의 IUnit 탐색 방식은 프로젝트의 View↔Presenter 연결 구조에 따라
> 조정이 필요할 수 있다. `CharacterView`에 `IUnit Owner` 프로퍼티 추가가 가장 단순한 대안.

**검증:** 컴파일 오류 없음.

---

### Step 3 — `IBossPattern` + 7개 패턴 클래스 생성 (Step 1 완료 후)

**새 파일:** `Assets/Scripts/04.Game/01.Entity/Boss/Patterns/IBossPattern.cs`

```csharp
using UnityEngine;

public interface IBossPattern
{
    /// <summary>Warning 단계 매 프레임 — 위치 추적이 필요한 패턴만 구현. 기본 no-op.</summary>
    void OnWarningTick(BossMonster boss, BossPatternData data, ref Vector2 lockedTarget) { }

    /// <summary>Active 단계 진입 — 데미지 판정 + 인디케이터 FlashActive.</summary>
    void Activate(BossMonster boss, BossPatternData data, Vector2 lockedTarget,
                  SpatialGrid<IUnit> unitGrid, Notifier notifier, BossMonsterView view);
}
```

**새 파일:** `TrackingZonePattern.cs` (P1)

```csharp
using UnityEngine;

public class TrackingZonePattern : IBossPattern
{
    public void OnWarningTick(BossMonster boss, BossPatternData data, ref Vector2 lockedTarget)
    {
        // Warning 동안 플레이어(적 중 Player 팀) 방향으로 lockedTarget 갱신
        var nearest = FindNearestEnemy(boss);
        if (nearest != null)
            lockedTarget = nearest.Transform.position;

        boss.BossView.MoveIndicator(BossPatternType.TrackingZone, lockedTarget);
    }

    public void Activate(BossMonster boss, BossPatternData data, Vector2 lockedTarget,
                         SpatialGrid<IUnit> unitGrid, Notifier notifier, BossMonsterView view)
    {
        view.FlashIndicator(BossPatternType.TrackingZone);
        foreach (var u in unitGrid.Query(lockedTarget, data.range))
        {
            if (u.Team == boss.Team || !u.IsAlive) continue;
            if (Vector2.Distance(lockedTarget, u.Transform.position) <= data.range)
                DamageProcessor.ProcessDamage(boss, u, notifier);
        }
    }

    private static IUnit FindNearestEnemy(BossMonster boss)
    {
        // boss.UnitGrid.Query 로 Enemy Team 탐색
        var pos = (Vector2)boss.Transform.position;
        IUnit nearest = null;
        float minDist = float.MaxValue;
        foreach (var u in boss.UnitGrid.Query(pos, boss.Combat.DetectionRange))
        {
            if (u.Team == boss.Team || !u.IsAlive) continue;
            float d = Vector2.Distance(pos, u.Transform.position);
            if (d < minDist) { minDist = d; nearest = u; }
        }
        return nearest;
    }
}
```

**새 파일:** `ChargePattern.cs` (P2)

```csharp
using UnityEngine;

public class ChargePattern : IBossPattern
{
    public void OnWarningTick(BossMonster boss, BossPatternData data, ref Vector2 lockedTarget)
    {
        // Warning 동안 가장 가까운 적 방향을 lockedTarget(방향 벡터)으로 저장
        var nearest = FindNearestEnemy(boss);
        if (nearest != null)
            lockedTarget = ((Vector2)nearest.Transform.position - (Vector2)boss.Transform.position).normalized;

        boss.BossView.MoveChargeIndicator(lockedTarget, data.chargeDistance, data.chargeWidth);
    }

    public void Activate(BossMonster boss, BossPatternData data, Vector2 lockedTarget,
                         SpatialGrid<IUnit> unitGrid, Notifier notifier, BossMonsterView view)
    {
        // P2는 Activate 진입 시 Coroutine 돌진을 시작하고 EndPattern은 콜백으로 처리
        // BossPatternExecutor에서 P2 전용 처리를 분기한다 (아래 설명 참고)
        view.StartCharge(lockedTarget, data, (hitUnits) =>
        {
            foreach (var u in hitUnits)
                DamageProcessor.ProcessDamage(boss, u, notifier);
        });
    }

    private static IUnit FindNearestEnemy(BossMonster boss) { /* TrackingZone과 동일 */ throw new System.NotImplementedException(); }
}
```

**새 파일:** `CrossZonePattern.cs` (P3), `XZonePattern.cs` (P4)

P3/P4는 구조가 동일하고 방향(축 vs 대각선)만 다르다.

```csharp
// CrossZonePattern.cs
public class CrossZonePattern : IBossPattern
{
    private static readonly Vector2[] Directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

    public void Activate(BossMonster boss, BossPatternData data, Vector2 lockedTarget,
                         SpatialGrid<IUnit> unitGrid, Notifier notifier, BossMonsterView view)
    {
        view.FlashIndicator(BossPatternType.CrossZone);
        var origin = (Vector2)boss.Transform.position;
        foreach (var dir in Directions)
            DamageLineArea(origin, dir, data, boss, unitGrid, notifier);
    }

    private static void DamageLineArea(Vector2 origin, Vector2 dir, BossPatternData data,
                                       BossMonster boss, SpatialGrid<IUnit> grid, Notifier notifier)
    {
        float halfWidth = data.width * 0.5f;
        foreach (var u in grid.Query(origin, data.range + halfWidth))
        {
            if (u.Team == boss.Team || !u.IsAlive) continue;
            var toUnit = (Vector2)u.Transform.position - origin;
            float along = Vector2.Dot(toUnit, dir);
            if (along < 0f || along > data.range) continue;
            var perp   = toUnit - dir * along;
            if (perp.magnitude <= halfWidth)
                DamageProcessor.ProcessDamage(boss, u, notifier);
        }
    }
}

// XZonePattern.cs — Directions만 대각선으로 변경
public class XZonePattern : IBossPattern
{
    private static readonly float Inv = 1f / Mathf.Sqrt(2f);
    private static readonly Vector2[] Directions =
    {
        new Vector2( Inv,  Inv), new Vector2(-Inv,  Inv),
        new Vector2( Inv, -Inv), new Vector2(-Inv, -Inv),
    };
    // Activate 로직은 CrossZonePattern과 동일 (공통 헬퍼 사용)
    public void Activate(...) { /* 위와 동일 */ }
}
```

**새 파일:** `CurseMarkPattern.cs` (P5)

```csharp
public class CurseMarkPattern : IBossPattern
{
    public void OnWarningTick(BossMonster boss, BossPatternData data, ref Vector2 lockedTarget)
    {
        // Warning 초반 절반: 플레이어 현재 위치 추적
        // Warning 후반 절반: lockedTarget 고정 (BossPatternExecutor에서 제어)
        var nearest = FindNearestEnemy(boss);
        if (nearest != null)
            lockedTarget = nearest.Transform.position;
        boss.BossView.MoveIndicator(BossPatternType.CurseMark, lockedTarget);
    }

    public void Activate(BossMonster boss, BossPatternData data, Vector2 lockedTarget,
                         SpatialGrid<IUnit> unitGrid, Notifier notifier, BossMonsterView view)
    {
        view.FlashIndicator(BossPatternType.CurseMark);
        foreach (var u in unitGrid.Query(lockedTarget, data.range))
        {
            if (u.Team == boss.Team || !u.IsAlive) continue;
            if (Vector2.Distance(lockedTarget, u.Transform.position) <= data.range)
                DamageProcessor.ProcessDamage(boss, u, notifier);
        }
    }
    private static IUnit FindNearestEnemy(BossMonster boss) { throw new System.NotImplementedException(); }
}
```

**새 파일:** `ProjectileBarragePattern.cs` (P6)

```csharp
using UnityEngine;

public class ProjectileBarragePattern : IBossPattern
{
    public void Activate(BossMonster boss, BossPatternData data, Vector2 lockedTarget,
                         SpatialGrid<IUnit> unitGrid, Notifier notifier, BossMonsterView view)
    {
        // 타겟 방향 계산
        var dir = (lockedTarget - (Vector2)boss.Transform.position).normalized;
        view.FireProjectiles(boss, dir, data, notifier);
    }
}
```

**새 파일:** `SummonMinionsPattern.cs` (P7)

```csharp
using UnityEngine;

public class SummonMinionsPattern : IBossPattern
{
    private readonly EntitySpawner entitySpawner;
    private readonly ObstacleGrid  obstacleGrid;

    public SummonMinionsPattern(EntitySpawner entitySpawner, ObstacleGrid obstacleGrid)
    {
        this.entitySpawner = entitySpawner;
        this.obstacleGrid  = obstacleGrid;
    }

    public void Activate(BossMonster boss, BossPatternData data, Vector2 lockedTarget,
                         SpatialGrid<IUnit> unitGrid, Notifier notifier, BossMonsterView view)
    {
        var origin  = (Vector2)boss.Transform.position;
        int spawned = 0;

        for (int attempt = 0; attempt < 20 && spawned < data.summonCount; attempt++)
        {
            var offset = Random.insideUnitCircle.normalized * data.summonRadius;
            var pos    = origin + offset;
            if (!obstacleGrid.IsWalkable(pos)) continue;
            entitySpawner.SpawnMonster(data.summonData, pos);
            spawned++;
        }
    }
}
```

**검증:** 컴파일 오류 없음.

---

### Step 4 — `BossTrigger` + `BossFSM` + State 4종 생성 (Step 3 완료 후)

**새 파일:** `Assets/Scripts/04.Game/01.Entity/Boss/BossFSM.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

public enum BossTrigger { Detected, PatternReady, PatternComplete, Die }

public class BossFSM
{
    private readonly StateMachine<BossMonster, BossTrigger> stateMachine;

    public readonly BossChaseState       ChaseState;
    public readonly BossPatternCastState CastState;

    public BossFSM(BossMonster boss, BossMonsterView view)
    {
        var idle  = new BossIdleState();
        ChaseState = new BossChaseState(boss.BossData, view);
        CastState  = new BossPatternCastState(view);
        var dead  = new BossDeadState();

        stateMachine = new StateMachine<BossMonster, BossTrigger>(boss, idle);
        stateMachine.AddTransition(idle,       BossTrigger.Detected,         ChaseState);
        stateMachine.AddTransition(ChaseState, BossTrigger.PatternReady,     CastState);
        stateMachine.AddTransition(ChaseState, BossTrigger.Die,              dead);
        stateMachine.AddTransition(CastState,  BossTrigger.PatternComplete,  ChaseState);
        stateMachine.AddTransition(CastState,  BossTrigger.Die,              dead);
    }

    public void Update(float deltaTime) => stateMachine.Update();
    public void Fire(BossTrigger trigger) => stateMachine.Fire(trigger);
}
```

**새 파일:** `Assets/Scripts/04.Game/01.Entity/Boss/States/BossIdleState.cs`

```csharp
public class BossIdleState : State<BossMonster, BossTrigger>
{
    public override void OnEnter() => Owner.View.PlayIdleAnimation();

    public override void OnUpdate()
    {
        // detectionRange를 맵 크기 이상으로 설정하면 즉시 발화
        var pos = (Vector2)Owner.Transform.position;
        foreach (var u in Owner.UnitGrid.Query(pos, Owner.BossData.detectionRange))
        {
            if (u.Team == Owner.Team || !u.IsAlive) continue;
            Machine.Fire(BossTrigger.Detected);
            return;
        }
    }
}
```

**새 파일:** `Assets/Scripts/04.Game/01.Entity/Boss/States/BossChaseState.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

public class BossChaseState : State<BossMonster, BossTrigger>
{
    private readonly BossMonsterData data;
    private readonly BossMonsterView view;
    private readonly float[]         cooldowns;

    private static readonly Dictionary<BossPatternType, IBossPattern> Handlers = new()
    {
        { BossPatternType.TrackingZone,      new TrackingZonePattern()      },
        { BossPatternType.Charge,            new ChargePattern()            },
        { BossPatternType.CrossZone,         new CrossZonePattern()         },
        { BossPatternType.XZone,             new XZonePattern()             },
        { BossPatternType.CurseMark,         new CurseMarkPattern()         },
        { BossPatternType.ProjectileBarrage, new ProjectileBarragePattern() },
        // SummonMinionsPattern은 EntitySpawner 주입 필요 — BossFSM 생성자에서 추가
    };

    public BossChaseState(BossMonsterData data, BossMonsterView view)
    {
        this.data     = data;
        this.view     = view;
        cooldowns     = new float[data.patterns.Length];
    }

    public override void OnEnter() => Owner.View.PlayMoveAnimation();

    public override void OnUpdate()
    {
        // 추적 이동 (기존 MonsterLeaderFSM과 동일한 A* 방식)
        MoveTowardPlayer();

        // 쿨다운 틱
        for (int i = 0; i < cooldowns.Length; i++)
            if (cooldowns[i] > 0f) cooldowns[i] -= Time.deltaTime;

        // HP 0 체크
        if (!Owner.IsAlive) { Machine.Fire(BossTrigger.Die); return; }

        // 준비된 패턴 선택
        TryFirePattern();
    }

    private void TryFirePattern()
    {
        var patterns = data.patterns;
        var ready    = new List<int>();
        for (int i = 0; i < patterns.Length; i++)
            if (cooldowns[i] <= 0f) ready.Add(i);

        if (ready.Count == 0) return;

        int idx      = ready[Random.Range(0, ready.Count)];
        var selected = patterns[idx];
        var mult     = Owner.IsEnraged ? data.enrageCooldownMultiplier : 1f;
        cooldowns[idx] = selected.cooldown * mult;

        Owner.BossFSM.CastState.SetPattern(selected,
            Handlers.TryGetValue(selected.type, out var h) ? h : null);
        Machine.Fire(BossTrigger.PatternReady);
    }

    private void MoveTowardPlayer() { /* A* 경로 추적 — Monster 기존 로직 재사용 */ }
}
```

**새 파일:** `Assets/Scripts/04.Game/01.Entity/Boss/States/BossPatternCastState.cs`

```csharp
using UnityEngine;

public class BossPatternCastState : State<BossMonster, BossTrigger>
{
    private readonly BossMonsterView view;

    private BossPatternData pattern;
    private IBossPattern    handler;
    private Vector2         lockedTarget;
    private float           phaseTimer;
    private bool            isWarning = true;
    private bool            chargeComplete;

    public BossPatternCastState(BossMonsterView view) { this.view = view; }

    public void SetPattern(BossPatternData selected, IBossPattern h)
    {
        pattern        = selected;
        handler        = h;
        isWarning      = true;
        chargeComplete = false;
    }

    public override void OnEnter()
    {
        Owner.View.Movement.Move(Vector2.zero); // 이동 정지
        lockedTarget = Owner.Transform.position;
        phaseTimer   = pattern.warningDuration;
        view.ShowIndicator(pattern.type, Owner.Transform.position, pattern);
    }

    public override void OnUpdate()
    {
        phaseTimer -= Time.deltaTime;

        if (!Owner.IsAlive) { Machine.Fire(BossTrigger.Die); return; }

        if (isWarning)
        {
            handler?.OnWarningTick(Owner, pattern, ref lockedTarget);
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
            {
                view.HideIndicator(pattern.type);
                Machine.Fire(BossTrigger.PatternComplete);
            }
        }
    }

    private void ActivatePattern()
    {
        view.FlashIndicator(pattern.type);
        handler?.Activate(Owner, pattern, lockedTarget,
                          Owner.UnitGrid, Owner.Notifier, view);
        if (pattern.type == BossPatternType.Charge)
            view.RegisterChargeComplete(() => chargeComplete = true);
    }

    public override void OnExit() => view.HideAllIndicators();
}
```

**새 파일:** `Assets/Scripts/04.Game/01.Entity/Boss/States/BossDeadState.cs`

```csharp
public class BossDeadState : State<BossMonster, BossTrigger>
{
    public override void OnEnter() => Owner.View.PlayDeathSequence();
}
```

**검증:** 컴파일 오류 없음. `StateMachine<BossMonster, BossTrigger>` 및 `State<BossMonster, BossTrigger>` 기반 클래스가 존재해야 함.

---

### Step 5 — `BossMonsterView` 생성 (Step 2, 4 완료 후)

**새 파일:** `Assets/Scripts/04.Game/01.Entity/Boss/BossMonsterView.cs`

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossMonsterView : MonsterView
{
    [Header("존 인디케이터")]
    [SerializeField] private ZoneIndicatorView circleIndicator;
    [SerializeField] private ZoneIndicatorView crossIndicator;
    [SerializeField] private ZoneIndicatorView xIndicator;
    [SerializeField] private ZoneIndicatorView lineIndicator;

    [Header("P6 투사체")]
    [SerializeField] private GameObject projectilePrefab;

    private Action chargeCompleteCallback;
    private bool   isCharging;

    // ── 인디케이터 API ────────────────────────────────────────────
    public void ShowIndicator(BossPatternType type, Vector2 origin, BossPatternData data)
    {
        var ind = GetIndicator(type);
        if (ind == null) return;
        float sx = type == BossPatternType.TrackingZone || type == BossPatternType.CurseMark
            ? data.range * 2f
            : data.range * 2f;
        float sy = sx;
        ind.Show(origin, sx, sy);
    }

    public void MoveIndicator(BossPatternType type, Vector2 worldPos)
        => GetIndicator(type)?.UpdatePosition(worldPos);

    public void MoveChargeIndicator(Vector2 dir, float distance, float width)
    {
        var pos = (Vector2)transform.position + dir * (distance * 0.5f);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        lineIndicator?.Show(pos, width, distance);
        lineIndicator?.transform.SetPositionAndRotation(
            new Vector3(pos.x, pos.y, pos.y),
            Quaternion.Euler(0f, 0f, angle));
    }

    public void FlashIndicator(BossPatternType type) => GetIndicator(type)?.FlashActive();

    public void HideIndicator(BossPatternType type) => GetIndicator(type)?.Hide();

    private ZoneIndicatorView GetIndicator(BossPatternType type) => type switch
    {
        BossPatternType.TrackingZone      => circleIndicator,
        BossPatternType.CurseMark         => circleIndicator,
        BossPatternType.CrossZone         => crossIndicator,
        BossPatternType.XZone            => xIndicator,
        BossPatternType.Charge            => lineIndicator,
        _ => null,
    };

    // ── P2 돌진 ───────────────────────────────────────────────────
    public void RegisterChargeComplete(Action onComplete) => chargeCompleteCallback = onComplete;

    public void StartCharge(Vector2 direction, BossPatternData data, Action<List<IUnit>> onHit)
    {
        if (isCharging) return;
        StartCoroutine(ChargeRoutine(direction, data, onHit));
    }

    private IEnumerator ChargeRoutine(Vector2 dir, BossPatternData data, Action<List<IUnit>> onHit)
    {
        isCharging = true;
        Movement.MoveSpeed = data.chargeSpeed;

        float elapsed   = 0f;
        float duration  = data.chargeDistance / data.chargeSpeed;
        var   startPos  = (Vector2)transform.position;
        var   hitUnits  = new List<IUnit>();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Movement.Move(dir);
            // 이동 중 경로상 유닛 수집 (중복 방지)
            // 실제 Hit 판정은 완료 후 onHit 콜백으로 전달
            yield return null;
        }

        Movement.Move(Vector2.zero);
        Movement.MoveSpeed = /* 원래 속도로 복원 — BossMonster가 보관 */ 0f;
        isCharging = false;

        onHit?.Invoke(hitUnits);
        chargeCompleteCallback?.Invoke();
        chargeCompleteCallback = null;
    }

    // ── P6 투사체 발사 ────────────────────────────────────────────
    public void FireProjectiles(IUnit owner, Vector2 baseDir, BossPatternData data, Notifier notifier)
    {
        StartCoroutine(FireRoutine(owner, baseDir, data, notifier));
    }

    private IEnumerator FireRoutine(IUnit owner, Vector2 baseDir, BossPatternData data, Notifier notifier)
    {
        float halfSpread = data.spreadAngle * 0.5f;
        float step = data.projectileCount > 1
            ? data.spreadAngle / (data.projectileCount - 1)
            : 0f;

        for (int i = 0; i < data.projectileCount; i++)
        {
            float angle = -halfSpread + step * i;
            var   dir   = Quaternion.Euler(0f, 0f, angle) * baseDir;

            var go   = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            var proj = go.GetComponent<BossProjectile>();
            proj.Initialize(owner, dir, data, notifier);

            if (i < data.projectileCount - 1)
                yield return new WaitForSeconds(data.fireInterval);
        }
    }
}
```

**검증:** 컴파일 오류 없음.

---

### Step 6 — `BossMonster` 생성 (Step 4, 5 완료 후)

**새 파일:** `Assets/Scripts/04.Game/01.Entity/Boss/BossMonster.cs`

```csharp
using UnityEngine;

public class BossMonster : Monster
{
    public BossMonsterData    BossData { get; }
    public BossMonsterView    BossView { get; }
    public SpatialGrid<IUnit> UnitGrid { get; }
    public Notifier           Notifier { get; }
    public bool               IsEnraged { get; private set; }

    public override float Radius => BossData.radius;

    public  readonly BossFSM bossFSM;
    private float            originalSpeed;

    public BossMonster(BossMonsterView view,
                       BossMonsterData data,
                       SpatialGrid<IUnit> unitGrid,
                       ObstacleGrid obstacleGrid,
                       Notifier notifier)
        : base(view, BuildMonsterData(data), unitGrid, MonsterRole.Leader, obstacleGrid)
    {
        BossData      = data;
        BossView      = view;
        UnitGrid      = unitGrid;
        Notifier      = notifier;
        originalSpeed = data.moveSpeed;
        bossFSM       = new BossFSM(this, view);
    }

    /// <summary>BossFSM이 이동·추적·패턴을 모두 담당하므로 base.Update() 호출 없음.</summary>
    public new void Update()
    {
        bossFSM.Update(Time.deltaTime);
        CheckEnrage();
    }

    private void CheckEnrage()
    {
        if (IsEnraged || Health.CurrentHp > Health.MaxHp * BossData.enrageThreshold) return;
        IsEnraged = true;
        View.Movement.MoveSpeed = originalSpeed * BossData.enrageSpeedMultiplier;
    }

    private static MonsterData BuildMonsterData(BossMonsterData d)
    {
        // BossMonsterData → MonsterData 변환 (Monster 생성자 주입용)
        // 실제 구현 시 MonsterData 인스턴스를 new로 생성하거나
        // BossMonsterData에 MonsterData baseData 필드를 추가하는 것도 대안이다
        throw new System.NotImplementedException("BuildMonsterData 구현 필요");
    }
}
```

> `BossFSM`이 이동·추적·패턴 전부를 담당하므로 `MonsterLeaderFSM`은 사용하지 않는다.
> `BuildMonsterData`는 Monster 생성자에 최소한의 데이터만 주입하고, 실제 AI는 BossFSM이 처리한다.

**검증:** 컴파일 오류 없음.

---

### Step 7 — UI 컴포넌트 2종 생성 [내부 병렬: 두 파일 독립]

**새 파일:** `Assets/Scripts/04.Game/03.UI/Boss/BossHpBarView.cs`

```csharp
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHpBarView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image          iconImage;
    [SerializeField] private Image          hpFillImage;

    private BossMonster boundBoss;

    public void Bind(BossMonster boss)
    {
        if (boundBoss != null) Unbind();
        boundBoss = boss;

        nameText.text   = boss.BossData.displayName;
        iconImage.sprite = boss.BossData.icon;
        boss.Health.OnDamaged += OnDamaged;
        boss.Health.OnDeath   += OnDeath;

        gameObject.SetActive(true);
        Refresh(boss.Health);
    }

    private void Unbind()
    {
        if (boundBoss == null) return;
        boundBoss.Health.OnDamaged -= OnDamaged;
        boundBoss.Health.OnDeath   -= OnDeath;
        boundBoss = null;
    }

    private void OnDamaged(int _) => Refresh(boundBoss?.Health);
    private void OnDeath()        => Hide();

    private void Refresh(UnitHealth h)
    {
        if (h == null) return;
        hpFillImage.DOKill();
        hpFillImage.DOFillAmount((float)h.CurrentHp / h.MaxHp, 0.15f);
    }

    public void Hide()
    {
        Unbind();
        gameObject.SetActive(false);
    }
}
```

**새 파일:** `Assets/Scripts/04.Game/03.UI/Boss/BossWarningView.cs`

```csharp
using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossWarningView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private Image          bossIcon;
    [SerializeField] private CanvasGroup    canvasGroup;

    public void Show(string bossName, Sprite icon, float duration, Action onComplete)
    {
        bossNameText.text = bossName;
        bossIcon.sprite   = icon;
        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;

        DOTween.Sequence()
            .Append(canvasGroup.DOFade(1f, 0.3f))
            .AppendInterval(Mathf.Max(0f, duration - 0.6f))
            .Append(canvasGroup.DOFade(0f, 0.3f))
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            });
    }
}
```

**검증:** 컴파일 오류 없음.

---

### Step 8 — `BossSpawnSystem` 생성 (Step 6, 7 완료 후)

**새 파일:** `Assets/Scripts/04.Game/02.System/Boss/BossSpawnSystem.cs`

```csharp
using System;
using UnityEngine;

public class BossSpawnSystem
{
    private readonly BossMonsterData[]  bossPool;
    private readonly EntitySpawner      entitySpawner;
    private readonly BossWarningView    warningView;
    private readonly BossHpBarView      hpBarView;
    private readonly SpatialGrid<IUnit> unitGrid;
    private readonly ObstacleGrid       obstacleGrid;
    private readonly Transform          playerTransform;
    private readonly Notifier           notifier;

    private float elapsedTime;
    private float respawnTimer;
    private BossMonster activeBoss;

    public event Action<BossMonster> OnBossSpawned;
    public event Action<BossMonster> OnBossDied;

    private const float SpawnTime    = 180f;
    private const float RespawnDelay = 240f;
    private const float WarnDuration = 2.5f;
    private const float SpawnOffset  = 15f; // 플레이어로부터 스폰 거리

    public BossSpawnSystem(BossMonsterData[] bossPool,
                           EntitySpawner entitySpawner,
                           BossWarningView warningView,
                           BossHpBarView hpBarView,
                           SpatialGrid<IUnit> unitGrid,
                           ObstacleGrid obstacleGrid,
                           Transform playerTransform,
                           Notifier notifier)
    {
        this.bossPool        = bossPool;
        this.entitySpawner   = entitySpawner;
        this.warningView     = warningView;
        this.hpBarView       = hpBarView;
        this.unitGrid        = unitGrid;
        this.obstacleGrid    = obstacleGrid;
        this.playerTransform = playerTransform;
        this.notifier        = notifier;
        respawnTimer         = -1f; // 첫 스폰은 SpawnTime 후
    }

    public void Update(float deltaTime)
    {
        if (activeBoss != null) return;

        elapsedTime  += deltaTime;
        respawnTimer -= deltaTime;

        if (elapsedTime >= SpawnTime && respawnTimer <= 0f)
            StartSpawnSequence();
    }

    private void StartSpawnSequence()
    {
        respawnTimer = float.MaxValue; // 중복 트리거 방지
        var data = bossPool[UnityEngine.Random.Range(0, bossPool.Length)];
        warningView.Show(data.displayName, data.icon, WarnDuration, () => SpawnBoss(data));
    }

    private void SpawnBoss(BossMonsterData data)
    {
        var spawnPos = FindSpawnPosition();
        var go       = UnityEngine.Object.Instantiate(data.viewPrefab, spawnPos, Quaternion.identity);
        var view     = go.GetComponent<BossMonsterView>();

        activeBoss   = new BossMonster(view, data, unitGrid, obstacleGrid, notifier);
        activeBoss.Health.OnDeath += () => OnBossDefeated(activeBoss);

        entitySpawner.RegisterBoss(activeBoss);
        OnBossSpawned?.Invoke(activeBoss);
        hpBarView.Bind(activeBoss);
    }

    private void OnBossDefeated(BossMonster boss)
    {
        entitySpawner.UnregisterBoss(boss);
        OnBossDied?.Invoke(boss);
        activeBoss   = null;
        respawnTimer = RespawnDelay;
    }

    private Vector2 FindSpawnPosition()
    {
        var center = (Vector2)playerTransform.position;
        for (int i = 0; i < 20; i++)
        {
            var dir = UnityEngine.Random.insideUnitCircle.normalized;
            var pos = center + dir * SpawnOffset;
            if (obstacleGrid.IsWalkable(pos)) return pos;
        }
        return center + Vector2.right * SpawnOffset;
    }
}
```

**수정 파일:** `EntitySpawner.cs` — `RegisterBoss` / `UnregisterBoss` 추가

```csharp
private BossMonster activeBoss;

public void RegisterBoss(BossMonster boss)
{
    activeBoss = boss;
    OnMonsterSpawned?.Invoke(boss); // CombatSystem 등록
}

public void UnregisterBoss(BossMonster boss)
{
    activeBoss = null;
    OnMonsterDespawned?.Invoke(boss);
}

// Update()에서 activeBoss 업데이트 추가
public void Update(float deltaTime)
{
    // 기존 스탠드얼론 몬스터 업데이트
    foreach (var m in activeMonsters) { ... }

    // 보스 업데이트
    activeBoss?.Update();
}
```

**검증:** 컴파일 오류 없음.

---

### Step 9 — `GameController` + `GameLoop` 통합 (Step 8 완료 후)

**수정 파일:** `GameController.cs`

```csharp
private readonly BossSpawnSystem bossSpawnSystem;

// 생성자 파라미터 추가
public GameController(..., BossMonsterData[] bossPool,
                          BossWarningView warningView, BossHpBarView hpBarView)
{
    // 기존 코드 ...
    bossSpawnSystem = new BossSpawnSystem(
        bossPool, entitySpawner, warningView, hpBarView,
        unitGrid, obstacleGrid, player.Transform, notifier);

    bossSpawnSystem.OnBossSpawned += boss => combatSystem.RegisterUnit(boss);
    bossSpawnSystem.OnBossDied    += boss => combatSystem.UnregisterUnit(boss);
}

public void Update()
{
    // 기존 코드 ...
    bossSpawnSystem.Update(Time.deltaTime);
}
```

**수정 파일:** `GameLoop.cs`

```csharp
[SerializeField] private BossMonsterData[] bossPool;
[SerializeField] private BossWarningView   bossWarningView;
[SerializeField] private BossHpBarView     bossHpBarView;

// GameController 생성자 호출 시 위 3개 파라미터 추가 전달
```

**검증:** Play Mode 진입, 컴파일 오류 없음.

---

### Step 10 — 프리팹 & ScriptableObject 에셋 세팅

1. **ZoneIndicator 프리팹 4종 생성**
   - `CircleIndicator.prefab` — Sprite: 원형 반투명 스프라이트, `ZoneIndicatorView` 컴포넌트
   - `CrossIndicator.prefab` — Sprite: 십자형
   - `XIndicator.prefab` — Sprite: X자형
   - `LineIndicator.prefab` — Sprite: 직사각형 (돌진 경로)
   - 저장 위치: `Assets/Prefabs/Boss/BossIndicators/`

2. **BossPatternData 에셋 7개 생성** (`Assets/ScriptableObjects/Patterns/`)
   - `P1_TrackingZone.asset` / `P2_Charge.asset` / ... / `P7_SummonMinions.asset` 수치 입력

3. **BossMonsterData 에셋 2개 생성** (`Assets/ScriptableObjects/`)
   - `BossMonsterData_Monk.asset` — YellowMonk 스탯 + P1·P3·P5·P7 패턴 배열
   - `BossMonsterData_Pawn.asset` — YellowPawn 스탯 + P2·P4·P6·P7 패턴 배열

4. **Boss View 프리팹 2종 생성** (`Assets/Prefabs/Boss/`)
   - `YellowMonkBossView.prefab` — `BossMonsterView` 컴포넌트 + 인디케이터 레퍼런스
   - `YellowPawnBossView.prefab`

5. **PlayPage UI 설정**
   - `BossHpBarView` GO를 PlayPage 캔버스 상단에 배치, 비활성화
   - `BossWarningView` GO를 PlayPage 중앙에 배치, 비활성화

6. **GameLoop SerializeField 세팅**
   - `bossPool` 배열에 Monk/Pawn 에셋 할당
   - `bossWarningView` / `bossHpBarView` 레퍼런스 연결

---

## 검증 체크리스트

### Step 1 — ScriptableObject
- [ ] 메뉴 `Data/BossMonsterData`, `Data/BossPatternData` 에셋 생성 가능
- [ ] 인스펙터에서 수치 편집 확인

### Step 2 — ZoneIndicatorView / BossProjectile
- [ ] `ZoneIndicatorView.Show()` → 게임 뷰에 반투명 노란 스프라이트 표시
- [ ] `FlashActive()` → 0.3초 DOTween 빨간색 전환
- [ ] `Hide()` → DOFade 0 후 비활성화

### Step 3 — 패턴 클래스
- [ ] 컴파일 오류 없음
- [ ] `CrossZonePattern.Activate()` 호출 시 SpatialGrid 쿼리 후 데미지 처리

### Step 4 — BossTrigger / BossFSM / States
- [ ] `BossIdleState` → `BossChaseState` (Detected) 전이 확인
- [ ] `BossChaseState` 플레이어 추적 이동 동작 확인
- [ ] 쿨다운 완료 패턴 있으면 `PatternReady` 발화 → `BossPatternCastState` 진입
- [ ] `BossPatternCastState` Warning → Active 타이머 정상 전환
- [ ] `PatternComplete` 후 `BossChaseState` 복귀
- [ ] HP 0 → `Die` 발화 → `BossDeadState` 진입

### Step 5 — BossMonsterView
- [ ] `ShowIndicator()` → 해당 타입 인디케이터 활성화
- [ ] `StartCharge()` Coroutine이 duration 후 완료 콜백 호출
- [ ] `FireProjectiles()` Coroutine이 fireInterval 간격으로 3발 순차 발사

### Step 6 — BossMonster
- [ ] `BossFSM.Update()` 매 프레임 호출 확인
- [ ] `BossIdleState` → `BossChaseState` → `BossPatternCastState` 전이 동작 확인
- [ ] HP 50% 이하 → `IsEnraged = true`, 이동속도 증가

### Step 7 — UI
- [ ] `BossHpBarView.Bind(boss)` 호출 시 이름·아이콘·HP 바 표시
- [ ] 보스 피격 시 HP 바 갱신 (DOTween)
- [ ] `BossWarningView.Show()` → FadeIn 0.3s → 대기 → FadeOut 0.3s

### Step 8 — BossSpawnSystem
- [ ] Play Mode 180초 후 경고 UI 표시
- [ ] 경고 2.5초 후 보스 스폰
- [ ] 보스 사망 후 `hpBarView` 비활성화, `respawnTimer = 240` 설정
- [ ] 240초 후 재스폰 정상 동작

### Step 9 — GameController 통합
- [ ] 기존 일반 몬스터 동작 영향 없음
- [ ] 보스가 CombatSystem에 등록되어 플레이어·스쿼드와 자동 교전

### Step 10 — 에셋 세팅
- [ ] Play Mode에서 보스 패턴 예고 인디케이터 가시적으로 표시
- [ ] 패턴 데미지 적용 확인 (Player HP 감소)
- [ ] 보스 사망 후 HP 바 숨김

---

## 작업 분류

| Step | 내용 | 선행 조건 | 병렬 여부 |
|------|------|----------|----------|
| Step 1 | ScriptableObject 클래스 2종 | 없음 | [병렬 가능] Step 2와 |
| Step 2 | ZoneIndicatorView / BossProjectile | 없음 | [병렬 가능] Step 1과 |
| Step 3 | IBossPattern + 패턴 7종 (P7 SummonMinions 포함) | Step 1 완료 | |
| Step 4 | BossTrigger + BossFSM + State 4종 | Step 3 완료 | |
| Step 5 | BossMonsterView | Step 2, 4 완료 | |
| Step 6 | BossMonster | Step 4, 5 완료 | |
| Step 7 | BossHpBarView / BossWarningView | Step 6 완료 | [내부 병렬] |
| Step 8 | BossSpawnSystem + EntitySpawner 수정 | Step 6, 7 완료 | |
| Step 9 | GameController / GameLoop 통합 | Step 8 완료 | |
| Step 10 | 프리팹 & 에셋 세팅 | Step 9 완료 | |

---

## 관련 파일

| 파일 | 구분 | 역할 |
|------|------|------|
| `ScriptableObjects/BossMonsterData.cs` | 신규 | 보스 기획 데이터 |
| `ScriptableObjects/BossPatternData.cs` | 신규 | 패턴 파라미터 |
| `01.Entity/Boss/BossMonster.cs` | 신규 | 보스 Presenter |
| `01.Entity/Boss/BossMonsterView.cs` | 신규 | 보스 View + Coroutine |
| `01.Entity/Boss/BossFSM.cs` | 신규 | 보스 전용 FSM |
| `01.Entity/Boss/States/BossIdleState.cs` | 신규 | 스폰 직후 대기 상태 |
| `01.Entity/Boss/States/BossChaseState.cs` | 신규 | 추적 이동 + 패턴 쿨다운 관리 |
| `01.Entity/Boss/States/BossPatternCastState.cs` | 신규 | Warning→Active 타이머 |
| `01.Entity/Boss/States/BossDeadState.cs` | 신규 | 사망 연출 |
| `01.Entity/Boss/ZoneIndicatorView.cs` | 신규 | 장판 예고 프리팹 컴포넌트 |
| `01.Entity/Boss/BossProjectile.cs` | 신규 | P6 투사체 |
| `01.Entity/Boss/Patterns/IBossPattern.cs` | 신규 | 패턴 인터페이스 |
| `01.Entity/Boss/Patterns/TrackingZonePattern.cs` | 신규 | P1 |
| `01.Entity/Boss/Patterns/ChargePattern.cs` | 신규 | P2 |
| `01.Entity/Boss/Patterns/CrossZonePattern.cs` | 신규 | P3 |
| `01.Entity/Boss/Patterns/XZonePattern.cs` | 신규 | P4 |
| `01.Entity/Boss/Patterns/CurseMarkPattern.cs` | 신규 | P5 |
| `01.Entity/Boss/Patterns/ProjectileBarragePattern.cs` | 신규 | P6 |
| `01.Entity/Boss/Patterns/SummonMinionsPattern.cs` | 신규 | P7 |
| `02.System/Boss/BossSpawnSystem.cs` | 신규 | 스폰 타이머 + 경고 연동 |
| `02.System/Entity/EntitySpawner.cs` | 수정 | RegisterBoss / UnregisterBoss 추가 |
| `02.System/Game/GameController.cs` | 수정 | BossSpawnSystem 통합 |
| `03.UI/Boss/BossHpBarView.cs` | 신규 | PlayPage 상단 HP 바 |
| `03.UI/Boss/BossWarningView.cs` | 신규 | 스폰 경고 오버레이 |
| `GameLoop.cs` | 수정 | bossPool / UI 레퍼런스 SerializeField 추가 |
