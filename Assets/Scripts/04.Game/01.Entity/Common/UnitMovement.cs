using UnityEngine;

public class UnitMovement : MonoBehaviour
{
    public float MoveSpeed { get; set; }

    public void Move(Vector2 direction)
    {
        transform.Translate((Vector3)(direction * (MoveSpeed * Time.deltaTime)));
    }

    public void MoveTo(Vector2 target)
    {
        var direction = ((Vector2)transform.position - target).normalized;
        Move(-direction);
    }

    public void Stop()
    {
        // 필요 시 velocity 리셋 (Rigidbody 사용 시 확장)
    }
}
