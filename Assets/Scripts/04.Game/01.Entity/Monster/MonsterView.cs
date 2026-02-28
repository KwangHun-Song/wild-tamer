using UnityEngine;

public class MonsterView : CharacterView
{
    public void Subscribe(Monster monster)
    {
        monster.OnMoveRequested += direction => Movement.Move(direction);
    }

    public void PlayHitEffect() { }

    public void PlayDeathEffect() { }

    public void PlayTamingEffect() { }
}
