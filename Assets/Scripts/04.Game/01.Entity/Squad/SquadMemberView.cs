using UnityEngine;

public class SquadMemberView : CharacterView
{
    private SquadMember subscribedMember;

    public void Subscribe(SquadMember member)
    {
        subscribedMember = member;
        member.OnMoveRequested += OnMoveRequested;
    }

    public void Unsubscribe()
    {
        if (subscribedMember != null)
        {
            subscribedMember.OnMoveRequested -= OnMoveRequested;
            subscribedMember = null;
        }
    }

    private void OnMoveRequested(Vector2 direction) => Movement.Move(direction);

    private void OnDestroy() => Unsubscribe();
}
