# 2.6 테이밍

> 상위 문서: [Phase 2 설계](../design.md)

몬스터 처치 시 확률 기반으로 테이밍 판정을 수행한다. 성공 시 해당 몬스터를 SquadMember로 스폰하여 부대에 합류시킨다.

---

## TamingSystem (pure C#)

`IOnUnitDeathListener`를 구현하여 Notifier로부터 처치 이벤트를 수신한다. `monster.monsterView`(private)에 직접 접근하지 않고 `Monster.PlayTamingEffect()` 공개 메서드를 호출한다.

```csharp
public class TamingSystem : IOnUnitDeathListener
{
    private readonly Squad squad;
    private readonly EntitySpawner spawner;
    private readonly Notifier notifier;

    public TamingSystem(Squad squad, EntitySpawner spawner, Notifier notifier)
    {
        this.squad    = squad;
        this.spawner  = spawner;
        this.notifier = notifier;
        notifier.Subscribe(this);
    }

    public void Dispose() => notifier.Unsubscribe(this);

    public void OnUnitDeath(IUnit deadUnit, IUnit killer)
    {
        if (killer.Team != UnitTeam.Player) return;
        if (deadUnit is not Monster monster) return;
        if (Random.value > monster.Data.tamingChance) return;

        var member = spawner.SpawnSquadMember(monster.Data, monster.Transform.position);
        squad.AddMember(member);

        monster.PlayTamingEffect();     // 공개 메서드 경유, monsterView 직접 접근 금지

        notifier.Notify<IOnTamingListener>(l => l.OnTamingSuccess(monster, member));
    }
}
```

### 테이밍 흐름

```
[Player 팀이 Monster 처치]
    └──→ DamageProcessor.ProcessDamage()
            └──→ Notifier<IOnUnitDeathListener>
                    └──→ TamingSystem.OnUnitDeath()
                            ├── killer.Team == Player? Y
                            ├── deadUnit is Monster? Y
                            ├── Random.value <= tamingChance? Y
                            ├──→ EntitySpawner.SpawnSquadMember()
                            ├──→ Squad.AddMember()
                            │       └──→ OnMemberAdded → CombatSystem.RegisterUnit()
                            ├──→ Monster.PlayTamingEffect()
                            └──→ Notifier<IOnTamingListener>
```
