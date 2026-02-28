using UnityEngine;

public class PlayerView : CharacterView
{
    private Player subscribedPlayer;

    public void Subscribe(Player player)
    {
        subscribedPlayer = player;
        player.OnMoveRequested += OnMoveRequested;
    }

    public void Unsubscribe()
    {
        if (subscribedPlayer != null)
        {
            subscribedPlayer.OnMoveRequested -= OnMoveRequested;
            subscribedPlayer = null;
        }
    }

    private void OnMoveRequested(Vector2 direction) => Movement.Move(direction);

    private void OnDestroy() => Unsubscribe();
}
