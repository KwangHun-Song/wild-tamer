# 2.4 몬스터 - 코드 리뷰

## 리뷰 대상 파일

| 파일 | 역할 |
|------|------|
| `Monster.cs` | Monster Presenter (pure C#) |
| `MonsterView.cs` | Monster View (MonoBehaviour) |
| `MonsterAI.cs` | FSM 기반 AI + 탐지/이동/공격 로직 |
| `MonsterIdleState.cs` | Idle 상태 |
| `MonsterChaseState.cs` | Chase 상태 |
| `MonsterAttackState.cs` | Attack 상태 |
| `MonsterData.cs` | 몬스터 기획 데이터 (ScriptableObject) |
| `BossPattern.cs` | 보스 패턴 데이터 (ScriptableObject) |

### 참조 문서

- `Assets/Docs/System/Phase2_Core/design.md` (전체 설계)
- `Assets/Docs/System/Phase2_Core/design/monster.md` (몬스터 상세 설계)
- `Assets/Docs/System/Phase2_Core/review/concept_design_review.md` (설계 리뷰)
- `Assets/Modules/FiniteStateMachine/Runtime/StateMachine.cs` (FSM 프레임워크)

---

## 설계 일관성 체크

| 항목 | 설계 문서 | 실제 구현 | 판단 |
|------|-----------|-----------|------|
| Monster 생성자 시그니처 | `Monster(MonsterView view, MonsterData data)` (2개 파라미터) | `Monster(MonsterView view, MonsterData data, SpatialGrid<IUnit> unitGrid)` (3개 파라미터) | 구현이 개선된 형태. SpatialGrid를 Monster 생성 시 주입받아 MonsterAI에 전달. 설계 리뷰 이슈 8 반영 결과 |
| MonsterAI 생성자 | `MonsterAI(Monster owner, SpatialGrid<IUnit> unitGrid)` | 동일 | 일치 |
| PlayTamingEffect 캡슐화 | `monster.PlayTamingEffect()` 공개 메서드 위임 | `public void PlayTamingEffect() => monsterView.PlayTamingEffect();` | 일치. 설계 리뷰 이슈 5 정확히 반영 |
| monsterView 접근 제한 | private 필드, 외부 노출 금지 | `private readonly MonsterView monsterView` | 일치 |
| MonsterAI.Update() 처리 | 설계에서 State별 탐지 로직 제시 | MonsterAI에서 `new` 키워드로 Update 오버라이드하여 중앙 집중 처리 | 구현 방식 변경 (아래 이슈 참조) |
| 상태 전이 트리거 | DetectEnemy, LoseEnemy, InAttackRange, OutOfAttackRange | 동일 | 일치 |
| MonsterData 필드 | id, displayName, grade, maxHp, attackDamage, attackRange, attackCooldown, detectionRange, moveSpeed, tamingChance, prefab, squadPrefab, bossPatterns | 동일 | 일치 |

---

## 긍정적인 점

### 1. 설계 리뷰 이슈의 정확한 반영

설계 리뷰에서 지적된 핵심 이슈들이 충실하게 구현에 반영되었다.

- **이슈 5 (캡슐화 위반)**: `Monster.PlayTamingEffect()` 공개 메서드가 존재하고, `monsterView`는 `private readonly`로 외부에 노출되지 않는다.
- **이슈 6 (Time.time 직접 사용)**: `UnitCombat`이 `elapsed` 누적 + `Tick(deltaTime)` 방식으로 구현되어 Unity 의존이 제거되었다.
- **이슈 8 (SpatialGrid 주입)**: `MonsterAI` 생성자에 `SpatialGrid<IUnit>`이 주입되며, `EntitySpawner`를 통한 주입 경로가 명확하다.

### 2. MVP 패턴의 일관된 적용

`Monster(Presenter) / MonsterView(View)` 분리가 설계 방침에 정확히 부합한다. Monster는 순수 C# 클래스이고, MonsterView는 MonoBehaviour를 상속한다. 이벤트 기반 통신(`OnMoveRequested`)으로 View와 Presenter가 결합하며, `view.Subscribe(this)` 패턴으로 양방향 바인딩을 명시적으로 수행한다.

### 3. 트리거 기반 FSM 전이의 명확한 정의

`MonsterTrigger` enum과 `Transitions` 배열이 상태 전이 테이블을 그대로 코드화하여 가독성이 높다. 4개 전이(Idle->Chase, Chase->Idle, Chase->Attack, Attack->Chase)가 설계 문서의 전이 요약과 정확히 일치한다.

### 4. SpatialGrid null 방어

`MonsterAI.HasEnemyInRange()`와 `FindClosestEnemy()`에서 `if (UnitGrid == null) return false/null;` 방어 코드가 있어, 테스트 환경이나 UnitGrid 미주입 상황에서도 크래시가 발생하지 않는다.

### 5. MonsterData ScriptableObject 완성도

설계 문서에 정의된 모든 필드(id, displayName, grade, maxHp, attackDamage, attackRange, attackCooldown, detectionRange, moveSpeed, tamingChance, prefab, squadPrefab, bossPatterns)가 빠짐없이 구현되었다. `MonsterGrade` enum도 함께 정의되어 있다.

### 6. MonsterChaseState / MonsterAttackState의 OnExit 이동 정지 처리

Chase와 Attack 상태에서 빠져나갈 때 `Owner.Move(Vector2.zero)`를 호출하여 이동을 정지시키는 처리가 적절하다. 상태 전이 시 잔여 이동이 남지 않도록 보장한다.

---

## 이슈

### 1. MonsterAI의 `new` 키워드로 StateMachine.Update() 숨김 (중요도: 중간)

**파일**: `MonsterAI.cs:43`

```csharp
/// <summary>Monster.Update()에서 매 프레임 호출. 상태 전이 판정과 이동을 처리한다.</summary>
public new void Update()
{
    var pos = (Vector2)Owner.Transform.position;
    switch (CurrentState)
    {
        // ...
    }
}
```

`new` 키워드로 부모 `StateMachine<Monster, MonsterTrigger>.Update()`를 숨긴다. 이 방식 자체는 의도적이고 주석으로 근거를 명시하고 있어 동작에는 문제가 없다. 그러나 다음과 같은 위험이 존재한다:

- 호출자가 `StateMachine<Monster, MonsterTrigger>` 타입으로 참조할 경우 원래 `Update()`(TryTransition만 실행)가 호출되어 탐지/이동 로직이 누락된다.
- FSM 프레임워크의 `Update()` → `TryTransition()` 기본 흐름과 다른 경로를 사용하므로, FSM 프레임워크에 Condition 기반 전이를 추가했을 때 MonsterAI만 다르게 동작할 수 있다.

**제안**: 현재 Monster.Update() -> MonsterAI.Update() 호출 경로가 고정되어 있어 당장 문제는 아니지만, FSM 프레임워크에 `OnUpdate()` 가상 메서드를 추가하거나, MonsterAI의 주기적 로직을 별도 메서드(예: `Evaluate()`)로 분리하여 `new` 숨김 없이 호출하는 방식을 고려할 수 있다.

---

### 2. MonsterAI.Update() 내 상태별 로직 중앙 집중 -- State 클래스 미활용 (중요도: 중간)

**파일**: `MonsterAI.cs:43-81`

```csharp
switch (CurrentState)
{
    case MonsterIdleState _:
        if (HasEnemyInRange(pos, Owner.Combat.DetectionRange))
            ExecuteCommand(MonsterTrigger.DetectEnemy);
        break;
    case MonsterChaseState _:
        // ... 이동 + 전이 로직
        break;
    case MonsterAttackState _:
        // ... 공격 + 전이 로직
        break;
}
```

설계 문서(`monster.md`)에서는 "각 State는 `StateMachine.Owner`(Monster)와 `(StateMachine as MonsterAI).UnitGrid`를 통해 탐지를 수행한다"고 명시하여 각 State 클래스가 자체 판정 책임을 지는 구조를 제시했다. 그러나 실제 구현에서는 모든 판정 로직이 `MonsterAI.Update()`의 switch 문에 집중되고, 세 State 클래스(`MonsterIdleState`, `MonsterChaseState`, `MonsterAttackState`)는 `OnEnter()`/`OnExit()`만 구현하여 사실상 빈 껍데기에 가깝다.

현재 상태가 3개뿐이므로 실용적 문제는 크지 않으나:
- 상태가 추가될 때마다 switch 분기가 늘어나 MonsterAI가 비대해진다.
- State 패턴의 장점(상태별 행동 캡슐화)이 제대로 활용되지 못한다.

**제안**: FSM 프레임워크의 State에 `OnUpdate()` 가상 메서드를 추가하고, 각 State가 자체 탐지/이동/공격 로직을 가지도록 분산한다. 또는 현재 3개 상태 수준에서는 이대로 유지하되, 상태가 5개 이상으로 늘어나는 시점에서 리팩토링 계획을 명시한다.

---

### 3. MonsterAttackState에서 실제 데미지 처리 누락 (중요도: 중간)

**파일**: `MonsterAI.cs:74-79`

```csharp
case MonsterAttackState _:
    if (!HasEnemyInRange(pos, Owner.Combat.AttackRange))
        ExecuteCommand(MonsterTrigger.OutOfAttackRange);
    else if (Owner.Combat.CanAttack)
        Owner.Combat.ResetCooldown(); // DamageProcessor는 Step 8에서 연결
    break;
```

`CanAttack`이 true일 때 쿨다운만 리셋하고 실제 데미지 처리(타겟 특정, DamageProcessor 호출 등)는 수행하지 않는다. 주석에 "Step 8에서 연결"이라고 명시되어 있어 의도된 스텁임을 알 수 있으나, 다음 사항이 우려된다:

- **공격 대상 미특정**: 쿨다운은 리셋하지만 "누구를" 공격하는지의 정보가 전달되지 않는다. Step 8에서 연결할 때 가장 가까운 적을 다시 조회해야 하며, 이 패턴이 확정되지 않았다.
- **쿨다운 소비의 낭비 가능성**: 데미지가 실제로 적용되지 않는데 쿨다운만 리셋되면, 나중에 연결 시 타이밍 불일치가 발생할 수 있다.

**제안**: 현 단계에서는 스텁 코드로 유지하되, Step 8 구현 시 다음을 고려할 것:
1. Attack 상태에서 현재 타겟을 필드로 유지하고 `FindClosestEnemy()`로 갱신
2. `ResetCooldown()` 호출을 실제 데미지 적용 성공 후로 이동

---

### 4. MonsterAI에서 HasEnemyInRange와 FindClosestEnemy의 중복 조회 (중요도: 낮음)

**파일**: `MonsterAI.cs:54-71`

```csharp
case MonsterChaseState _:
    if (!HasEnemyInRange(pos, Owner.Combat.DetectionRange))    // 1차 조회
    {
        ExecuteCommand(MonsterTrigger.LoseEnemy);
    }
    else if (HasEnemyInRange(pos, Owner.Combat.AttackRange))   // 2차 조회
    {
        ExecuteCommand(MonsterTrigger.InAttackRange);
    }
    else
    {
        var target = FindClosestEnemy(pos, Owner.Combat.DetectionRange); // 3차 조회
        // ...
    }
    break;
```

Chase 상태에서 매 프레임 최대 3회 `UnitGrid.Query()`를 호출한다. SpatialGrid의 쿼리 비용이 낮다면 실용적 문제는 없으나, 몬스터 수가 많아지면 누적된다.

**제안**: `FindClosestEnemy()`가 가장 가까운 적과 거리를 함께 반환하도록 수정하면 단 1회 조회로 세 가지 판정(탐지 범위 내 존재, 공격 범위 내 존재, 추적 대상)을 모두 처리할 수 있다.

```csharp
var (target, dist) = FindClosestEnemyWithDistance(pos, Owner.Combat.DetectionRange);
if (target == null) { ExecuteCommand(MonsterTrigger.LoseEnemy); }
else if (dist <= Owner.Combat.AttackRange) { ExecuteCommand(MonsterTrigger.InAttackRange); }
else { Owner.Move(((Vector2)target.Transform.position - pos).normalized); }
```

---

### 5. BossPattern ScriptableObject의 최소 구현 (중요도: 낮음)

**파일**: `BossPattern.cs:1-8`

```csharp
[CreateAssetMenu(menuName = "Data/BossPattern")]
public class BossPattern : ScriptableObject
{
    public string patternId;
    public string description;
}
```

`patternId`와 `description`만 존재하며, 보스 패턴이 실제로 어떤 행동을 정의하는지(공격 타입, 범위, 타이밍, 이펙트 등)의 필드가 없다. 현 단계에서는 스텁으로 충분하나, 보스 전투 구현 시 필드 확장이 필요하다.

**제안**: 현 단계에서는 이대로 유지. 보스 전투 구현 단계에서 패턴 타입, 데미지 배율, 범위, 쿨다운 등 구체 필드를 추가한다.

---

### 6. MonsterData의 public 필드 -- SerializeField + private 권장 (중요도: 낮음)

**파일**: `MonsterData.cs:6-18`

```csharp
public class MonsterData : ScriptableObject
{
    public string id;
    public string displayName;
    public MonsterGrade grade;
    public int maxHp;
    // ...
}
```

모든 필드가 `public`으로 선언되어 런타임에서 외부 코드가 값을 변경할 수 있다. ScriptableObject는 Unity 에디터에서 직렬화 데이터로 사용되는 것이 일반적이므로 읽기 전용 접근이 바람직하다.

**제안**: `[SerializeField] private` + 읽기 전용 프로퍼티 패턴을 적용한다. 다만 프로젝트 전반의 ScriptableObject 데이터 컨벤션이 `public` 필드 기준이라면 일관성을 위해 현행 유지도 합리적이다.

```csharp
[SerializeField] private string id;
public string Id => id;
```

---

### 7. Monster 생성자의 시그니처가 설계 문서와 상이 (중요도: 낮음)

**파일**: `Monster.cs:14`

```csharp
// 설계 문서 (monster.md)
public Monster(MonsterView view, MonsterData data) // 2개 파라미터

// 실제 구현
public Monster(MonsterView view, MonsterData data, SpatialGrid<IUnit> unitGrid) // 3개 파라미터
```

설계 문서에서는 `MonsterAI(this)` 2개 파라미터로 생성하고, SpatialGrid 주입은 별도 명시했으나, 실제 구현에서는 Monster 생성자에 `SpatialGrid<IUnit>`을 직접 받아 `MonsterAI`에 전달한다. `EntitySpawner.SpawnMonster()`에서도 정확히 이 시그니처로 호출하므로 동작에 문제는 없다.

**제안**: 구현이 더 명확한 의존성 주입 형태이므로 코드를 기준으로 설계 문서(`monster.md`)를 업데이트한다.

> **[수정 완료]** 커밋 `57737ca`에서 반영됨. `monster.md`의 Monster 생성자 시그니처, MonsterAI 생성, MonsterAI.Update() 패턴 설명, EntitySpawner 코드가 실제 구현에 맞게 동기화됨.

---

## 코드 리뷰 체크리스트

| 항목 | 충족 | 비고 |
|------|------|------|
| 설계 일관성 | △ | Monster 생성자 시그니처 차이, State 책임 분산 방식 변경. 모두 개선 방향이나 설계 문서 동기화 필요 |
| 코딩 컨벤션 준수 | O | 명명 규칙, 접근 제한자, XML 주석 사용이 일관적 |
| 에러 처리 | O | SpatialGrid null 방어, 이벤트 null 조건부 호출(`?.Invoke`) 적용 |
| 캡슐화 | O | monsterView는 private, PlayTamingEffect() 공개 메서드로 위임. 설계 리뷰 이슈 5 충족 |
| 테스트 존재 | X | 몬스터 관련 유닛 테스트 없음. 다만 현 단계에서는 FSM 프레임워크 테스트가 별도 존재하므로 허용 가능 |

---

## 종합 평가

| 항목 | 등급 | 설명 |
|------|------|------|
| 설계 반영도 | **A** | 설계 리뷰 이슈(캡슐화, SpatialGrid 주입, UnitCombat 개선)가 모두 정확하게 반영됨 |
| 구조 설계 | **B+** | MVP 분리, FSM 활용, 이벤트 기반 통신이 잘 구현됨. State 클래스 활용도가 낮은 점이 아쉬움 |
| 구현 품질 | **B+** | 방어 코드, 주석, 이벤트 패턴이 양호. `new` 키워드 사용과 중복 조회가 개선 여지 |
| 컨벤션 준수 | **A** | 명명, 접근 제한자, 주석이 일관적 |
| 데이터 완성도 | **A** | MonsterData 필드가 설계와 100% 일치. BossPattern은 스텁이나 현 단계에서 적절 |

### 우선 보강이 필요한 3가지

1. **MonsterAI의 `new Update()` 구조 개선** -- FSM 프레임워크에 `OnUpdate()` 가상 메서드를 추가하거나, 별도 메서드로 분리하여 메서드 숨김 위험을 제거한다. (이슈 1, 2 관련)
2. **SpatialGrid 중복 조회 최적화** -- Chase 상태에서 프레임당 최대 3회 조회를 1회로 줄이는 리팩토링을 검토한다. 몬스터 수가 많아지는 시점에서 성능에 영향을 줄 수 있다. (이슈 4 관련)
3. **설계 문서 동기화** -- Monster 생성자 시그니처와 State 책임 분산 방식이 설계 문서와 달라졌으므로 `monster.md`를 실제 구현에 맞게 업데이트한다. (이슈 7 관련)
