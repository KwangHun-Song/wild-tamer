using UnityEngine;

public abstract class CharacterView : MonoBehaviour
{
    [SerializeField] private UnitHealth health;
    [SerializeField] private UnitMovement movement;

    public UnitHealth Health => health;
    public UnitMovement Movement => movement;
}
