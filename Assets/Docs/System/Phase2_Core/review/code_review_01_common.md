# Phase 2: 코어 시스템 - 공통 개체 구조 코드 리뷰

## 리뷰 대상 파일

| 파일 | 역할 | MonoBehaviour |
|------|------|:---:|
| `IUnit.cs` | 전투 참여 개체 공통 인터페이스 | - |
| `Character.cs` | Presenter 추상 베이스 (pure C#) | X |
| `CharacterView.cs` | View 추상 베이스 | O |
| `UnitHealth.cs` | 체력 컴포넌트 | O |
| `UnitMovement.cs` | 이동 컴포넌트 | O |
| `UnitCombat.cs` | 공격 수치/쿨다운 (pure C#) | X |
| `IOnHitListener.cs` | 피격 이벤트 리스너 | - |
| `IOnUnitDeathListener.cs` | 사망 이벤트 리스너 | - |
| `IOnTamingListener.cs` | 테이밍 이벤트 리스너 | - |

## 참조 설계 문서

- `Assets/Docs/System/Phase2_Core/design.md`
- `Assets/Docs/System/Phase2_Core/design/entity_common.md`
- `Assets/Docs/System/Phase2_Core/review/concept_design_review.md`

---

## 설계 문서 대비 구현 충족도

### IUnit

| 항목 | 설계 | 실제 구현 | 충족 | 비고 |
|------|------|-----------|:----:|------|
| `UnitTeam` enum | `Player, Enemy` | `Player, Enemy` | O | |
| `Team` 프로퍼티 | `UnitTeam Team { get; }` | `UnitTeam Team { get; }` | O | |
| `Transform` 프로퍼티 | `Transform Transform { get; }` | `Transform Transform { get; }` | O | |
| `Health` 프로퍼티 | `UnitHealth Health { get; }` | `UnitHealth Health { get; }` | O | |
| `Combat` 프로퍼티 | `UnitCombat Combat { get; }` | `UnitCombat Combat { get; }` | O | |
| `IsAlive` 프로퍼티 | `bool IsAlive { get; }` | `bool IsAlive { get; }` | O | |

### Character

| 항목 | 설계 | 실제 구현 | 충족 | 비고 |
|------|------|-----------|:----:|------|
| IUnit 구현 | `Character : IUnit` | `Character : IUnit` | O | |
| `Team` abstract | `abstract UnitTeam Team` | `abstract UnitTeam Team` | O | |
| `Transform` 위임 | `View.Transform` | `View.transform` | O | 구현이 정확. 설계 문서가 대문자로 표기했으나 MonoBehaviour의 `transform`(소문자)이 올바름 |
| `Health` 위임 | `View.Health` | `View.Health` | O | |
| `Combat` 소유 | Presenter 소유 | `UnitCombat Combat { get; }` | O | |
| `IsAlive` 위임 | `View.Health.IsAlive` | `View.Health.IsAlive` | O | |
| `View` protected | `protected CharacterView View` | `protected CharacterView View { get; }` | O | |
| 생성자 | `(CharacterView view, UnitCombat combat)` | `(CharacterView view, UnitCombat combat)` | O | |
| `SetPosition()` | 설계 리뷰에서 추가 확정 | `public void SetPosition(Vector2)` | O | concept_design_review.md 이슈 #3 반영 완료 |

### CharacterView

| 항목 | 설계 | 실제 구현 | 충족 | 비고 |
|------|------|-----------|:----:|------|
| MonoBehaviour 상속 | O | O | O | |
| `health` SerializeField | `[SerializeField] private UnitHealth` | `[SerializeField] private UnitHealth health` | O | |
| `movement` SerializeField | `[SerializeField] private UnitMovement` | `[SerializeField] private UnitMovement movement` | O | |
| `Health` 프로퍼티 | `UnitHealth Health => health` | `UnitHealth Health => health` | O | |
| `Movement` 프로퍼티 | `UnitMovement Movement => movement` | `UnitMovement Movement => movement` | O | |

### UnitHealth

> **[구현 변경]** 커밋 `19406c6`에서 MonoBehaviour → pure C#으로 리팩토링됨 (MVP 레이어 위반 수정).

| 항목 | 설계 | 실제 구현 | 충족 | 비고 |
|------|------|-----------|:----:|------|
| MonoBehaviour 상속 | O | X | △ | 커밋 `19406c6`에서 pure C#으로 리팩토링됨 |
| `MaxHp` | `int MaxHp { get; private set; }` | `int MaxHp { get; private set; }` | O | |
| `CurrentHp` | `int CurrentHp { get; private set; }` | `int CurrentHp { get; private set; }` | O | |
| `IsAlive` | `CurrentHp > 0` | `CurrentHp > 0` | O | |
| `OnDamaged` 이벤트 | `event Action<int>` | `event Action<int> OnDamaged` | O | |
| `OnDeath` 이벤트 | `event Action` | `event Action OnDeath` | O | |
| `Initialize(int)` | 명세 있음 | 구현됨 | O | |
| `TakeDamage(int)` | 명세 있음 | 구현됨 | O | |

### UnitMovement

| 항목 | 설계 | 실제 구현 | 충족 | 비고 |
|------|------|-----------|:----:|------|
| MonoBehaviour 상속 | O | O | O | |
| `MoveSpeed` | `float MoveSpeed { get; set; }` | `float MoveSpeed { get; set; }` | O | |
| `Move(Vector2)` | 명세 있음 | 구현됨 | O | |
| `MoveTo(Vector2)` | 명세 있음 | 구현됨 | O | |
| `Stop()` | 명세 있음 | 구현됨 (빈 메서드 + 주석) | O | |

### UnitCombat

| 항목 | 설계 | 실제 구현 | 충족 | 비고 |
|------|------|-----------|:----:|------|
| pure C# (non-MonoBehaviour) | O | O | O | |
| `AttackDamage` | `int AttackDamage { get; set; }` | `int AttackDamage { get; set; }` | O | |
| `AttackRange` | `float AttackRange { get; set; }` | `float AttackRange { get; set; }` | O | |
| `DetectionRange` | `float DetectionRange { get; set; }` | `float DetectionRange { get; set; }` | O | |
| `cooldown` readonly | `readonly float cooldown` | `readonly float cooldown` | O | |
| `elapsed` 누적 방식 | deltaTime 누적 | `float elapsed` + `Tick(float)` | O | concept_design_review.md 이슈 #6 반영 (Time.time 미사용) |
| `CanAttack` | `elapsed >= cooldown` | `elapsed >= cooldown` | O | |
| `ResetCooldown()` | `elapsed = 0f` | `elapsed = 0f` | O | |
| `Tick(float deltaTime)` | 설계 리뷰에서 추가 확정 | 구현됨 | O | |
| 생성자 | `(int, float, float, float)` | `(int, float, float, float)` | O | |

### Notifier 이벤트 인터페이스

| 항목 | 설계 | 실제 구현 | 충족 | 비고 |
|------|------|-----------|:----:|------|
| `IOnHitListener : IListener` | `OnHit(IUnit, IUnit, int)` | `OnHit(IUnit attacker, IUnit target, int damage)` | O | |
| `IOnUnitDeathListener : IListener` | `OnUnitDeath(IUnit, IUnit)` | `OnUnitDeath(IUnit deadUnit, IUnit killer)` | O | |
| `IOnTamingListener : IListener` | `OnTamingSuccess(Monster, SquadMember)` | `OnTamingSuccess(Monster monster, SquadMember newMember)` | O | |
| `using Base` (IListener 네임스페이스) | 명시되지 않았으나 필수 | 3개 파일 모두 `using Base;` 포함 | O | |

---

## 긍정적인 점

### 1. 설계 충실도가 높다

9개 파일 전체가 설계 문서(`entity_common.md`)의 클래스 명세와 정확히 일치한다. 설계 리뷰(`concept_design_review.md`)에서 확정된 수정 사항 2건(`UnitCombat.Tick(float dt)`, `Character.SetPosition()`)도 빠짐없이 반영되어 있다.

### 2. MonoBehaviour 사용 기준을 정확히 준수한다

설계 문서의 MonoBehaviour 사용 기준표를 그대로 따르고 있다:
- `Character`, `UnitCombat`, `UnitHealth` -- pure C# (MonoBehaviour X, `UnitHealth`는 커밋 `19406c6`에서 리팩토링됨)
- `CharacterView`, `UnitMovement` -- MonoBehaviour (씬 배치, Transform/컴포넌트 직렬화)

### 3. MVP 계층 분리가 명확하다

`Character`(Presenter)는 `View`를 `protected`로 선언하여 외부에서의 직접 접근을 차단하고, `SetPosition()`처럼 필요한 경우만 공개 메서드로 캡슐화했다. `Transform`, `Health` 등 IUnit 프로퍼티도 View에 위임하되 Character를 통해서만 접근 가능하다.

### 4. UnitCombat의 Unity 의존성 제거

`Time.time` 대신 `Tick(float deltaTime)` 누적 방식을 채택하여 EditMode 단위 테스트가 가능하다. 생성자에서 `elapsed = cooldown`으로 초기화하여 생성 직후 바로 공격 가능한 합리적인 기본 동작을 제공한다. 이 초기화 로직은 설계 문서에 명시되지 않았으나 게임플레이 관점에서 적절한 구현 판단이다.

### 5. 이벤트/인터페이스 설계가 일관적이다

3개의 Notifier 리스너 인터페이스(`IOnHitListener`, `IOnUnitDeathListener`, `IOnTamingListener`)가 모두 `Base.IListener`를 상속하며, 네이밍 패턴(`IOn{Event}Listener`), 메서드 시그니처 스타일이 통일되어 있다.

---

## 이슈

### 1. UnitHealth.TakeDamage()에 음수 데미지 검증 없음 (중요도: 중간)

**파일**: `UnitHealth.cs:19-28`

```csharp
public void TakeDamage(int damage)
{
    if (!IsAlive) return;

    CurrentHp = Mathf.Max(0, CurrentHp - damage);
    OnDamaged?.Invoke(damage);

    if (!IsAlive)
        OnDeath?.Invoke();
}
```

`damage`에 음수 값이 전달되면 `CurrentHp`가 `MaxHp`를 초과할 수 있다. 현재 `DamageProcessor`가 유일한 호출자일 때는 문제가 없지만, `TakeDamage`가 `public`이므로 향후 다른 시스템에서 잘못된 값을 전달할 가능성이 있다.

**제안**: 메서드 시작부에 `damage` 값 검증을 추가한다.

```csharp
public void TakeDamage(int damage)
{
    if (!IsAlive || damage <= 0) return;
    // ...
}
```

> **[수정 완료]** 커밋 `4550d55`에서 반영됨. `TakeDamage()` 시작부에 `if (!IsAlive || damage <= 0) return;` 가드 추가됨.

---

### 2. UnitCombat.elapsed 오버플로우 가능성 (중요도: 중간)

**파일**: `UnitCombat.cs:23`

```csharp
public void Tick(float deltaTime) => elapsed += deltaTime;
```

`elapsed`는 `ResetCooldown()` 호출 없이 매 프레임 누적되며, 공격하지 않는 유닛(예: 먼 거리의 몬스터)은 `elapsed`가 계속 증가한다. `float`의 정밀도 한계(약 16,777,216 이후 1.0f 단위 손실)에 도달하는 데 약 194일(16,777,216초)이 필요하므로 실제 게임 세션에서는 발생하기 어렵다. 다만 방어적 코딩 관점에서 상한을 두면 안전하다.

**제안**: `Tick()` 내에서 `cooldown` 이상으로 누적되지 않도록 클램프한다.

```csharp
public void Tick(float deltaTime)
{
    if (elapsed < cooldown)
        elapsed += deltaTime;
}
```

> **[수정 완료]** 커밋 `4550d55`에서 반영됨. `elapsed < cooldown` 조건 추가로 `cooldown` 이상 누적되지 않도록 클램프됨.

---

### 3. UnitMovement.MoveTo() 방향 계산이 우회적이다 (중요도: 낮음)

**파일**: `UnitMovement.cs:12-16`

```csharp
public void MoveTo(Vector2 target)
{
    var direction = ((Vector2)transform.position - target).normalized;
    Move(-direction);
}
```

`(현재 위치 - 목표)` 후 부호를 반전(`-direction`)하는 방식은 `(목표 - 현재 위치).normalized`와 동일하나, 의도 파악에 한 단계 더 필요하다.

**제안**: 직관적인 방향 계산으로 변경한다.

```csharp
public void MoveTo(Vector2 target)
{
    var direction = (target - (Vector2)transform.position).normalized;
    Move(direction);
}
```

> **[수정 완료]** 커밋 `4550d55`에서 반영됨. `(target - (Vector2)transform.position).normalized` 직관적 방향 계산으로 변경됨.

---

### 4. UnitMovement.Move()에서 Time.deltaTime 직접 사용 (중요도: 낮음)

**파일**: `UnitMovement.cs:7-10`

```csharp
public void Move(Vector2 direction)
{
    transform.Translate((Vector3)(direction * (MoveSpeed * Time.deltaTime)));
}
```

`UnitMovement`는 MonoBehaviour이므로 `Time.deltaTime` 사용 자체는 문제가 없다. 그러나 `UnitCombat`에서는 설계 리뷰(이슈 #6)를 통해 `Time.time` 의존을 제거하고 외부에서 `deltaTime`을 주입하는 방식으로 변경한 바 있다. `UnitMovement`도 동일한 패턴을 적용하면 `HitStop`(역경직) 등 `timeScale` 변경 시 일관된 시간 제어가 가능하고, View 컴포넌트 단독 단위 테스트도 가능해진다.

다만 `UnitMovement`는 MonoBehaviour(View 계층)이므로 `UnitCombat`(pure C#, Presenter 계층)과는 설계 맥락이 다르다. `Time.deltaTime` 사용은 MonoBehaviour의 일반적인 패턴이며, 현 단계에서는 큰 문제가 아니다. `HitStop` 기능 구현 시 `timeScale` 기반으로 동작한다면 `Time.deltaTime`이 자동으로 반영되므로 별도 주입 없이도 동작할 수 있다.

**제안**: 현 단계에서는 유지하되, 향후 `HitStop` 구현 시 `timeScale` 방식이 아닌 수동 시간 제어가 필요해지면 `Move(Vector2 direction, float deltaTime)` 시그니처로 변경을 검토한다.

---

### 5. 설계 문서의 `View.Transform` vs 구현의 `View.transform` 표기 불일치 (중요도: 낮음)

**파일**: `Character.cs:6`

```csharp
public Transform Transform => View.transform;
```

설계 문서(`entity_common.md:112`)는 `View.Transform`(대문자 T)으로 표기했으나, `CharacterView`에는 `Transform` 프로퍼티가 별도 선언되어 있지 않다. 구현은 MonoBehaviour의 `transform`(소문자) 프로퍼티를 올바르게 사용하고 있다. 이는 설계 문서의 표기 오류이며, 구현이 정확하다.

**제안**: 설계 문서(`entity_common.md`)의 `View.Transform`을 `View.transform`으로 수정하여 구현과 일치시킨다.

---

### 6. 네임스페이스 미사용 (중요도: 낮음)

**파일**: 리뷰 대상 9개 파일 전체

9개 파일 모두 네임스페이스 없이 글로벌 네임스페이스에 선언되어 있다. 프로젝트 규모가 커지면 `UnitHealth`, `UnitCombat` 등 일반적인 이름이 서드파티 에셋과 충돌할 수 있다.

다만, 코딩 컨벤션(`csharp_coding_convention.md`)에서 네임스페이스 규칙이 "논의가 필요한 사항"으로 분류되어 있고, 기존 코드(`Monster.cs`, `SquadMember.cs`, `Player.cs` 등)도 동일하게 글로벌 네임스페이스를 사용하고 있으므로 프로젝트 전반의 일관성은 유지되고 있다.

**제안**: 프로젝트 차원에서 네임스페이스 규칙이 확정되면 일괄 적용한다. 현 단계에서는 기존 패턴 유지가 적절하다.

---

## 종합 평가

| 항목 | 등급 | 설명 |
|------|------|------|
| 설계 일관성 | **A** | 설계 문서 명세와 구현이 정확히 일치. 설계 리뷰 수정 사항(`Tick`, `SetPosition`) 모두 반영됨 |
| 코딩 컨벤션 준수 | **A** | Allman 스타일, 접근 제한자 명시, 언더바 미사용 등 컨벤션 전반 준수 |
| 캡슐화 | **A** | View를 `protected`로 제한, 외부 접근은 공개 메서드로 캡슐화 |
| MonoBehaviour 기준 준수 | **A** | 설계 문서의 기준표와 정확히 일치 |
| 에러 처리 | **B+** | `UnitHealth.TakeDamage()`의 음수 데미지 검증 부재가 유일한 미비점 |
| 이벤트/인터페이스 설계 | **A** | IListener 상속, 네이밍 패턴, 시그니처 스타일 모두 일관적 |

### 우선 보강이 필요한 2가지

1. **UnitHealth.TakeDamage() 음수 데미지 방어** (이슈 #1) -- public API이므로 `damage <= 0` 가드를 추가하여 예상치 못한 체력 증가를 방지
2. **UnitCombat.elapsed 클램프** (이슈 #2) -- 방어적 코딩으로 불필요한 누적을 방지. 실제 문제 발생 확률은 낮으나 비용 없이 적용 가능
