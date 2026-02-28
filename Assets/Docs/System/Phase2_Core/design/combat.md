# 2.5 자동 전투 시스템

> 상위 문서: [Phase 2 설계](../design.md)

등록된 유닛들 사이의 자동 교전을 처리한다. 유닛 탐색은 SpatialGrid로 최적화하며, 타격 판정 결과를 Notifier로 전파하여 연출(VFX)과 테이밍 시스템이 반응하도록 한다.

---

## CombatSystem (pure C#)

`RegisterUnit` / `UnregisterUnit`은 IUnit을 받으므로 Monster, SquadMember, Player 모두 처리 가능하다. 유닛 등록은 GameController 생성자에서 이벤트 구독으로 자동화된다.

```csharp
public class CombatSystem
{
    private readonly SpatialGrid<IUnit> unitGrid = new(2f);
    private readonly Notifier notifier;

    public CombatSystem(Notifier notifier) => this.notifier = notifier;

    public void RegisterUnit(IUnit unit) { ... }
    public void UnregisterUnit(IUnit unit) { ... }

    /// <summary>MonsterAI의 탐지를 위해 읽기 전용으로 unitGrid를 노출한다.</summary>
    public SpatialGrid<IUnit> UnitGrid => unitGrid;

    public void Update()
    {
        RebuildGrid();
        ProcessCombat();
    }

    private void RebuildGrid() { ... }
    private void ProcessCombat() { ... }
}
```

### CombatSystem 유닛 등록 흐름

유닛 등록/해제는 GameController 생성자에서 이벤트 구독으로 자동화된다. CombatSystem이 Squad나 EntitySpawner를 직접 참조하지 않으므로 Mediator 패턴이 유지된다.

```
Player 생성 시:                  combatSystem.RegisterUnit(Player)
Squad.OnMemberAdded:             combatSystem.RegisterUnit(SquadMember)
Squad.OnMemberRemoved:           combatSystem.UnregisterUnit(SquadMember)
EntitySpawner.OnMonsterSpawned:  combatSystem.RegisterUnit(Monster)
EntitySpawner.OnMonsterDespawned:combatSystem.UnregisterUnit(Monster)
```

---

## DamageProcessor (pure static)

데미지 계산과 이벤트 발행을 담당하는 순수 정적 클래스.

```csharp
public static class DamageProcessor
{
    public static void ProcessDamage(IUnit attacker, IUnit target, Notifier notifier)
    {
        var damage = attacker.Combat.AttackDamage;
        target.Health.TakeDamage(damage);
        attacker.Combat.ResetCooldown();

        notifier.Notify<IOnHitListener>(l => l.OnHit(attacker, target, damage));

        if (!target.IsAlive)
            notifier.Notify<IOnUnitDeathListener>(l => l.OnUnitDeath(target, attacker));
    }
}
```

### 이벤트 흐름

```
DamageProcessor.ProcessDamage()
    ├──→ Notifier<IOnHitListener>
    │       ├──→ HitStop       (역경직)
    │       ├──→ CameraShake   (카메라 흔들림)
    │       └──→ HitEffectPlayer (이펙트/사운드)
    └──→ Notifier<IOnUnitDeathListener>
            └──→ TamingSystem  (테이밍 판정)
```
