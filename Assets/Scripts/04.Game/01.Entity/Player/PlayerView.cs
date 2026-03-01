using UnityEngine;

public class PlayerView : CharacterView
{
    [SerializeField] private Animator animator;

    private static readonly int IsMoving = Animator.StringToHash("isMoving");
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

    private void OnMoveRequested(Vector2 direction)
    {
        animator.SetBool(IsMoving, direction.sqrMagnitude > 0.01f);
        Movement.Move(direction);
    }

    private void OnDestroy() => Unsubscribe();
}
