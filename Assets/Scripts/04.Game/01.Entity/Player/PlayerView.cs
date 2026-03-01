using UnityEngine;

public class PlayerView : CharacterView
{
    private Player subscribedPlayer;

    public void Subscribe(Player player)
    {
        subscribedPlayer = player;
        player.OnMoveRequested += OnMoveRequested;
        player.OnAttackFired   += HandleAttackRequested;
    }

    public void Unsubscribe()
    {
        if (subscribedPlayer != null)
        {
            subscribedPlayer.OnMoveRequested -= OnMoveRequested;
            subscribedPlayer.OnAttackFired   -= HandleAttackRequested;
            subscribedPlayer = null;
        }
    }

    private void OnMoveRequested(Vector2 direction) => HandleMoveRequested(direction);

    private void OnDestroy() => Unsubscribe();
}
