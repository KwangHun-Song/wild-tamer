# Code Review 09 — 몬스터 스쿼드 & 스포너 시스템

**리뷰 대상 커밋:** 미커밋 변경 사항 (git working tree)
**리뷰 날짜:** 2026-03-01
**등급: B+ (양호)**

---

## 리뷰 대상 파일

| 파일 | 변경 유형 |
|------|-----------|
| `Scripts/.../Squad/MonsterSquad.cs` | 신규 |
| `Scripts/.../Entity/MonsterSquadSpawner.cs` | 신규 |
| `Scripts/.../Monster/MonsterLeaderAI.cs` | 신규 |
| `Scripts/.../Monster/IMonsterBehavior.cs` | 신규 |
| `Scripts/.../Monster/States/MonsterWanderState.cs` | 신규 |
| `Scripts/.../Monster/Monster.cs` | 수정 (obstacleGrid 추가) |
| `Scripts/.../Entity/EntitySpawner.cs` | 수정 (walkable spawn 추가) |

---

## 긍정적인 점

1. **리더 승계 로직이 명확하고 견고하다.** `HandleMemberDeath()`에서 `leader = null` 처리 후 `FirstOrDefault(m => m.IsAlive)`로 다음 리더를 결정하는 흐름이 직관적이며, `PromoteLeader()` 단일 메서드로 책임이 응집되어 있다.

2. **MonsterSquadSpawner의 관심사 분리가 훌륭하다.** spawn/despawn/AI update 세 책임을 각각 `TrySpawnSquad`, `TryDespawnFarSquads`, `squad.Update` 위임으로 명확히 나누었다.

3. **Monster.cs, EntitySpawner.cs 수정이 하위 호환을 유지한다.** `obstacleGrid = null` 옵셔널 파라미터 추가로 기존 호출 코드(MonsterAI, 단독 스폰)가 무수정 동작한다.

4. **EntitySpawner.FindWalkableOffset()이 장애물 위치 스폰 문제를 해결했다.** 10회 재시도로 walkable 위치를 탐색하는 단순하지만 효과적인 접근법이다.

5. **IMonsterBehavior 인터페이스로 MonsterAI / MonsterLeaderAI가 추상화되었다.** `Monster.cs`가 구체 타입이 아닌 인터페이스에만 의존하므로 향후 AI 교체/추가가 용이하다.

---

## 이슈

### 높음

#### #1 MonsterSquad: `Health.OnDeath` 람다 구독 → 해제 불가 (MonsterSquad.cs:37)

```csharp
monster.Health.OnDeath += () => HandleMemberDeath(monster);
```

람다 클로저로 구독하면 나중에 `-=`로 해제할 수 없다. 해당 monster가 디스폰된 후에도 `Health.OnDeath` 이벤트가 MonsterSquad를 계속 참조하여 GC 수집을 방해한다.

**동일 패턴이 code_review_05에서 `Monster.cs`에서도 지적되어 이미 수정됐다.** 같은 패턴이 재발했으므로 주의가 필요하다.

**대안:** 딕셔너리로 핸들러를 저장하거나, `AddMember`/`RemoveMember` 시 명시적 핸들러를 관리한다.

```csharp
// 딕셔너리 방식
private readonly Dictionary<Monster, Action> deathHandlers = new();

public void AddMember(Monster monster)
{
    members.Add(monster);
    void handler() => HandleMemberDeath(monster);
    deathHandlers[monster] = handler;
    monster.Health.OnDeath += handler;
    if (leader == null) PromoteLeader(monster);
}

// HandleMemberDeath 내부에서 해제
private void HandleMemberDeath(Monster dead)
{
    if (deathHandlers.TryGetValue(dead, out var handler))
    {
        dead.Health.OnDeath -= handler;
        deathHandlers.Remove(dead);
    }
    // ... 나머지 로직
}
```

---

#### #2 MonsterLeaderAI: `new` 키워드로 부모 메서드 은닉 (MonsterLeaderAI.cs:51)

```csharp
public new void Update()
```

`new`는 다형성 없이 부모 메서드를 단순 은닉(hiding)한다. `MonsterLeaderAI`가 `StateMachine<Monster, MonsterTrigger>`로 캐스팅되면 부모의 `Update()`가 호출되어 의도한 동작이 실행되지 않는다. `IMonsterBehavior`가 이 메서드를 호출하도록 위임하는 구조이나, 인터페이스를 통하지 않는 간접 호출 경로에서 버그가 발생할 수 있다.

**대안:** 메서드를 `Tick()`처럼 별개 이름으로 분리하거나, 부모가 `virtual`이면 `override`를 사용한다.

```csharp
// IMonsterBehavior.Update() 구현 - 별개 이름으로
void IMonsterBehavior.Update() => Tick();

private void Tick()
{
    // 기존 Update() 로직
}
```

---

### 중간

#### #3 MonsterLeaderAI Chase 상태에서 SpatialGrid 3중 쿼리 (MonsterLeaderAI.cs:67-83)

Chase 상태에서 매 프레임 3회 Grid 쿼리가 발생한다:

```csharp
// 1번
if (!HasEnemyInRange(pos, Owner.Combat.DetectionRange)) { ... }
// 2번
else if (HasEnemyInRange(pos, Owner.Combat.AttackRange)) { ... }
// 3번
var target = FindClosestEnemy(pos, Owner.Combat.DetectionRange);
```

스쿼드 내 리더가 많을수록 (최대 12개 스쿼드 × 리더 1명 = 최대 12회 per frame) 누적 비용이 된다.

**대안:** 쿼리를 한 번만 수행하고 결과를 재사용한다.

```csharp
case MonsterChaseState _:
    var enemies = UnitGrid.Query(pos, Owner.Combat.DetectionRange)
                          .Where(u => u.Team != Owner.Team && u.IsAlive)
                          .ToList();
    if (enemies.Count == 0)
    {
        Owner.Move(Vector2.zero);
        ExecuteCommand(MonsterTrigger.LoseEnemy);
    }
    else if (enemies.Any(u => Vector2.Distance(pos, u.Transform.position) <= Owner.Combat.AttackRange))
    {
        ExecuteCommand(MonsterTrigger.InAttackRange);
    }
    else { /* FindClosest from enemies */ }
    break;
```

---

#### #4 MonsterSquad: LINQ lazy evaluation으로 O(N²) 순회 (MonsterSquad.cs:55-70)

```csharp
IEnumerable<IUnit> allAsUnits = members.Where(m => m.IsAlive);

foreach (var follower in members)
{
    // ...
    var dir = flock.CalculateDirection(follower, allAsUnits, leaderTf, obstacleGrid);
    // CalculateDirection 내부에서 allAsUnits를 foreach로 순회
}
```

`Where()`는 lazy evaluation이므로 `CalculateDirection` 내부에서 iteration될 때마다 `members` 전체를 재순회한다. 팔로워 N명이면 O(N²) 연산이 된다. 현재 최대 12명이므로 144회 순회로 실질적 영향은 작지만, 설계 의도와 구현 간의 불일치가 있다.

**대안:** 미리 List로 구체화한다.

```csharp
var aliveUnits = members.Where(m => m.IsAlive).Cast<IUnit>().ToList();
```

---

### 낮음

#### #5 MonsterSquadSpawner: `MinSquadCount` 선언 후 미사용 (MonsterSquadSpawner.cs:11)

```csharp
public int MinSquadCount = 3;
```

`TrySpawnSquad()`에서 `>= MaxSquadCount`만 체크하고 `MinSquadCount`는 사용되지 않는다. "최소 N개 유지" 기능이 미구현 상태다. 구현할 계획이 있다면 TODO 주석, 없다면 제거가 권장된다.

---

#### #6 EntitySpawner: `FindWalkableOffset` 실패 시 origin 반환 (EntitySpawner.cs:90)

```csharp
return origin; // 10회 실패 시
```

10회 모두 실패하면 `origin`(리더 스폰 위치)을 반환해, 모든 팔로워가 리더와 같은 위치에 겹쳐 스폰될 수 있다. 미소한 랜덤 오프셋을 적용하는 것이 낫다.

```csharp
return origin + (Vector2)UnityEngine.Random.insideUnitCircle * 0.3f;
```

---

#### #7 public 필드 사용 (코딩 컨벤션 위반)

- `MonsterSquad.cs:18` — `public float StopRadius = 0.6f`
- `MonsterLeaderAI.cs:22` — `public float WanderChangeInterval = 3f`

프로젝트 코딩 컨벤션에서 공개 상태는 Property로 노출한다.

```csharp
public float StopRadius { get; set; } = 0.6f;
```

---

## 종합 평가

| 항목 | 충족도 |
|------|--------|
| 기능 구현 완성도 | O |
| 아키텍처 설계 | O |
| 메모리 관리 | X (람다 구독 누수) |
| 성능 | △ (중복 Grid 쿼리, lazy LINQ) |
| 코딩 컨벤션 | △ (public 필드) |

**등급: B+**
스쿼드 스폰 시스템의 전체 흐름과 설계는 잘 완성되었다. 높음 이슈 2개(람다 구독 누수, `new` 메서드 은닉)는 수정이 권장되며, 특히 #1은 이전 리뷰에서 이미 지적된 패턴의 재발이므로 팀 코드 표준으로 명문화를 검토할 필요가 있다.
