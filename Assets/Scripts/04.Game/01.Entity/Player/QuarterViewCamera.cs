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
        var desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime) + ShakeOffset;
    }
}
