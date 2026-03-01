# 군집 및 부대 시스템 - 코드 리뷰

## 리뷰 대상 파일

| 파일 | 역할 | 유형 |
|------|------|------|
| `Scripts/04.Game/02.System/Squad/Squad.cs` | 부대 관리 | pure C# |
| `Scripts/04.Game/01.Entity/Squad/SquadMember.cs` | 부대원 Presenter | pure C# |
| `Scripts/04.Game/01.Entity/Squad/SquadMemberView.cs` | 부대원 View | MonoBehaviour |
| `Scripts/04.Game/01.Entity/Squad/FlockBehavior.cs` | 군집 이동 계산 | pure C# |
| `Scripts/04.Game/02.System/Spatial/SpatialGrid.cs` | 공간 분할 자료구조 | pure C# |

참조 설계 문서: `design/squad.md`, `design.md`, `concept_design_review.md`

---

## 긍정적인 점

- **설계 문서와의 높은 일관성**: 5개 파일 모두 `design/squad.md`의 클래스 명세(시그니처, 상속 구조, 멤버)와 정확히 일치한다. 설계 의도가 구현에 충실히 반영되었다.
- **OnMemberAdded/OnMemberRemoved 이벤트 구현**: 설계 리뷰에서 추가 요구된 이벤트가 `Squad.cs`에 올바르게 구현되어 있으며, `AddMember`/`RemoveMember` 호출 시 정확히 발생한다.
- **MVP 패턴 준수**: `SquadMember`(Presenter)는 pure C#이며, 이벤트(`OnMoveRequested`)를 통해 `SquadMemberView`(MonoBehaviour)와 통신한다. View가 `Subscribe()`로 이벤트를 구독하는 패턴이 일관된다.
- **MonsterData 보유**: `SquadMember`가 `MonsterData Data` 프로퍼티를 보유하여 Monster와 동일 데이터를 공유하는 설계가 정확히 구현되었다.
- **Squad.Clear()의 안전한 구현**: 컬렉션 순회 중 수정 문제를 `members.ToList()`로 복사본을 만들어 안전하게 처리하며, 각 멤버에 대해 `RemoveMember()`를 호출하여 `OnMemberRemoved` 이벤트도 정상 발생한다.
- **SpatialGrid 제네릭 설계**: `SpatialGrid<T>`로 제네릭화하여 `IUnit` 외에도 다양한 타입에 재사용 가능하다.
- **FlockBehavior 5가지 행동 벡터 구현**: Alignment, Cohesion, Separation, Follow, Avoidance가 모두 개별 메서드로 분리되어 가중치 기반으로 합산된다.

---

## 이슈

### 1. FlockBehavior — NeighborRadius 미사용으로 모든 부대원이 이웃으로 간주됨 (중요도: 높음)

**파일**: `FlockBehavior.cs:11, 19-26`

`NeighborRadius` 필드가 선언되어 있으나(11행), `CalculateDirection()`에서 이웃 필터링에 전혀 사용되지 않는다. 현재 구현은 `self`를 제외한 전체 부대원 목록을 이웃으로 취급한다.

```csharp
public float NeighborRadius = 3f;  // 선언만 되어 있음

// 19-26행: self만 제외하고 전부 이웃으로 추가
List<SquadMember> others = new List<SquadMember>();
foreach (SquadMember neighbor in neighbors)
{
    if (neighbor != self)
    {
        others.Add(neighbor);
    }
}
```

부대원 수가 적은 초기에는 문제가 없지만, 부대원이 많아지면 Cohesion/Separation 벡터가 먼 거리의 부대원에게까지 영향을 받아 군집 행동이 부자연스러워진다. 또한 O(n) 필터링이 아닌 O(1) 근접 판정이 Boids 알고리즘의 핵심이다.

**제안**: `NeighborRadius` 기반으로 거리 필터링을 추가한다.

```csharp
List<SquadMember> others = new List<SquadMember>();
foreach (SquadMember neighbor in neighbors)
{
    if (neighbor != self)
    {
        float dist = Vector2.Distance(
            self.Transform.position, neighbor.Transform.position);
        if (dist <= NeighborRadius)
        {
            others.Add(neighbor);
        }
    }
}
```

> **[수정 완료]** 커밋 `4550d55`에서 반영됨. `NeighborRadius` 기반 거리 필터링이 이웃 선정 루프에 추가됨.

---

### 2. FlockBehavior — Alignment가 항상 zero를 반환하여 가중치가 무의미 (중요도: 중간)

**파일**: `FlockBehavior.cs:44-48`

```csharp
private Vector2 CalculateAlignment(SquadMember self, List<SquadMember> neighbors)
{
    // UnitMovement에 MoveDirection이 없어 현재는 zero 반환 — 향후 확장 시 구현
    return Vector2.zero;
}
```

Alignment(정렬)는 Boids 3대 행동 중 하나로, 이웃의 이동 방향 평균에 자신을 맞추는 행동이다. 현재 `UnitMovement`에 `MoveDirection` 프로퍼티가 없어 stub 처리한 것은 이해되나, `AlignmentWeight = 1f`로 기본값이 설정된 상태에서 실제 효과가 없다는 점이 코드만 보면 혼란을 줄 수 있다.

**제안**: 두 가지 중 택일한다.
1. `UnitMovement`에 `MoveDirection` 프로퍼티를 추가하고 Alignment를 실제 구현한다.
2. 현재대로 유지하되 `AlignmentWeight = 0f`로 기본값을 변경하여, 미구현 상태임을 기본값으로 명시한다.

---

### 3. SpatialGrid.Query — 반경 내 거리 필터링 없이 셀 단위 근사만 수행 (중요도: 중간)

**파일**: `SpatialGrid.cs:29-48`

```csharp
public List<T> Query(Vector2 center, float radius)
{
    List<T> result = new List<T>();
    int range = Mathf.CeilToInt(radius / cellSize);
    Vector2Int centerCell = WorldToCell(center);

    for (int x = centerCell.x - range; x <= centerCell.x + range; x++)
    {
        for (int y = centerCell.y - range; y <= centerCell.y + range; y++)
        {
            Vector2Int cell = new Vector2Int(x, y);
            if (cells.TryGetValue(cell, out List<T> items))
            {
                result.AddRange(items);
            }
        }
    }

    return result;
}
```

현재 `Query()`는 반경을 셀 단위로 변환하여 사각형 영역의 셀을 순회하지만, 개별 아이템의 실제 거리를 검증하지 않는다. 셀 크기에 따라 반경 밖의 아이템도 반환될 수 있다. 예를 들어 `cellSize=2`, `radius=3`이면 `range=2`가 되어 실제 5x5 셀(최대 거리 ~5.66)을 순회하므로 반경 3을 크게 초과하는 아이템이 포함된다.

공간 분할의 1차 필터로는 이 구현이 유효하며, 호출자 측에서 2차 거리 검증을 수행한다면 문제없다. 그러나 `Query` API의 파라미터명이 `radius`이므로, 호출자는 반환값이 반경 내에 있다고 기대할 수 있다.

**제안**: 두 가지 중 택일한다.
1. `Query` 내부에서 실제 거리 필터링을 추가한다 (정확성 우선).
2. 현재 구현을 유지하되 메서드명을 `QueryApproximate`로 변경하거나, XML 주석으로 셀 단위 근사임을 명시한다 (성능 우선).

단, 이 메서드의 용도가 CombatSystem/MonsterAI의 탐지 범위 조회인 점을 고려하면, 거리 필터링을 포함하는 쪽이 API 계약에 부합한다. 아이템이 위치 정보를 갖고 있지 않은 제네릭 구조(`T where T : class`)이므로, 위치와 아이템을 함께 저장하는 방식으로 확장이 필요할 수 있다.

---

### 4. Squad.Update — deltaTime 파라미터 미사용 (중요도: 중간)

**파일**: `Squad.cs:43-49`

```csharp
public void Update(Transform leader, ObstacleGrid obstacleGrid, float deltaTime)
{
    foreach (SquadMember member in members)
    {
        Vector2 direction = flock.CalculateDirection(member, members, leader, obstacleGrid);
        member.Move(direction);
    }
}
```

`deltaTime` 파라미터를 받지만 사용하지 않는다. 설계 문서의 `Squad.Update()` 시그니처에 `deltaTime`이 포함되어 있어 시그니처는 일치하지만, 실제 이동 속도 계산은 `UnitMovement.Move()` 내부에서 `Time.deltaTime`을 직접 사용하고 있다.

현재 동작에는 문제가 없으나 (UnitMovement가 자체적으로 deltaTime을 처리), 아키텍처적으로 pure C# 계층에서 deltaTime을 전달받는 설계인데 View(MonoBehaviour) 계층에서 `Time.deltaTime`을 직접 사용하는 것은 일관성이 떨어진다. 향후 테스트 가능성에도 영향을 줄 수 있다.

**제안**: `FlockBehavior.CalculateDirection()`이 방향만 반환하고 `UnitMovement.Move()`가 속도를 처리하는 현재 구조를 유지한다면, 미사용 파라미터를 제거하거나 `// reserved for future use` 주석을 추가한다. 또는 `UnitMovement`에 deltaTime을 외부에서 주입하는 방식으로 통일할 수 있다.

> **[수정 완료]** 커밋 `4550d55`에서 반영됨. `Squad.Update()`에 `member.Combat.Tick(deltaTime)` 호출이 추가되어 `deltaTime` 파라미터가 실질적으로 사용됨.

---

### 5. FlockBehavior — public 필드 대신 프로퍼티 권장 (중요도: 낮음)

**파일**: `FlockBehavior.cs:6-11`

```csharp
public float AlignmentWeight = 1f;
public float CohesionWeight = 1f;
public float SeparationWeight = 1.5f;
public float FollowWeight = 2f;
public float AvoidanceWeight = 2f;
public float NeighborRadius = 3f;
```

설계 문서에서는 가중치를 public 필드로 정의하고 있어 설계 일치 자체는 문제없다. 그러나 설계 리뷰(이슈 10)에서 `public readonly` 필드보다 프로퍼티를 권장한 선례가 있으며, C# 컨벤션 상 public 필드보다 auto-property가 권장된다.

**제안**: 런타임에 가중치 조정이 필요 없다면 `{ get; set; }` 프로퍼티 또는 생성자 주입을 고려한다.

```csharp
public float AlignmentWeight { get; set; } = 1f;
```

---

### 6. SquadMemberView — 이벤트 구독 해제 미구현 (중요도: 중간)

**파일**: `SquadMemberView.cs:5-8`

```csharp
public void Subscribe(SquadMember member)
{
    member.OnMoveRequested += direction => Movement.Move(direction);
}
```

익명 람다로 이벤트를 구독하여 구독 해제가 불가능하다. `SquadMember`가 오브젝트 풀에서 재활용되거나 `Squad.RemoveMember()`로 제거될 때, 이전 구독이 해제되지 않으면 메모리 누수 또는 이미 비활성화된 View에 대한 호출이 발생할 수 있다.

설계 문서에는 `Unsubscribe` 메서드가 별도로 명시되어 있지 않으나, `EntitySpawner`가 오브젝트 풀(`Facade.Pool`)을 사용하는 설계이므로 재활용 시나리오를 고려해야 한다.

**제안**: 람다를 메서드로 분리하고 `Unsubscribe` 메서드를 추가한다.

```csharp
private SquadMember subscribedMember;

public void Subscribe(SquadMember member)
{
    subscribedMember = member;
    member.OnMoveRequested += OnMoveRequested;
}

public void Unsubscribe()
{
    if (subscribedMember != null)
    {
        subscribedMember.OnMoveRequested -= OnMoveRequested;
        subscribedMember = null;
    }
}

private void OnMoveRequested(Vector2 direction) => Movement.Move(direction);
```

> **[수정 완료]** 커밋 `4550d55`에서 반영됨. `subscribedMember` 필드, `Unsubscribe()`, `OnDestroy()` 추가됨.

---

### 7. SpatialGrid.Insert — 중복 삽입 방지 없음 (중요도: 낮음)

**파일**: `SpatialGrid.cs:19-27`

```csharp
public void Insert(T item, Vector2 position)
{
    Vector2Int cell = WorldToCell(position);
    if (!cells.ContainsKey(cell))
    {
        cells[cell] = new List<T>();
    }
    cells[cell].Add(item);
}
```

동일 아이템을 동일 위치에 두 번 `Insert`하면 리스트에 중복 추가된다. 설계 문서의 사용 패턴(매 프레임 `Clear()` -> `Insert()`)에서는 문제되지 않으나, API 안정성 측면에서 방어 코드 추가를 고려할 수 있다.

**제안**: 현재 `Clear()` -> `Insert()` 패턴이 보장된다면 현행 유지. 문서로 사용 패턴을 명시하거나, 필요 시 `HashSet<T>` 변환을 고려한다.

---

## 설계 대비 구현 비교

| 항목 | 설계 문서 | 실제 구현 | 판단 |
|------|-----------|-----------|------|
| Squad 클래스 구조 | members, Members, Count, Add/Remove/Clear/Update | 동일 | 일치 |
| OnMemberAdded/Removed 이벤트 | 설계 리뷰에서 추가 | 구현됨 | 일치 |
| Squad.flock 필드 | 설계 문서에 암시적 | 생성자에서 `new FlockBehavior()` 생성 | 일치 |
| SquadMember : Character | MonsterData 보유, Team=Player | 동일 | 일치 |
| SquadMember.OnMoveRequested | 이벤트로 View와 통신 | 동일 | 일치 |
| SquadMemberView.Subscribe | member 이벤트 구독 | 동일 | 일치 |
| FlockBehavior 시그니처 | CalculateDirection(self, neighbors, leader, obstacleGrid) | 동일 | 일치 |
| FlockBehavior 5가지 행동 | Alignment/Cohesion/Separation/Follow/Avoidance | 모두 구현 (Alignment는 stub) | 부분 일치 |
| SpatialGrid<T> | Clear/Insert/Query | 동일 | 일치 |

---

## 종합 평가

| 항목 | 등급 | 설명 |
|------|------|------|
| 설계 일관성 | **A** | 5개 파일 모두 설계 문서의 클래스 명세와 정확히 일치. 설계 리뷰 반영 사항(OnMemberAdded/Removed)도 구현됨 |
| 코딩 컨벤션 준수 | **B+** | Allman 스타일, 명명 규칙, 접근 제한자 명시 등 전반적으로 양호. public 필드(FlockBehavior) 개선 여지 있음 |
| 캡슐화 | **B+** | MVP 패턴이 일관되게 적용됨. 이벤트 구독 해제 미구현으로 인한 잠재적 누수 가능성 존재 |
| 에러 처리 | **B** | null 체크나 빈 컬렉션 처리는 적절하나, 이벤트 해제와 중복 삽입 방어가 미흡 |
| 테스트 존재 | **해당없음** | 본 리뷰 범위에 테스트 파일이 포함되지 않음 |

### 우선 보강이 필요한 3가지

1. **FlockBehavior의 NeighborRadius 미사용** (높음) — Boids 알고리즘의 핵심인 이웃 반경 필터링이 빠져 있어 부대원 수 증가 시 군집 행동이 비정상적으로 동작할 수 있다.
2. **SquadMemberView 이벤트 구독 해제** (중간) — 오브젝트 풀 재활용 시 메모리 누수 및 잘못된 콜백 호출 가능성이 있다.
3. **SpatialGrid.Query의 거리 필터링 부재** (중간) — 반경 파라미터가 셀 단위 근사로만 사용되어 API 계약과 실제 동작 사이에 괴리가 있다.
