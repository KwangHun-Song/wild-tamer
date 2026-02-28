public class PlayerView : CharacterView
{
    public void Subscribe(Player player)
    {
        player.OnMoveRequested += direction => Movement.Move(direction);
    }
}
