# Phase 2: 코어 시스템 - 컨셉/설계 리뷰

## 리뷰 대상 문서

| 문서 | 경로 |
|------|------|
| `concept.md` | `Assets/Docs/System/Phase2_Core/concept.md` |
| `design.md` | `Assets/Docs/System/Phase2_Core/design.md` |

---

## 컨셉 문서 리뷰

### 작업 컨벤션 충족도

| 항목 | 충족 | 비고 |
|------|------|------|
| 시스템의 목적과 역할 | O | Phase 1 베이스 위에 코어 루프 구현이라는 목표가 명확히 서술됨 |
| 유저 관점의 동작 설명 | O | 탐험/전투/테이밍/부대 확장 각 단계를 플레이어 경험 중심으로 서술 |
| 레퍼런스 자료 | O | Wild Tamer, concept.md, milestone.md, Phase1 문서 참조 포함 |
| 다른 시스템과의 관계 | O | 9개 시스템 의존 관계 다이어그램 및 설명 테이블 포함 |

### 긍정적인 점

- **단계별 구현 순서 정의**: 의존성 기반 Layer 1~5 구현 순서를 명시하여 개발 진행 방향이 명확하다.
- **기술적 고려사항 선제 정의**: NavMesh 미사용, Boids 군집, 다수 개체 최적화 등 핵심 과제를 컨셉 단계에서 이미 식별하여 설계 단계에서의 방향성이 일관되게 이어진다.
- **유저 경험 중심 서술**: 시스템 목록 나열에 그치지 않고 탐험→전투→테이밍→부대 확장의 사이클을 플레이어 관점에서 구체적으로 서술하여 설계 의도가 명확하다.

### 개선 제안

이슈 없음. 컨벤션 충족도 및 내용 완성도 모두 양호하다.

---

## 설계 문서 리뷰

### 작업 컨벤션 충족도

| 항목 | 충족 | 비고 |
|------|------|------|
| 핵심 클래스 및 인터페이스 구조 | O | IUnit, Character, CharacterView, 9개 시스템 클래스 전부 정의됨 |
| 클래스 간 의존 관계 및 데이터 흐름 | O | GameController 중심 의존 방향과 이벤트 흐름 명시 |
| 사용할 디자인 패턴 | O | Mediator, MVP, Memento, Observer, FSM 선택 근거 포함 |
| 외부 시스템과의 인터페이스 정의 | O | Facade.Pool, Facade.Coroutine, Base.Notifier 연동 방식 명시 |

### 긍정적인 점

- **GameController Mediator 패턴**: 9개 시스템을 단일 오케스트레이터가 조율하고, Notifier로 시스템 간 직접 참조를 차단하는 설계가 명확하다. 전투→연출, 전투→테이밍의 이벤트 흐름이 특히 잘 정의되어 있다.
- **MVP 계층 분리**: `Character(pure C#) / CharacterView(MonoBehaviour)` 구분이 일관되며, View 이벤트를 통해 씬과 간접 연결하는 방식이 설계 전반에 걸쳐 지켜진다.
- **MonoBehaviour 사용 기준표**: 어떤 클래스가 MonoBehaviour를 상속해야 하는지 명확한 기준을 표로 정리한 점이 구현 일관성을 보장하는 데 유효하다.
- **SpatialGrid로 탐색 최적화**: O(n²) → O(n) 전환을 별도 클래스로 분리하여 재사용성과 테스트 가능성을 모두 확보했다.
- **GameSnapshot(Memento)**: View를 포함하지 않는 순수 데이터 스냅샷으로 저장/복원 경계를 명확히 한 점이 좋다.

### 개선 제안

> **수정 완료**: 아래 이슈는 리뷰 후 `design.md` 수정에 모두 반영되었다.

#### 1. TamingSystem 생성자 파라미터 불일치 (중요도: 높음) — 수정 완료

**위치**: `design.md` — GameController 생성자 vs TamingSystem 생성자

GameController 생성자에서는 2개 파라미터로 생성했으나 TamingSystem 생성자는 3개를 요구한다.

```csharp
// 수정 전
tamingSystem = new TamingSystem(Squad, Notifier);

// 수정 후
tamingSystem = new TamingSystem(Squad, entitySpawner, Notifier);
```

`spawner` 파라미터가 누락되면 컴파일 오류가 발생한다. TamingSystem 내부에서 `spawner.SpawnSquadMember()`를 호출하므로 반드시 주입이 필요하다.

---

#### 2. SquadMemberSnapshot 생성자 파라미터 불일치 (중요도: 높음) — 수정 완료

**위치**: `design.md` — GameController.CreateSnapshot()

```csharp
// 수정 전
Squad.Members.Select(m => new SquadMemberSnapshot(m)).ToList()

// 수정 후
var playerPos = (Vector2)Player.Transform.position;
Squad.Members.Select(m => new SquadMemberSnapshot(m, playerPos)).ToList()
```

`playerPos`는 `PositionOffset`(플레이어 기준 상대 좌표) 계산에 필수 값이다.

---

#### 3. RestoreFromSnapshot에서 protected View 직접 접근 (중요도: 높음) — 수정 완료

**위치**: `design.md` — GameController.RestoreFromSnapshot()

```csharp
// 수정 전 — protected 경계 위반 (컴파일 오류)
Player.View.transform.position = snapshot.PlayerPosition;

// 수정 후 — Character.SetPosition() 공개 메서드 추가
Player.SetPosition(snapshot.PlayerPosition);
// Character 내부: public void SetPosition(Vector2 position) => View.transform.position = (Vector3)position;
```

View 접근을 Character 계층 내로 캡슐화하여 외부에서 직접 View에 접근하지 않도록 처리했다.

---

#### 4. CombatSystem 유닛 등록 흐름 미정의 (중요도: 높음) — 수정 완료

**위치**: `design.md` — GameController 생성자

Player만 등록되고, Squad 멤버와 Monster의 등록/해제 시점이 정의되어 있지 않았다.

```csharp
// 수정 후 — 이벤트 기반 자동 등록
combatSystem.RegisterUnit(Player);
Squad.OnMemberAdded   += combatSystem.RegisterUnit;
Squad.OnMemberRemoved += combatSystem.UnregisterUnit;
entitySpawner.OnMonsterSpawned   += combatSystem.RegisterUnit;
entitySpawner.OnMonsterDespawned += combatSystem.UnregisterUnit;
```

Squad와 EntitySpawner에 각각 `OnMemberAdded/Removed`, `OnMonsterSpawned/Despawned` 이벤트를 추가하고 GameController가 구독하는 방식으로, Mediator 패턴을 유지하면서 자동 등록 흐름을 확립했다.

---

#### 5. TamingSystem에서 Monster.monsterView 직접 접근 — 캡슐화 위반 (중요도: 높음) — 수정 완료

**위치**: `design.md` — TamingSystem.OnUnitDeath()

```csharp
// 수정 전 — private 필드 직접 접근 (컴파일 오류)
monster.monsterView.PlayTamingEffect();

// 수정 후 — Monster 공개 메서드 위임
monster.PlayTamingEffect();
// Monster 내부: public void PlayTamingEffect() => monsterView.PlayTamingEffect();
```

---

#### 6. UnitCombat(pure C#)의 Time.time 직접 사용 (중요도: 중간) — 수정 완료

**위치**: `design.md` — UnitCombat

```csharp
// 수정 전 — UnityEngine 의존
public bool CanAttack => Time.time - lastAttackTime >= cooldown;
public void ResetCooldown() => lastAttackTime = Time.time;

// 수정 후 — deltaTime 누적 방식
private float elapsed;
public bool CanAttack => elapsed >= cooldown;
public void ResetCooldown() => elapsed = 0f;
public void Tick(float deltaTime) => elapsed += deltaTime;
```

GameController.Update()에서 `Time.deltaTime`을 `Tick()`으로 전달하는 방식으로 Unity 의존을 제거하여 EditMode 단위 테스트가 가능해졌다.

---

#### 7. HitStop 중첩 처리 없음 (중요도: 중간) — 수정 완료

**위치**: `design.md` — HitStop.OnHit()

매 히트 이벤트마다 새 코루틴을 시작하면 다수 유닛이 동시에 공격할 때 timeScale 복원 타이밍이 엇갈릴 수 있다.

```csharp
// 수정 후
private bool isActive;
public void OnHit(IUnit attacker, IUnit target, int damage)
{
    if (isActive) return;
    Facade.Coroutine.StartCoroutine(ApplyHitStop());
}
```

---

#### 8. MonsterAI 적 탐지 로직 미정의 (중요도: 중간) — 수정 완료

**위치**: `design.md` — MonsterAI / MonsterIdleState

MonsterAI 생성자에 `SpatialGrid<IUnit>` 주입을 추가하고 CombatSystem이 `UnitGrid` 프로퍼티를 공개하여, MonsterIdleState가 DetectionRange 기반으로 적을 조회하는 흐름을 명시했다.

```csharp
// GameController 생성자에서 주입
monsterAI = new MonsterAI(monster, combatSystem.UnitGrid);
```

---

#### 9. MonsterSpawner 역할 범위와 이름 불일치 (중요도: 낮음) — 수정 완료

`MonsterSpawner`가 `SpawnSquadMember()`도 담당하여 이름이 역할을 대표하지 못했다.

**수정**: `EntitySpawner`로 이름 변경, 폴더도 `02.System/Entity/`로 정리.

---

#### 10. `public readonly Notifier Notifier = new()` — 필드보다 프로퍼티 권장 (중요도: 낮음) — 수정 완료

```csharp
// 수정 전
public readonly Notifier Notifier = new();

// 수정 후
public Notifier Notifier { get; } = new();
```

---

## 종합 평가

| 항목 | 등급 | 설명 |
|------|------|------|
| 아키텍처 구조 | **A** | Mediator+MVP+Notifier 조합이 9개 시스템 결합을 효과적으로 관리. 패턴 선택과 역할 분리가 명확함 |
| 설계 완성도 | **B+** | 초기 코드 예시 수준의 버그(생성자 불일치, 접근 제어 위반)가 다수 포함되었으나 리뷰 후 전부 수정 완료 |
| 구현 가이드 품질 | **B+** | 폴더 구조, MonoBehaviour 기준표, 데이터 흐름도가 구현자에게 충분한 맥락을 제공. MonsterAI 탐지 로직도 보완됨 |
| 코딩 컨벤션 | **A** | UnitCombat Time 의존 제거, Notifier 프로퍼티 전환 등 지적 사항이 모두 반영됨 |

### 우선 보강이 필요한 2가지

리뷰에서 도출된 10개 이슈는 모두 `design.md`에 반영되었다. 구현 단계에서 추가 확인이 필요한 사항:

1. **MonsterAI 탐지 State 구현** — `MonsterIdleState`에서 SpatialGrid 조회 시 DetectionRange 단위(월드 좌표)와 그리드 셀 크기 일치 여부를 구현 시점에서 검증 필요
2. **EntitySpawner 오브젝트 풀 연동** — `Facade.Pool.Spawn<MonsterView>()` 호출 시 MonsterData별 프리팹 매핑 방식을 구현 단계에서 구체화 필요
