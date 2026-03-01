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
        // x 성분이 충분히 클 때만 flip 갱신 — 미세 진동으로 인한 깜빡임 방지
        if (Mathf.Abs(direction.x) > 0.3f)
            spriteRenderer.flipX = direction.x < 0;
        Movement.Move(direction);
    }
}
