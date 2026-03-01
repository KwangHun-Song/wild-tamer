using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public abstract class CharacterView : MonoBehaviour
{
    [SerializeField] private UnitMovement movement;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private string moveAnimTrigger   = "move";
    [SerializeField] private string idleAnimTrigger   = "idle";
    [SerializeField] private string attackAnimTrigger = "attack";

    private const int QueueSize = 5;

    private bool isMovingState = false;
    private readonly Queue<Vector2> directionQueue = new ();

    public UnitMovement Movement => movement;

    protected void HandleAttackRequested()
    {
        isMovingState = false;
        animator.SetTrigger(attackAnimTrigger);
    }

    protected void HandleMoveRequested(Vector2 direction)
    {
        // 실제 속도(유닛/초) 계산 — direction은 0~1 크기의 벡터
        float speed = direction.magnitude * Movement.MoveSpeed;

        // 이동 상태 변화 시에만 트리거 (매 프레임 SetTrigger 방지)
        bool moving = speed > 0.05f && direction.magnitude > 0.01f;
        if (moving != isMovingState)
        {
            animator.SetTrigger(moving ? moveAnimTrigger : idleAnimTrigger);
            isMovingState = moving;
        }

        // 몇프레임동안 같은 방향을 유지했는지 체크한다.
        directionQueue.Enqueue(direction);
        if (directionQueue.Count > QueueSize)
            directionQueue.Dequeue();

        var averageDirection = directionQueue.Aggregate((a, b) => a + b) / directionQueue.Count;

        // 몇 프레임동안 같은 방향을 유지했을 때, X축 방향으로 더 크게 움직였다면 스프라이트를 플립한다.
        var xAxisAbs = Mathf.Abs(averageDirection.x);
        var yAxisAbs = Mathf.Abs(averageDirection.y);
        if (xAxisAbs > yAxisAbs)
        {
            if (direction.x < 0) spriteRenderer.flipX = true;
            else if (direction.x > 0) spriteRenderer.flipX = false;
        }

        Movement.Move(direction);
    }
}
