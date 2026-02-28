using Base;

public interface IOnTamingListener : IListener
{
    void OnTamingSuccess(Monster monster, SquadMember newMember);
}
