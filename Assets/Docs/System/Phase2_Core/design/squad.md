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
| 행동 | MonsterStandaloneFSM / MonsterLeaderFSM | SquadMemberFSM + FlockBehavior |
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

    public void AddMember(SquadMember member) { ... }
    public void RemoveMember(SquadMember member) { ... }
    public void Clear() { ... }

    /// <summary>GameController.Update()에서 호출. 각 부대원 이동 방향을 계산하고 FSM을 구동한다.</summary>
    public void Update(Transform leader, ObstacleGrid obstacleGrid, float deltaTime) { ... }
}
```

---

## SquadMember (pure C#) : Character

Monster와 동일한 MonsterData를 보유하며, 팀이 Player다.
`SquadMemberFSM`을 소유하며, `SetMoveDirection()`으로 FlockBehavior 결과를 받고 `Update()`로 FSM을 구동한다.

```csharp
public class SquadMember : Character
{
    public override UnitTeam Team => UnitTeam.Player;
    public MonsterData Data { get; }
    public Vector2 DesiredMoveDirection { get; private set; }

    private readonly SquadMemberFSM fsm;

    public SquadMember(SquadMemberView view, MonsterData data, SpatialGrid<IUnit> unitGrid)
        : base(view, CreateCombat(data))
    {
        Data = data;
        Health.Initialize(data.maxHp);
        View.Movement.MoveSpeed = data.moveSpeed;
        fsm = new SquadMemberFSM(this, unitGrid);
        fsm.SetUp();
    }

    /// <summary>Squad.Update()에서 호출. FlockBehavior가 계산한 이동 방향을 전달한다.</summary>
    public void SetMoveDirection(Vector2 direction) => DesiredMoveDirection = direction;

    /// <summary>Squad.Update()에서 호출하여 FSM을 구동한다.</summary>
    public void Update() => fsm.Update();
}
```

---

## SquadMemberFSM / 상태 구조

Move 상태 없이 **Idle·Attack 모두에서 이동이 가능**하다.

| 상태 | 역할 |
|---|---|
| `SquadMemberIdleState` | FlockBehavior 방향 이동 + 적 감지 시 StartAttack |
| `SquadMemberAttackState` | 공격 + FlockBehavior 방향 이동 병행 |
| `SquadMemberDeadState` | 이동 정지, dead 애님 재생 |

트리거: `StartAttack`, `StopAttack`, `Die`

---

## SquadMemberView (MonoBehaviour) : CharacterView

이벤트 구독 없이 FSM States가 직접 호출하는 애님 API만 제공한다.
Editor에서 FlockBehavior 디버그 벡터를 Gizmo로 시각화한다.

---

## FlockBehavior (pure C#)

Boids 알고리즘(정렬·응집·분리)에 리더 추종과 장애물 회피를 결합한다. `Squad.Update()`에서 각 부대원의 이동 방향 계산에 사용한다.

```csharp
public class FlockBehavior
{
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

O(n²) 전수 탐색을 O(n) 근접 탐색으로 줄이는 공간 분할 자료구조. CombatSystem과 MonsterFSM이 공유한다.

```csharp
public class SpatialGrid<T> where T : class
{
    public SpatialGrid(float cellSize) { ... }
    public void Clear() { ... }
    public void Insert(T item, Vector2 position) { ... }
    public List<T> Query(Vector2 center, float radius) { ... }
}
```
