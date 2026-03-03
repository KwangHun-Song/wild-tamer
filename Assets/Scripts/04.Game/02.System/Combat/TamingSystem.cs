using System;
using Base;
using UnityEngine;

public class TamingSystem : IOnUnitDeathListener, IDisposable
{
    private readonly Squad squad;
    private readonly EntitySpawner spawner;
    private readonly Notifier notifier;

    public TamingSystem(Squad squad, EntitySpawner spawner, Notifier notifier)
    {
        this.squad = squad;
        this.spawner = spawner;
        this.notifier = notifier;
        notifier.Subscribe(this);
    }

    public void Dispose() => notifier.Unsubscribe(this);

    public void OnUnitDeath(IUnit deadUnit, IUnit killer)
    {
        if (killer.Team != UnitTeam.Player) return;
        if (deadUnit is not Monster monster) return;

        var tamingData = Facade.DB.Get<TamingData>("TamingData");
        if (UnityEngine.Random.value > tamingData.tamingChance) return;

        monster.MarkAsTamed((Vector2)monster.Transform.position);
        monster.OnReadyToSpawnTamed += HandleTamingSpawn;
    }

    private void HandleTamingSpawn(Monster m)
    {
        m.OnReadyToSpawnTamed -= HandleTamingSpawn;
        var member = spawner.SpawnSquadMember(m.Data, m.TamingSpawnPos);
        squad.AddMember(member);
        UserData.AddTamedMonster(m.Data.name);
        member.View.PlayCreateAnimation();
        notifier.Notify<IOnTamingListener>(l => l.OnTamingSuccess(m, member));
    }
}
