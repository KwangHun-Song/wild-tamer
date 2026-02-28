using Base;

public interface IOnUnitDeathListener : IListener
{
    void OnUnitDeath(IUnit deadUnit, IUnit killer);
}
