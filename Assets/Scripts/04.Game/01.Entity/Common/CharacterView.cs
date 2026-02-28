using UnityEngine;

public abstract class CharacterView : MonoBehaviour
{
    [SerializeField] private UnitMovement movement;

    public UnitMovement Movement => movement;
}
