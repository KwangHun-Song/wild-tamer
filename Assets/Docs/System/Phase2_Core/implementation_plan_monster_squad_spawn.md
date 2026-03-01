# 몬스터 스쿼드 & 스폰 시스템 구현 계획

## 개요

같은 종류의 몬스터 N마리가 스쿼드를 이루어 행동하는 시스템과,
카메라 밖에서 스쿼드를 자동 생성/제거하는 스폰 시스템을 구현한다.

### 핵심 기능 요약

| 기능 | 설명 |
|------|------|
| 몬스터 스쿼드 | 같은 종류 1~12마리, 리더 1명 + 팔로워 N명 |
| 리더 AI | Wander(배회) ↔ Chase(추적) → Attack FSM |
| 팔로워 행동 | FlockBehavior로 리더 추종 (독립 AI 없음) |
| 리더 승계 | 리더 사망 시 살아있는 팔로워가 리더 역할 인수 |
| 자동 스폰 | 카메라 외곽에서 주기적으로 스쿼드 생성 |
| 자동 디스폰 | 플레이어와 너무 멀어지면 스쿼드 전체 제거 |
| 수량 제한 | 최소/최대 활성 스쿼드 수 지정 |

---

## 아키텍처 개요

### 새 파일

| 클래스/인터페이스 | 경로 | 역할 |
|------------------|------|------|
| `IMonsterBehavior` | `01.Entity/Monster/IMonsterBehavior.cs` | AI 동작 인터페이스 (SetUp/Update) |
| `MonsterWanderState` | `01.Entity/Monster/States/MonsterWanderState.cs` | 리더 배회 상태 (얇은 Shell) |
| `MonsterLeaderAI` | `01.Entity/Monster/MonsterLeaderAI.cs` | 리더 전용 AI (Wander/Chase/Attack FSM) |
| `MonsterSquad` | `02.System/Squad/MonsterSquad.cs` | 스쿼드 관리 (리더+팔로워, 승계, FlockBehavior) |
| `MonsterSquadSpawner` | `02.System/Entity/MonsterSquadSpawner.cs` | 자동 스폰/디스폰 시스템 |

### 수정 파일

| 파일 | 변경 내용 |
|------|----------|
| `FlockBehavior.cs` | `SquadMember` → `IUnit` 타입으로 변경 (Monster 재사용 가능) |
| `Squad.cs` | FlockBehavior 타입 변경 반영 (#if UNITY_EDITOR 포함) |
| `MonsterAI.cs` | `IMonsterBehavior` 인터페이스 구현 추가 |
| `Monster.cs` | `IMonsterBehavior` 기반 AI + `MonsterRole` enum 지원 |
| `EntitySpawner.cs` | `SpawnMonsterSquad` / `DespawnMonsterSquad` 추가 |
| `GameController.cs` | `MonsterSquadSpawner` 통합, 생성자에 Camera + spawnTable 파라미터 추가 |

---

## 핵심 설계 결정

### FlockBehavior 타입 변경 (IUnit)

현재 FlockBehavior는 `SquadMember`를 직접 참조한다.
`Monster : Character : IUnit`도 `Transform.position`만 있으면 FlockBehavior를 그대로 쓸 수 있으므로,
파라미터 타입을 `IUnit` / `IEnumerable<IUnit>`으로 변경한다.

C# `IEnumerable<T>`는 공변(covariant, `out T`)이므로:
- `IReadOnlyList<SquadMember>` → `IEnumerable<IUnit>` 암묵 변환 가능
- Squad.cs 호출부 수정 불필요

### 스탠드얼론 vs 스쿼드 몬스터 업데이트 분리

스쿼드 몬스터는 `entitySpawner.activeMonsters`에 추가하지 않는다.
- CombatSystem 등록은 `OnMonsterSpawned` 이벤트로 처리 (activeMonsters와 무관)
- `entitySpawner.Update()` → 스탠드얼론 몬스터만 AI/Combat 업데이트
- `MonsterSquad.Update()` → 스쿼드 멤버 FlockBehavior + 리더 AI + Combat.Tick 담당
- `DespawnMonster()` → `activeMonsters.Remove()`가 no-op여도 이벤트·풀 반환은 정상 동작

### 팔로워 AI 없음

팔로워는 `MonsterRole.Follower`로 생성, `behavior = null`.
이동은 `MonsterSquad.Update()` 내 FlockBehavior가 전담.
CombatSystem이 전투(피격/쿨다운)를 담당하므로 팔로워 전용 Attack AI는 Phase 2 범위 외.

---

## 단계별 구현 순서

### Step 1 — FlockBehavior `IUnit` 타입 변경 [병렬 가능: Step 2와 동시 진행]

**수정 파일:** `Assets/Scripts/04.Game/01.Entity/Squad/FlockBehavior.cs`

변경 요점 (전체 파일):
- 필드 `List<SquadMember> neighborsCache` → `List<IUnit> neighborsCache`
- public 메서드 `SquadMember self` → `IUnit self`
- public 메서드 `IReadOnlyList<SquadMember> neighbors` → `IEnumerable<IUnit> neighbors`
- private 헬퍼 메서드 파라미터 동일하게 변경

```csharp
private readonly List<IUnit> neighborsCache = new();

public Vector2 CalculateDirection(
    IUnit self,
    IEnumerable<IUnit> neighbors,
    Transform leader,
    ObstacleGrid obstacleGrid)
{
    neighborsCache.Clear();
    var selfPos2D = (Vector2)self.Transform.position;
    foreach (var neighbor in neighbors)
    {
        if (neighbor == self) continue;
        float dist = Vector2.Distance(selfPos2D, (Vector2)neighbor.Transform.position);
        if (dist <= NeighborRadius)
            neighborsCache.Add(neighbor);
    }
    // 이하 기존 로직과 동일
    ...
}

private Vector2 CalculateCohesion(IUnit self, List<IUnit> neighbors)  { ... }
private Vector2 CalculateSeparation(IUnit self, List<IUnit> neighbors) { ... }
private Vector2 CalculateFollow(IUnit self, Transform leader)          { ... }
private Vector2 CalculateAlignment(IUnit self, List<IUnit> neighbors)  { ... }
private Vector2 CalculateAvoidance(IUnit self, ObstacleGrid grid)      { ... }

#if UNITY_EDITOR
public FlockDebugData ComputeDebugData(
    IUnit self,
    IEnumerable<IUnit> neighbors,
    Transform leader,
    ObstacleGrid obstacleGrid)
{ ... }
#endif
```

**수정 파일:** `Assets/Scripts/04.Game/02.System/Squad/Squad.cs`

`#if UNITY_EDITOR` 블록의 `ComputeDebugData` 호출 파라미터 타입만 맞춰 확인:
```csharp
// members는 IReadOnlyList<SquadMember> → IEnumerable<IUnit> 공변 변환으로 그대로 전달 가능
var direction = flock.CalculateDirection(member, members, leader, obstacleGrid);
#if UNITY_EDITOR
member.SetFlockDebug(flock.ComputeDebugData(member, members, leader, obstacleGrid));
#endif
```
> 대부분 수정 불필요. 컴파일 오류만 없으면 된다.

---

### Step 2 — `IMonsterBehavior` + `Monster` 리팩토링 [병렬 가능: Step 1과 동시 진행]

**새 파일:** `Assets/Scripts/04.Game/01.Entity/Monster/IMonsterBehavior.cs`

```csharp
public interface IMonsterBehavior
{
    void SetUp();
    void Update();
}
```

**수정 파일:** `Assets/Scripts/04.Game/01.Entity/Monster/MonsterAI.cs`

`IMonsterBehavior` 구현 선언 추가. 기존 `SetUp()`은 StateMachine 베이스에 있으므로 명시적 구현:

```csharp
public class MonsterAI : StateMachine<Monster, MonsterTrigger>, IMonsterBehavior
{
    void IMonsterBehavior.SetUp()    => SetUp();
    void IMonsterBehavior.Update()   => Update(); // 기존 new Update()
    // 나머지 기존 코드 그대로
}
```

**수정 파일:** `Assets/Scripts/04.Game/01.Entity/Monster/Monster.cs`

```csharp
public enum MonsterRole { Standalone, Leader, Follower }

public class Monster : Character
{
    public override UnitTeam Team => UnitTeam.Enemy;
    public MonsterData Data { get; }

    private readonly MonsterView monsterView;
    private IMonsterBehavior behavior;

    public event Action<Vector2> OnMoveRequested;

    /// <summary>스탠드얼론 몬스터용 (기존 코드 호환).</summary>
    public Monster(MonsterView view, MonsterData data, SpatialGrid<IUnit> unitGrid)
        : this(view, data, unitGrid, MonsterRole.Standalone) { }

    public Monster(MonsterView view, MonsterData data, SpatialGrid<IUnit> unitGrid, MonsterRole role)
        : base(view, CreateCombat(data))
    {
        Data = data;
        monsterView = view;
        Health.Initialize(data.maxHp);
        view.Movement.MoveSpeed = data.moveSpeed;
        Health.OnDamaged += OnHealthDamaged;
        Health.OnDeath   += OnHealthDeath;
        view.Subscribe(this);

        behavior = role switch
        {
            MonsterRole.Leader   => new MonsterLeaderAI(this, unitGrid),
            MonsterRole.Follower => null,
            _                    => new MonsterAI(this, unitGrid),
        };
        behavior?.SetUp();
    }

    /// <summary>리더 승계 시 호출. 팔로워에게 리더 AI를 부여한다.</summary>
    public void PromoteToLeader(SpatialGrid<IUnit> unitGrid)
    {
        behavior = new MonsterLeaderAI(this, unitGrid);
        behavior.SetUp();
    }

    private void OnHealthDamaged(int _) => monsterView.PlayHitEffect();
    private void OnHealthDeath()        => monsterView.PlayDeathEffect();

    public void Cleanup()
    {
        Health.OnDamaged -= OnHealthDamaged;
        Health.OnDeath   -= OnHealthDeath;
    }

    public void Update() => behavior?.Update();

    public void Move(Vector2 direction) => OnMoveRequested?.Invoke(direction);

    public void PlayTamingEffect() => monsterView.PlayTamingEffect();

    private static UnitCombat CreateCombat(MonsterData d)
        => new UnitCombat(d.attackDamage, d.attackRange, d.detectionRange, d.attackCooldown);
}
```

---

### Step 3 — `MonsterWanderState` 생성 (Step 2 완료 후)

**새 파일:** `Assets/Scripts/04.Game/01.Entity/Monster/States/MonsterWanderState.cs`

배회 방향·타이머 상태는 `MonsterLeaderAI`가 관리한다. 기존 State 클래스 패턴(얇게) 유지.

```csharp
using FiniteStateMachine;

public class MonsterWanderState : State<Monster, MonsterTrigger>
{
    public override void OnEnter() { /* TODO: idle 애니메이션 트리거 */ }
}
```

---

### Step 4 — `MonsterLeaderAI` 생성 (Step 3 완료 후)

**새 파일:** `Assets/Scripts/04.Game/01.Entity/Monster/MonsterLeaderAI.cs`

```csharp
using FiniteStateMachine;
using UnityEngine;

public class MonsterLeaderAI : StateMachine<Monster, MonsterTrigger>, IMonsterBehavior
{
    public SpatialGrid<IUnit> UnitGrid { get; }

    private readonly MonsterWanderState wander = new();
    private readonly MonsterChaseState  chase  = new();
    private readonly MonsterAttackState attack = new();

    private Vector2 wanderDirection;
    private float   wanderTimer;
    public float WanderChangeInterval = 3f;

    protected override State<Monster, MonsterTrigger> InitialState => wander;

    protected override State<Monster, MonsterTrigger>[] States
        => new State<Monster, MonsterTrigger>[] { wander, chase, attack };

    protected override StateTransition<Monster, MonsterTrigger>[] Transitions => new[]
    {
        StateTransition<Monster, MonsterTrigger>.Generate(wander, chase,  MonsterTrigger.DetectEnemy),
        StateTransition<Monster, MonsterTrigger>.Generate(chase,  wander, MonsterTrigger.LoseEnemy),
        StateTransition<Monster, MonsterTrigger>.Generate(chase,  attack, MonsterTrigger.InAttackRange),
        StateTransition<Monster, MonsterTrigger>.Generate(attack, chase,  MonsterTrigger.OutOfAttackRange),
    };

    public MonsterLeaderAI(Monster owner, SpatialGrid<IUnit> unitGrid) : base(owner)
    {
        UnitGrid = unitGrid;
    }

    void IMonsterBehavior.SetUp()
    {
        SetUp();
        PickNewWanderDirection();
    }

    void IMonsterBehavior.Update() => Update();

    public new void Update()
    {
        var pos = (Vector2)Owner.Transform.position;

        switch (CurrentState)
        {
            case MonsterWanderState _:
                wanderTimer -= Time.deltaTime;
                if (wanderTimer <= 0f) PickNewWanderDirection();
                Owner.Move(wanderDirection);
                if (HasEnemyInRange(pos, Owner.Combat.DetectionRange))
                    ExecuteCommand(MonsterTrigger.DetectEnemy);
                break;

            case MonsterChaseState _:
                if (!HasEnemyInRange(pos, Owner.Combat.DetectionRange))
                {
                    Owner.Move(Vector2.zero);
                    ExecuteCommand(MonsterTrigger.LoseEnemy);
                }
                else if (HasEnemyInRange(pos, Owner.Combat.AttackRange))
                {
                    ExecuteCommand(MonsterTrigger.InAttackRange);
                }
                else
                {
                    var target = FindClosestEnemy(pos, Owner.Combat.DetectionRange);
                    if (target != null)
                        Owner.Move(((Vector2)target.Transform.position - pos).normalized);
                }
                break;

            case MonsterAttackState _:
                if (!HasEnemyInRange(pos, Owner.Combat.AttackRange))
                    ExecuteCommand(MonsterTrigger.OutOfAttackRange);
                else if (Owner.Combat.CanAttack)
                    Owner.Combat.ResetCooldown();
                break;
        }
    }

    private void PickNewWanderDirection()
    {
        wanderDirection = Random.insideUnitCircle.normalized;
        wanderTimer     = WanderChangeInterval;
    }

    private bool HasEnemyInRange(Vector2 pos, float range)
    {
        if (UnitGrid == null) return false;
        foreach (var u in UnitGrid.Query(pos, range))
            if (u.Team != Owner.Team && u.IsAlive) return true;
        return false;
    }

    private IUnit FindClosestEnemy(Vector2 pos, float range)
    {
        if (UnitGrid == null) return null;
        IUnit closest = null;
        float minDist = float.MaxValue;
        foreach (var u in UnitGrid.Query(pos, range))
        {
            if (u.Team == Owner.Team || !u.IsAlive) continue;
            float d = Vector2.Distance(pos, u.Transform.position);
            if (d < minDist) { minDist = d; closest = u; }
        }
        return closest;
    }
}
```

> `HasEnemyInRange` / `FindClosestEnemy`는 MonsterAI와 동일한 로직이다.
> 공통 추출(기반 클래스·정적 유틸)은 추후 리팩토링 시 진행한다.

---

### Step 5 — `MonsterSquad` 생성 (Step 1, 4 완료 후)

**새 파일:** `Assets/Scripts/04.Game/02.System/Squad/MonsterSquad.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MonsterSquad
{
    private readonly List<Monster> members = new();
    private Monster leader;
    private readonly SpatialGrid<IUnit> unitGrid;
    private readonly FlockBehavior flock = new();

    public float StopRadius = 0.6f; // 리더 근처 팔로워 정지 반경

    public Monster Leader                    => leader;
    public IReadOnlyList<Monster> Members    => members;
    public bool IsEmpty                      => members.Count == 0;
    public MonsterData Data                  { get; }

    public event Action<Monster> OnMemberDied;
    public event Action          OnSquadEmpty;

    public MonsterSquad(MonsterData data, SpatialGrid<IUnit> unitGrid)
    {
        Data          = data;
        this.unitGrid = unitGrid;
    }

    public void AddMember(Monster monster)
    {
        members.Add(monster);
        monster.Health.OnDeath += () => HandleMemberDeath(monster);
        if (leader == null) PromoteLeader(monster);
    }

    public void Update(ObstacleGrid obstacleGrid, float deltaTime)
    {
        if (leader == null || !leader.IsAlive) return;

        var leaderTf  = leader.Transform;
        Vector2 leaderPos = leaderTf.position;

        // 리더 AI 업데이트
        leader.Combat.Tick(deltaTime);
        leader.Update();

        // 팔로워 FlockBehavior 이동 (리더 제외, 생존 멤버만)
        // IReadOnlyList<Monster>는 IEnumerable<IUnit>으로 공변 변환 가능
        IEnumerable<IUnit> allAsUnits = members.Where(m => m.IsAlive);

        foreach (var follower in members)
        {
            if (follower == leader || !follower.IsAlive) continue;

            follower.Combat.Tick(deltaTime);

            if (Vector2.Distance((Vector2)follower.Transform.position, leaderPos) <= StopRadius)
            {
                follower.Move(Vector2.zero);
                continue;
            }

            var dir = flock.CalculateDirection(follower, allAsUnits, leaderTf, obstacleGrid);
            follower.Move(dir);
        }
    }

    private void HandleMemberDeath(Monster dead)
    {
        members.Remove(dead);
        OnMemberDied?.Invoke(dead);

        if (dead == leader)
        {
            leader = null;
            var next = members.FirstOrDefault(m => m.IsAlive);
            if (next != null) PromoteLeader(next);
        }

        if (members.Count == 0)
            OnSquadEmpty?.Invoke();
    }

    private void PromoteLeader(Monster monster)
    {
        leader = monster;
        monster.PromoteToLeader(unitGrid);
    }
}
```

---

### Step 6 — `EntitySpawner` 확장 (Step 5 완료 후)

**수정 파일:** `Assets/Scripts/04.Game/02.System/Entity/EntitySpawner.cs`

스쿼드 몬스터는 `activeMonsters`에 추가하지 않는다.
`OnMonsterSpawned` 이벤트만 발생시켜 CombatSystem에 등록한다.
Update/Tick은 `MonsterSquad.Update()`가 담당하므로 이중 업데이트를 방지한다.

```csharp
private readonly List<MonsterSquad> activeSquads = new();

public IReadOnlyList<MonsterSquad> ActiveSquads => activeSquads;

public event Action<MonsterSquad> OnSquadSpawned;
public event Action<MonsterSquad> OnSquadDespawned;

/// <summary>몬스터 스쿼드를 스폰한다. count는 1~12로 클램프된다.</summary>
public MonsterSquad SpawnMonsterSquad(
    MonsterData data,
    Vector2 position,
    int count,
    SpatialGrid<IUnit> unitGrid)
{
    count = Mathf.Clamp(count, 1, 12);
    var squad = new MonsterSquad(data, unitGrid);

    for (int i = 0; i < count; i++)
    {
        var offset   = i == 0 ? Vector2.zero : Random.insideUnitCircle * 1.5f;
        var spawnPos = position + offset;
        var role     = i == 0 ? MonsterRole.Leader : MonsterRole.Follower;

        var go      = Facade.Pool.Spawn(data.prefab);
        go.transform.position = spawnPos;
        var view    = go.GetComponent<MonsterView>();
        var monster = new Monster(view, data, unitGrid, role);

        squad.AddMember(monster);
        OnMonsterSpawned?.Invoke(monster); // CombatSystem 등록
    }

    // 스쿼드 멤버 사망 시 자동 디스폰
    squad.OnMemberDied += DespawnMonster;

    activeSquads.Add(squad);
    OnSquadSpawned?.Invoke(squad);
    return squad;
}

/// <summary>스쿼드 전체를 디스폰한다.</summary>
public void DespawnMonsterSquad(MonsterSquad squad)
{
    squad.OnMemberDied -= DespawnMonster;

    foreach (var m in squad.Members.ToList())
        DespawnMonster(m);

    activeSquads.Remove(squad);
    OnSquadDespawned?.Invoke(squad);
}
```

> `DespawnMonster(monster)` 내부의 `activeMonsters.Remove(monster)` 는
> 스쿼드 몬스터가 리스트에 없어 no-op이지만, 이벤트 발생 및 풀 반환은 정상 동작한다.

---

### Step 7 — `MonsterSquadSpawner` 생성 (Step 6 완료 후)

**새 파일:** `Assets/Scripts/04.Game/02.System/Entity/MonsterSquadSpawner.cs`

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 카메라 외곽에서 몬스터 스쿼드를 주기적으로 스폰하고,
/// 플레이어와 너무 멀어진 스쿼드를 자동 디스폰한다.
/// </summary>
public class MonsterSquadSpawner
{
    public int   MinSquadCount      = 3;
    public int   MaxSquadCount      = 8;
    public int   MinMembersPerSquad = 1;
    public int   MaxMembersPerSquad = 12;
    public float SpawnMargin        = 3f;   // 카메라 경계 밖 추가 여유 거리
    public float DespawnDistance    = 35f;  // 플레이어 기준 디스폰 반경
    public float SpawnInterval      = 8f;   // 스폰 시도 주기(초)

    private readonly EntitySpawner      entitySpawner;
    private readonly SpatialGrid<IUnit> unitGrid;
    private readonly ObstacleGrid       obstacleGrid;
    private readonly Transform          playerTransform;
    private readonly Camera             camera;
    private readonly MonsterData[]      spawnTable;

    private float spawnTimer;

    public MonsterSquadSpawner(
        EntitySpawner entitySpawner,
        SpatialGrid<IUnit> unitGrid,
        ObstacleGrid obstacleGrid,
        Transform playerTransform,
        Camera camera,
        MonsterData[] spawnTable)
    {
        this.entitySpawner   = entitySpawner;
        this.unitGrid        = unitGrid;
        this.obstacleGrid    = obstacleGrid;
        this.playerTransform = playerTransform;
        this.camera          = camera;
        this.spawnTable      = spawnTable;
        spawnTimer           = SpawnInterval;
    }

    /// <summary>GameController.Update()에서 매 프레임 호출한다.</summary>
    public void Update(float deltaTime)
    {
        // 1. 스쿼드 AI 및 FlockBehavior 업데이트
        foreach (var squad in entitySpawner.ActiveSquads)
            squad.Update(obstacleGrid, deltaTime);

        // 2. 원거리 스쿼드 자동 디스폰
        TryDespawnFarSquads();

        // 3. 스폰 주기 체크
        spawnTimer -= deltaTime;
        if (spawnTimer <= 0f)
        {
            TrySpawnSquad();
            spawnTimer = SpawnInterval;
        }
    }

    private void TrySpawnSquad()
    {
        if (entitySpawner.ActiveSquads.Count >= MaxSquadCount) return;
        if (spawnTable == null || spawnTable.Length == 0) return;

        var pos   = FindSpawnPositionOutsideCamera();
        var data  = spawnTable[Random.Range(0, spawnTable.Length)];
        var count = Random.Range(MinMembersPerSquad, MaxMembersPerSquad + 1);
        entitySpawner.SpawnMonsterSquad(data, pos, count, unitGrid);
    }

    private void TryDespawnFarSquads()
    {
        Vector2 playerPos = playerTransform.position;

        foreach (var squad in entitySpawner.ActiveSquads.ToList())
        {
            if (squad.Leader == null) continue;
            float dist = Vector2.Distance(playerPos, (Vector2)squad.Leader.Transform.position);
            if (dist > DespawnDistance)
                entitySpawner.DespawnMonsterSquad(squad);
        }
    }

    private Vector2 FindSpawnPositionOutsideCamera()
    {
        var   camPos = (Vector2)camera.transform.position;
        float halfH  = camera.orthographicSize + SpawnMargin;
        float halfW  = halfH * camera.aspect    + SpawnMargin;

        return Random.Range(0, 4) switch
        {
            0 => new Vector2(Random.Range(camPos.x - halfW, camPos.x + halfW), camPos.y + halfH), // 위
            1 => new Vector2(Random.Range(camPos.x - halfW, camPos.x + halfW), camPos.y - halfH), // 아래
            2 => new Vector2(camPos.x - halfW, Random.Range(camPos.y - halfH, camPos.y + halfH)), // 왼쪽
            _ => new Vector2(camPos.x + halfW, Random.Range(camPos.y - halfH, camPos.y + halfH)), // 오른쪽
        };
    }
}
```

---

### Step 8 — `GameController` 통합 (Step 7 완료 후)

**수정 파일:** `Assets/Scripts/04.Game/02.System/Game/GameController.cs`

```csharp
// 추가 필드
private readonly MonsterSquadSpawner squadSpawner;

// 생성자 파라미터 추가
public GameController(
    PlayerView playerView,
    PlayerInput playerInput,
    ObstacleGrid obstacleGrid,
    Camera gameCamera,              // 추가
    MonsterData[] monsterSpawnTable) // 추가
{
    // ... 기존 코드 ...

    squadSpawner = new MonsterSquadSpawner(
        entitySpawner,
        unitGrid,
        obstacleGrid,
        Player.Transform,
        gameCamera,
        monsterSpawnTable);
}

// Update() 수정
public void Update()
{
    if (Phase != GamePhase.Play) return;
    var dt = Time.deltaTime;

    // 1. 입력 → Player (기존)
    // 2. 부대 이동 (기존)
    Squad.Update(Player.Transform, obstacleGrid, dt);

    // 3. 스탠드얼론 몬스터 AI (기존)
    entitySpawner.Update(dt);

    // 4. 몬스터 스쿼드 스폰/디스폰/AI (신규)
    squadSpawner.Update(dt);

    // 5. 전투 (기존)
    combatSystem.Update();
}
```

GameLoop(MonoBehaviour) 에서 GameController 생성 시 `Camera.main`과
`MonsterData[]` 배열(SerializeField)을 넘겨주도록 수정한다.

---

## 검증 체크리스트

### Step 1 — FlockBehavior 타입 변경
- [ ] `CalculateDirection` 파라미터가 `IUnit` / `IEnumerable<IUnit>` 타입으로 변경됨
- [ ] `Squad.Update()` 기존 호출 컴파일 오류 없음
- [ ] `SquadMember.SetFlockDebug` 기존 디버그 흐름 정상 동작

### Step 2 — Monster 리팩토링
- [ ] `MonsterRole.Standalone` → `MonsterAI` 초기화 (기존 동작 유지)
- [ ] `MonsterRole.Leader`     → `MonsterLeaderAI` 초기화
- [ ] `MonsterRole.Follower`   → AI 없음 (`behavior == null`)
- [ ] `PromoteToLeader()` 호출 시 `MonsterLeaderAI` 부여됨

### Step 3~4 — MonsterLeaderAI
- [ ] Wander 상태: 랜덤 방향으로 이동, `WanderChangeInterval`마다 방향 변경
- [ ] 플레이어가 `DetectionRange` 이내 진입 → Chase 전환
- [ ] Chase 상태: 플레이어(적) 추적, `AttackRange` 이내 진입 → Attack 전환
- [ ] 플레이어가 `DetectionRange` 벗어남 → Wander 복귀, 이동 정지

### Step 5 — MonsterSquad
- [ ] 첫 번째 AddMember가 리더 역할을 가져감
- [ ] 팔로워가 FlockBehavior로 리더를 추종함
- [ ] 리더 사망 시 살아있는 팔로워가 자동 승계
- [ ] 전체 사망 시 `OnSquadEmpty` 발생

### Step 6 — EntitySpawner
- [ ] `SpawnMonsterSquad` 호출 시 `ActiveSquads`에 추가됨
- [ ] 스쿼드 몬스터는 `OnMonsterSpawned` 이벤트 → CombatSystem 등록됨
- [ ] `DespawnMonsterSquad` 호출 시 GO 풀 반환 + `OnMonsterDespawned` 이벤트 발생

### Step 7 — MonsterSquadSpawner
- [ ] Play Mode에서 카메라 영역 밖에 스쿼드가 생성됨
- [ ] `ActiveSquads.Count`가 `MaxSquadCount`를 초과하지 않음
- [ ] 리더가 `DespawnDistance` 초과 시 스쿼드 전체 디스폰됨
- [ ] `SpawnInterval` 주기로 스폰 시도됨

### Step 8 — GameController
- [ ] `GameLoop`에서 `Camera` / `MonsterData[]` 올바르게 전달됨
- [ ] 기존 스탠드얼론 Monster 동작 영향 없음
- [ ] Play Mode에서 스쿼드 자동 스폰 + 팔로워 리더 추종 확인

---

## 작업 분류

| Step | 내용 | 선행 조건 | 병렬 여부 |
|------|------|----------|----------|
| Step 1 | FlockBehavior IUnit 타입 변경 | 없음 | [병렬 가능] Step 2와 |
| Step 2 | IMonsterBehavior + Monster 리팩토링 | 없음 | [병렬 가능] Step 1과 |
| Step 3 | MonsterWanderState 생성 | Step 2 완료 | |
| Step 4 | MonsterLeaderAI 생성 | Step 3 완료 | |
| Step 5 | MonsterSquad 생성 | Step 1, 4 완료 | |
| Step 6 | EntitySpawner 확장 | Step 5 완료 | |
| Step 7 | MonsterSquadSpawner 생성 | Step 6 완료 | |
| Step 8 | GameController 통합 | Step 7 완료 | |

---

## 관련 파일

| 파일 | 구분 | 역할 |
|------|------|------|
| `01.Entity/Squad/FlockBehavior.cs` | 수정 | IUnit 타입으로 변경 |
| `02.System/Squad/Squad.cs` | 수정 | FlockBehavior 변경 반영 |
| `01.Entity/Monster/Monster.cs` | 수정 | IMonsterBehavior + MonsterRole |
| `01.Entity/Monster/MonsterAI.cs` | 수정 | IMonsterBehavior 구현 추가 |
| `01.Entity/Monster/IMonsterBehavior.cs` | 신규 | AI 동작 인터페이스 |
| `01.Entity/Monster/MonsterLeaderAI.cs` | 신규 | 리더 AI (Wander/Chase/Attack) |
| `01.Entity/Monster/States/MonsterWanderState.cs` | 신규 | 배회 상태 |
| `02.System/Squad/MonsterSquad.cs` | 신규 | 몬스터 스쿼드 관리 |
| `02.System/Entity/EntitySpawner.cs` | 수정 | MonsterSquad 스폰/디스폰 추가 |
| `02.System/Entity/MonsterSquadSpawner.cs` | 신규 | 자동 스폰 시스템 |
| `02.System/Game/GameController.cs` | 수정 | squadSpawner 통합 |
