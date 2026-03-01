using UnityEngine;

public class UnitMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }

    public void Move(Vector2 direction)
    {
        transform.Translate((Vector3)(direction * (MoveSpeed * Time.deltaTime)));
    }

    public void MoveTo(Vector2 target)
    {
        var direction = (target - (Vector2)transform.position).normalized;
        Move(direction);
    }

    public void Stop()
    {
        // 필요 시 velocity 리셋 (Rigidbody 사용 시 확장)
    }
}
