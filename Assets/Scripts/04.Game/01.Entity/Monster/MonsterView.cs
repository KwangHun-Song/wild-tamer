using UnityEngine;

public class MonsterView : CharacterView
{
    private Monster subscribedMonster;

    public void Subscribe(Monster monster)
    {
        subscribedMonster = monster;
        monster.OnMoveRequested += OnMoveRequested;
    }

    public void Unsubscribe()
    {
        if (subscribedMonster != null)
        {
            subscribedMonster.OnMoveRequested -= OnMoveRequested;
            subscribedMonster = null;
        }
    }

    private void OnMoveRequested(Vector2 direction) => HandleMoveRequested(direction);

    private void OnDestroy() => Unsubscribe();

    public void PlayHitEffect() { }

    public void PlayDeathEffect() { }

    public void PlayTamingEffect() { }
}
