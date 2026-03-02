using UnityEngine;

public class QuarterViewCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset;
    [SerializeField] private float smoothSpeed = 5f;

    public Transform Target { set => target = value; }
    public Vector3 ShakeOffset { get; set; }

    private void LateUpdate()
    {
        if (target == null) return;
        // target의 z값(Y-소팅용)은 무시하고 XY만 추적한다
        var desired = new Vector3(target.position.x, target.position.y, 0f) + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime) + ShakeOffset;
    }
}
