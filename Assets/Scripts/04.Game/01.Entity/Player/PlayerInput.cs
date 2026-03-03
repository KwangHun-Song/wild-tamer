using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [SerializeField] private VirtualJoystick virtualJoystick; // null이면 키보드만 사용

    public Vector2 MoveDirection { get; private set; }

    private void Update()
    {
        var kb  = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        var joy = virtualJoystick != null ? virtualJoystick.Direction : Vector2.zero;
        MoveDirection = (kb + joy).normalized;
    }
}
