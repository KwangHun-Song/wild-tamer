using UnityEngine;

public abstract class CharacterView : MonoBehaviour
{
    [SerializeField] private UnitMovement movement;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private static readonly int IsMoving = Animator.StringToHash("isMoving");

    public UnitMovement Movement => movement;

    protected void HandleMoveRequested(Vector2 direction)
    {
        animator.SetBool(IsMoving, direction.sqrMagnitude > 0.01f);
        if (direction.x < -0.01f)
            spriteRenderer.flipX = true;
        else if (direction.x > 0.01f)
            spriteRenderer.flipX = false;
        Movement.Move(direction);
    }
}
