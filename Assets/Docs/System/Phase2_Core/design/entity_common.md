# 공통 개체 구조 (Entity Common)

> 상위 문서: [Phase 2 설계](../design.md)

Player, Monster, SquadMember가 공유하는 인터페이스, 추상 클래스, 컴포넌트를 정의한다.

---

## MVP 구조 개요

```
[PlayerInput (MB)]
       │ MoveDirection
       ▼
[GameController (C#)]          ← 중앙 오케스트레이터
       │ player.Move(direction)
       ▼
[Player (C#)] ──OnMoveRequested 이벤트──→ [PlayerView (MB)]
                                                  └──→ UnitMovement.Move()
```

- **Model**: 기획 데이터 (MonsterData, BossPattern) — ScriptableObject
- **Presenter** (`Character` → `Player` / `SquadMember` / `Monster`): 순수 C# 게임 로직. View 이벤트를 통해 씬과 연결.
- **View** (`CharacterView` → `PlayerView` / `SquadMemberView` / `MonsterView`): MonoBehaviour. 비주얼·이동·체력 처리.

---

## IUnit

모든 전투 참여 개체의 공통 인터페이스.

```csharp
public enum UnitTeam { Player, Enemy }

public interface IUnit
{
    UnitTeam Team { get; }
    Transform Transform { get; }    // View.Transform 위임
    UnitHealth Health { get; }      // View.Health 위임
    UnitCombat Combat { get; }      // Presenter 소유
    bool IsAlive { get; }
    float Radius { get; }           // 충돌 반경 (ResolveOverlaps 등에서 사용)
}
```

---

## CharacterView (MonoBehaviour)

PlayerView, SquadMemberView, MonsterView의 추상 베이스.

```csharp
public abstract class CharacterView : MonoBehaviour
{
    [SerializeField] private UnitHealth health;
    [SerializeField] private UnitMovement movement;

    public UnitHealth Health => health;
    public UnitMovement Movement => movement;
}
```

---

## UnitHealth (MonoBehaviour, CharacterView 컴포넌트)

체력 관리 컴포넌트. CharacterView와 같은 GameObject에 붙는다.

```csharp
public class UnitHealth : MonoBehaviour
{
    public int MaxHp { get; private set; }
    public int CurrentHp { get; private set; }
    public bool IsAlive => CurrentHp > 0;

    public event Action<int> OnDamaged;
    public event Action OnDeath;

    public void Initialize(int maxHp) { ... }
    public void TakeDamage(int damage) { ... }
}
```

---

## UnitMovement (MonoBehaviour, CharacterView 컴포넌트)

이동 처리 컴포넌트. CharacterView와 같은 GameObject에 붙는다.

```csharp
public class UnitMovement : MonoBehaviour
{
    public float MoveSpeed { get; set; }

    public void Move(Vector2 direction) { ... }
    public void MoveTo(Vector2 target) { ... }
    public void Stop() { ... }
}
```

---

## Character (pure C#)

Player, SquadMember, Monster의 추상 Presenter 베이스.

View는 `protected`로 선언한다. 외부에서 View에 직접 접근이 필요한 경우(위치 복원 등)는 Character에 전용 메서드를 추가하여 캡슐화를 유지한다.

```csharp
public abstract class Character : IUnit
{
    public abstract UnitTeam Team { get; }
    public abstract float Radius { get; }
    public Transform Transform => View.Transform;
    public UnitHealth Health => View.Health;
    public UnitCombat Combat { get; }
    public bool IsAlive => View.Health.IsAlive;

    protected CharacterView View { get; }

    protected Character(CharacterView view, UnitCombat combat)
    {
        View = view;
        Combat = combat;
    }

    /// <summary>
    /// View의 위치를 직접 설정한다. 스냅샷 복원 등 외부 제어가 필요한 경우 사용한다.
    /// Character 내부에서만 View에 접근하여 캡슐화를 유지한다.
    /// </summary>
    public void SetPosition(Vector2 position) => View.transform.position = (Vector3)position;
}
```

---

## UnitCombat (pure C#, Presenter 소유)

공격 관련 수치와 쿨다운을 담당한다.

`Time.time`에 직접 의존하지 않는다. `Tick(float deltaTime)`으로 시간을 누적하여 테스트 격리성을 확보한다. GameController.Update()에서 모든 유닛의 `Combat.Tick(Time.deltaTime)`을 호출한다.

```csharp
public class UnitCombat
{
    public int AttackDamage { get; set; }
    public float AttackRange { get; set; }
    public float DetectionRange { get; set; }

    private readonly float cooldown;
    private float elapsed;

    public UnitCombat(int damage, float range, float detectionRange, float cooldown) { ... }

    public bool CanAttack => elapsed >= cooldown;
    public void ResetCooldown() => elapsed = 0f;
    public void Tick(float deltaTime) => elapsed += deltaTime;
}
```

---

## Notifier 이벤트 인터페이스

시스템 간 느슨한 결합을 위한 이벤트 리스너 인터페이스. `Notifier.Subscribe(this)`로 등록한다.

```csharp
public interface IOnHitListener : IListener
{
    void OnHit(IUnit attacker, IUnit target, int damage);
}

public interface IOnUnitDeathListener : IListener
{
    void OnUnitDeath(IUnit deadUnit, IUnit killer);
}

public interface IOnTamingListener : IListener
{
    void OnTamingSuccess(Monster monster, SquadMember newMember);
}
```

| 인터페이스 | 발행처 | 구독처 |
|-----------|--------|--------|
| `IOnHitListener` | `DamageProcessor` | HitStop, CameraShake, HitEffectPlayer |
| `IOnUnitDeathListener` | `DamageProcessor` | TamingSystem |
| `IOnTamingListener` | `TamingSystem` | (Phase 3 UI 등 확장 예정) |
