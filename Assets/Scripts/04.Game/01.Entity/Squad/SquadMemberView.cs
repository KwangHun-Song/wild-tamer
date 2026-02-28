using UnityEngine;

public class SquadMemberView : CharacterView
{
    public void Subscribe(SquadMember member)
    {
        member.OnMoveRequested += direction => Movement.Move(direction);
    }
}
