using UnityEngine;

public abstract class CharacterView : MonoBehaviour
{
    [SerializeField] private UnitMovement movement;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private string moveAnimTrigger = "move";
    [SerializeField] private string idleAnimTrigger = "idle";
    [SerializeField] private float flipSpeedThreshold = 1f; // 이 속도(유닛/초) 이상에서만 flipX 갱신

    private bool isMovingState = false;

    public UnitMovement Movement => movement;

    protected void HandleMoveRequested(Vector2 direction)
    {
        // 실제 속도(유닛/초) 계산 — direction은 0~1 크기의 벡터
        float speed = direction.magnitude * Movement.MoveSpeed;

        // 이동 상태 변화 시에만 트리거 (매 프레임 SetTrigger 방지)
        bool moving = speed > 0.05f;
        if (moving != isMovingState)
        {
            animator.SetTrigger(moving ? moveAnimTrigger : idleAnimTrigger);
            isMovingState = moving;
        }

        // 속도 임계값 이상에서만 flipX 갱신 — 느린 이동·정지 시 깜빡임 방지
        if (speed > flipSpeedThreshold)
        {
            if (direction.x < 0) spriteRenderer.flipX = true;
            else if (direction.x > 0) spriteRenderer.flipX = false;
        }

        Movement.Move(direction);
    }
}
