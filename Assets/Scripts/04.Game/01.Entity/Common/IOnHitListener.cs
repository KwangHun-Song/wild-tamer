using Base;

public interface IOnHitListener : IListener
{
    void OnHit(IUnit attacker, IUnit target, int damage);
}
