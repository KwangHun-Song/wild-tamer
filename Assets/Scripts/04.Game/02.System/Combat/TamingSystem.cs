using Base;
using UnityEngine;

public class TamingSystem : IOnUnitDeathListener
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
		if (Random.value > monster.Data.tamingChance) return;

		var member = spawner.SpawnSquadMember(monster.Data, monster.Transform.position);
		squad.AddMember(member);

		monster.PlayTamingEffect();

		notifier.Notify<IOnTamingListener>(l => l.OnTamingSuccess(monster, member));
	}
}
