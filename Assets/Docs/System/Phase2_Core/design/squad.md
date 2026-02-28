# 2.2 군집 및 부대 시스템

> 상위 문서: [Phase 2 설계](../design.md)

테이밍에 성공한 몬스터가 아군 부대원(SquadMember)으로 전환되어 플레이어를 추종한다. Boids 기반 군집 이동(`FlockBehavior`)으로 NavMesh 없이 자연스러운 군집 대형을 유지한다.

---

## SquadMember와 Monster의 관계

SquadMember와 Monster는 동일한 `MonsterData`를 공유하며, 팀과 행동 로직만 다르다.

| | Monster | SquadMember |
|---|---|---|
| 데이터 | MonsterData | MonsterData (동일) |
| 팀 | Enemy | Player |
| 행동 | MonsterAI (자율) | FlockBehavior (추종) |
| View | MonsterView | SquadMemberView |

---

## Squad (pure C#)

부대원 목록을 관리한다. `OnMemberAdded` / `OnMemberRemoved` 이벤트를 통해 GameController가 CombatSystem에 자동 등록할 수 있도록 한다.

```csharp
public class Squad
{
    private readonly List<SquadMember> members = new();

    public IReadOnlyList<SquadMember> Members => members;
    public int Count => members.Count;

    public event Action<SquadMember> OnMemberAdded;
    public event Action<SquadMember> OnMemberRemoved;

    public void AddMember(SquadMember member)
    {
        members.Add(member);
        OnMemberAdded?.Invoke(member);
    }

    public void RemoveMember(SquadMember member)
    {
        members.Remove(member);
        OnMemberRemoved?.Invoke(member);
    }

    public void Clear()
    {
        foreach (var m in members.ToList())
            RemoveMember(m);
    }

    /// <summary>GameController.Update()에서 호출. 각 부대원 이동 방향을 계산하여 적용한다.</summary>
    public void Update(Transform leader, ObstacleGrid obstacleGrid, float deltaTime) { ... }
}
```

---

## SquadMember (pure C#) : Character

Monster와 동일한 MonsterData를 보유하며, 팀이 Player다.

```csharp
public class SquadMember : Character
{
    public override UnitTeam Team => UnitTeam.Player;
    public MonsterData Data { get; }

    public event Action<Vector2> OnMoveRequested;

    public SquadMember(SquadMemberView view, MonsterData data)
        : base(view, CreateCombat(data))
    {
        Data = data;
        view.Subscribe(this);

        view.Health.Initialize(data.maxHp);
        view.Movement.MoveSpeed = data.moveSpeed;
    }

    public void Move(Vector2 direction) => OnMoveRequested?.Invoke(direction);

    private static UnitCombat CreateCombat(MonsterData d)
        => new(d.attackDamage, d.attackRange, d.detectionRange, d.attackCooldown);
}
```

---

## SquadMemberView (MonoBehaviour) : CharacterView

SquadMember 이벤트를 구독하여 Movement를 구동한다.

```csharp
public class SquadMemberView : CharacterView
{
    public void Subscribe(SquadMember member)
    {
        member.OnMoveRequested += direction => Movement.Move(direction);
    }
}
```

---

## FlockBehavior (pure C#)

Boids 알고리즘(정렬·응집·분리)에 리더 추종과 장애물 회피를 결합한다. `Squad.Update()`에서 각 부대원의 이동 방향 계산에 사용한다.

```csharp
public class FlockBehavior
{
    public float AlignmentWeight;
    public float CohesionWeight;
    public float SeparationWeight;
    public float FollowWeight;
    public float AvoidanceWeight;
    public float NeighborRadius;

    public Vector2 CalculateDirection(
        SquadMember self,
        IReadOnlyList<SquadMember> neighbors,
        Transform leader,
        ObstacleGrid obstacleGrid) { ... }
}
```

| 행동 | 설명 |
|------|------|
| Alignment (정렬) | 이웃 부대원들의 이동 방향 평균에 맞춤 |
| Cohesion (응집) | 이웃 부대원들의 중심 방향으로 이동 |
| Separation (분리) | 이웃 부대원과 일정 거리 유지 |
| Follow (추종) | 리더(플레이어) 방향으로 이동 |
| Avoidance (회피) | ObstacleGrid 기반 장애물 회피 |

---

## SpatialGrid (pure C#)

O(n²) 전수 탐색을 O(n) 근접 탐색으로 줄이는 공간 분할 자료구조. CombatSystem과 MonsterAI가 공유한다.

```csharp
public class SpatialGrid<T> where T : class
{
    private readonly float cellSize;
    private readonly Dictionary<Vector2Int, List<T>> cells = new();

    public SpatialGrid(float cellSize) { ... }
    public void Clear() { ... }
    public void Insert(T item, Vector2 position) { ... }
    public List<T> Query(Vector2 center, float radius) { ... }
}
```
