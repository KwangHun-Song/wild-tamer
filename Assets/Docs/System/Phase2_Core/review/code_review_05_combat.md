# 자동 전투 / 테이밍 시스템 - 코드 리뷰

## 리뷰 대상 파일

| 파일 | 경로 | 역할 |
|------|------|------|
| `CombatSystem.cs` | `Scripts/04.Game/02.System/Combat/CombatSystem.cs` | 유닛 등록 및 자동 교전 처리 |
| `DamageProcessor.cs` | `Scripts/04.Game/02.System/Combat/DamageProcessor.cs` | 데미지 계산 및 이벤트 발행 |
| `TamingSystem.cs` | `Scripts/04.Game/02.System/Combat/TamingSystem.cs` | 테이밍 판정 및 부대 합류 |

### 참고 문서

- `Assets/Docs/System/Phase2_Core/design.md`
- `Assets/Docs/System/Phase2_Core/design/combat.md`
- `Assets/Docs/System/Phase2_Core/design/taming.md`
- `Assets/Docs/System/Phase2_Core/design/entity_common.md`
- `Assets/Docs/System/Phase2_Core/review/concept_design_review.md`

---

## 설계 일관성 검증

### 설계 리뷰 이슈 반영 확인

| 설계 리뷰 이슈 | 반영 여부 | 확인 위치 |
|----------------|-----------|-----------|
| 이슈 1: TamingSystem 생성자에 spawner 파라미터 주입 | O | `TamingSystem.cs:10` — `EntitySpawner spawner` 파라미터 존재 |
| 이슈 4: CombatSystem 유닛 등록 흐름 정의 | O | `CombatSystem.cs:24-33` — RegisterUnit/UnregisterUnit 구현 |
| 이슈 5: TamingSystem에서 monster.PlayTamingEffect() 호출 | O | `TamingSystem.cs:29` — `monster.PlayTamingEffect()` 사용, monsterView 직접 접근 없음 |
| 이슈 6: UnitCombat의 Time.time 직접 사용 제거 | O | `UnitCombat.cs` — `Tick(float deltaTime)` 방식으로 구현됨 |

### 설계 문서와 구현 차이

| 항목 | 설계 문서 | 실제 구현 | 판단 |
|------|-----------|-----------|------|
| CombatSystem 생성자 | `CombatSystem(Notifier notifier)` — 내부에서 `SpatialGrid` 생성 (`new(2f)`) | `CombatSystem(SpatialGrid<IUnit> unitGrid, Notifier notifier)` — 외부 주입 | 구현이 개선된 형태. 외부 주입으로 테스트 용이성과 SpatialGrid 공유(MonsterAI 탐지) 확보 |
| CombatSystem.Update() | `Update()` 매개변수 없음 | `Update()` 매개변수 없음 | 일치 |
| TamingSystem.Dispose() | `notifier.Unsubscribe(this)` | `notifier.Unsubscribe(this)` | 일치 |

---

## 긍정적인 점

1. **설계 리뷰 이슈 전수 반영**: 컨셉/설계 리뷰에서 도출된 높음 이슈(1, 4, 5번)가 구현에 빠짐없이 반영되었다. 특히 TamingSystem의 캡슐화 위반(`monsterView` 직접 접근) 수정과 `spawner` 파라미터 주입이 정확히 적용되었다.

2. **Team 비교 로직의 정확성**: `CombatSystem.ProcessCombat()`에서 `target.Team == unit.Team` 비교로 아군 공격을 방지한다. 추가로 `!target.IsAlive` 체크를 함께 수행하여 이미 죽은 유닛에 대한 불필요한 공격도 차단한다.

    ```csharp
    // CombatSystem.cs:60
    if (target.Team == unit.Team || !target.IsAlive) continue;
    ```

3. **DamageProcessor의 명확한 이벤트 흐름**: `TakeDamage` -> `ResetCooldown` -> `IOnHitListener` 통지 -> 사망 시 `IOnUnitDeathListener` 통지 순서가 설계 문서의 이벤트 흐름도와 정확히 일치한다.

4. **TamingSystem의 Notifier 구독/해제 쌍**: 생성자에서 `notifier.Subscribe(this)`, `Dispose()`에서 `notifier.Unsubscribe(this)`를 호출하여 리스너 누수를 방지한다.

5. **SpatialGrid 외부 주입으로 설계 개선**: 설계 문서에서는 CombatSystem 내부에서 `SpatialGrid`를 생성했으나, 구현에서는 외부 주입 방식으로 변경하여 MonsterAI와의 공유가 자연스러워지고 단위 테스트 시 Mock 주입이 가능해졌다.

6. **순수 C# 설계 준수**: 세 파일 모두 MonoBehaviour를 상속하지 않으며, 설계 문서의 "순수 C# 시스템" 원칙을 충실히 따른다.

---

## 이슈

#### 1. CombatSystem.ProcessCombat() 순회 중 컬렉션 변경 위험 (중요도: 높음)

**파일**: `CombatSystem.cs:51-65`

```csharp
private void ProcessCombat()
{
    foreach (var unit in registeredUnits)
    {
        // ...
        DamageProcessor.ProcessDamage(unit, target, notifier);
        break;
    }
}
```

`DamageProcessor.ProcessDamage()`는 `IOnUnitDeathListener`를 통지하고, 이 리스너 중 `TamingSystem.OnUnitDeath()`가 `squad.AddMember()`를 호출한다. `Squad.OnMemberAdded` 이벤트에 `combatSystem.RegisterUnit()`이 구독되어 있으므로(설계 문서의 유닛 등록 흐름), **foreach 순회 중에 `registeredUnits` 리스트가 변경**되어 `InvalidOperationException`이 발생할 수 있다.

마찬가지로 Monster 사망 시 `EntitySpawner.DespawnMonster()` -> `OnMonsterDespawned` -> `combatSystem.UnregisterUnit()`으로 리스트에서 제거될 수도 있다.

**제안**: 순회 전에 리스트를 복사하거나, 등록/해제를 즉시 반영하지 않고 버퍼링한 뒤 Update 말미에 일괄 적용하는 방식을 사용한다.

```csharp
// 방안 A: 스냅샷 복사
private void ProcessCombat()
{
    var snapshot = registeredUnits.ToArray(); // 또는 pooled list
    foreach (var unit in snapshot) { ... }
}

// 방안 B: 지연 등록/해제 큐
private readonly List<IUnit> pendingAdd = new();
private readonly List<IUnit> pendingRemove = new();

public void RegisterUnit(IUnit unit) => pendingAdd.Add(unit);
public void UnregisterUnit(IUnit unit) => pendingRemove.Add(unit);

// Update() 말미에서 일괄 처리
private void FlushPending() { ... }
```

---

#### 2. TamingSystem.Dispose()가 IDisposable 인터페이스를 구현하지 않음 (중요도: 중간)

**파일**: `TamingSystem.cs:18`

```csharp
public void Dispose() => notifier.Unsubscribe(this);
```

`Dispose()`라는 이름을 사용하지만 `IDisposable` 인터페이스를 구현하지 않는다. 이로 인해:
- `using` 문을 사용할 수 없어 자동 정리가 불가능하다.
- 호출자가 `Dispose()` 호출을 잊을 경우 리스너 누수가 발생한다.
- C# 관례상 `Dispose()`는 `IDisposable` 구현을 전제하므로, 인터페이스 없이 사용하면 혼동을 줄 수 있다.

**제안**: `IDisposable`을 구현하거나, 인터페이스 없이 사용한다면 메서드명을 `Cleanup()` 등으로 변경하여 의도를 명확히 한다.

```csharp
public class TamingSystem : IOnUnitDeathListener, IDisposable
```

---

#### 3. TamingSystem에서 UnityEngine.Random 직접 사용 -- 테스트 격리성 저하 (중요도: 중간)

**파일**: `TamingSystem.cs:24`

```csharp
if (Random.value > monster.Data.tamingChance) return;
```

`UnityEngine.Random.value`를 직접 사용하므로 EditMode 단위 테스트에서 테이밍 성공/실패를 결정적으로 제어할 수 없다. 설계 문서에서 `UnitCombat`의 `Time.time` 직접 사용을 제거한 것과 동일한 맥락에서, Random도 외부 주입이 가능한 형태가 바람직하다.

**제안**: `Func<float>` 등의 랜덤 값 공급자를 생성자에서 주입받는다. 기본값으로 `() => Random.value`를 사용하면 런타임 동작은 동일하다.

```csharp
private readonly Func<float> randomProvider;

public TamingSystem(Squad squad, EntitySpawner spawner, Notifier notifier,
    Func<float> randomProvider = null)
{
    this.randomProvider = randomProvider ?? (() => Random.value);
    // ...
}

// 사용처
if (randomProvider() > monster.Data.tamingChance) return;
```

---

#### 4. CombatSystem.RegisterUnit()의 중복 검사가 O(n) (중요도: 중간)

**파일**: `CombatSystem.cs:24-28`

```csharp
public void RegisterUnit(IUnit unit)
{
    if (!registeredUnits.Contains(unit))
        registeredUnits.Add(unit);
}
```

`List.Contains()`는 O(n) 탐색이다. 유닛 수가 많아질수록 등록 성능이 저하된다. 현재 `RebuildGrid()`에서도 매 프레임 전체 리스트를 순회하므로 List가 적절하지만, 중복 검사만을 위해 보조 HashSet을 두면 등록 시점의 성능을 O(1)로 개선할 수 있다.

**제안**: `HashSet<IUnit>`을 보조 자료구조로 추가하거나, 이벤트 구독 구조상 중복 등록이 원천적으로 불가능하다면 Contains 검사 자체를 제거한다.

```csharp
private readonly HashSet<IUnit> unitSet = new();
private readonly List<IUnit> registeredUnits = new();

public void RegisterUnit(IUnit unit)
{
    if (unitSet.Add(unit))
        registeredUnits.Add(unit);
}

public void UnregisterUnit(IUnit unit)
{
    if (unitSet.Remove(unit))
        registeredUnits.Remove(unit);
}
```

---

#### 5. 인덴테이션 불일치 -- 탭/스페이스 혼용 (중요도: 낮음)

**파일**: `TamingSystem.cs` 전체

`CombatSystem.cs`와 `DamageProcessor.cs`는 스페이스 인덴테이션을 사용하지만, `TamingSystem.cs`는 탭 인덴테이션을 사용한다. 프로젝트 `.editorconfig` 설정에 따라 통일이 필요하다.

**제안**: `.editorconfig`에 정의된 인덴테이션 방식으로 통일한다. 세 파일 모두 동일한 Combat 폴더 내에 위치하므로 일관성이 중요하다.

---

## 코드 리뷰 체크리스트

| 항목 | 충족 | 비고 |
|------|------|------|
| 설계 일관성 | △ | CombatSystem 생성자가 설계 대비 개선(외부 주입)되었으나 문서 업데이트 필요. 순회 중 컬렉션 변경 위험은 설계에서 미예측 |
| 코딩 컨벤션 준수 | △ | 인덴테이션 불일치(탭/스페이스 혼용), 접근 제한자 명시는 준수 |
| 에러 처리 | △ | ProcessCombat 순회 중 InvalidOperationException 가능성 존재 |
| 캡슐화 | O | monsterView 직접 접근 없음, UnitGrid 읽기 전용 노출, View protected 유지 |
| 테스트 존재 | X | 단위 테스트 미작성 (별도 단계에서 작성 예정으로 판단) |

---

## 종합 평가

| 항목 | 등급 | 설명 |
|------|------|------|
| 설계 반영도 | **A** | 설계 리뷰 이슈 5건(생성자 불일치, 캡슐화 위반, 유닛 등록 흐름 등)이 전수 반영됨. 생성자 외부 주입 변경은 개선 사항 |
| 구조 설계 | **B+** | Notifier 기반 이벤트 흐름, SpatialGrid 분리, 정적 DamageProcessor 등 역할 분리가 명확. 다만 순회 중 컬렉션 변경 위험이 구조적 결함 |
| 구현 품질 | **B** | 핵심 로직은 정확하나, 런타임 예외 가능성(이슈 1)과 테스트 격리성 부족(이슈 3)이 존재 |
| 컨벤션 준수 | **B+** | 명명 규칙, 접근 제한자, XML 주석 등 대부분 준수. 인덴테이션 불일치만 보완 필요 |

### 우선 보강이 필요한 3가지

1. **[높음] ProcessCombat 순회 중 컬렉션 변경 방어** -- 테이밍 성공 시 `RegisterUnit` 호출 체인에 의해 `InvalidOperationException`이 발생할 수 있다. 스냅샷 복사 또는 지연 등록 큐 방식으로 즉시 수정이 필요하다.
2. **[중간] TamingSystem IDisposable 구현** -- `Dispose()` 메서드명 사용 시 인터페이스 구현을 동반하거나, 메서드명을 변경하여 C# 관례와의 혼동을 방지한다.
3. **[중간] Random 외부 주입으로 테스트 격리성 확보** -- `UnitCombat`에서 `Time.time`을 제거한 설계 원칙과 동일하게, `TamingSystem`에서도 `Random.value`를 주입 가능하게 하여 EditMode 테스트에서 결정적 검증이 가능하도록 한다.
