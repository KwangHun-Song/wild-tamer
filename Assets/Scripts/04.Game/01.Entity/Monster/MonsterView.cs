using UnityEngine;

public class MonsterView : CharacterView
{
    [SerializeField] private Animator animator;

    private static readonly int IsMoving = Animator.StringToHash("isMoving");
    private Monster subscribedMonster;

    public void Subscribe(Monster monster)
    {
        subscribedMonster = monster;
        monster.OnMoveRequested += OnMoveRequested;
    }

    public void Unsubscribe()
    {
        if (subscribedMonster != null)
        {
            subscribedMonster.OnMoveRequested -= OnMoveRequested;
            subscribedMonster = null;
        }
    }

    private void OnMoveRequested(Vector2 direction)
    {
        animator.SetBool(IsMoving, direction.sqrMagnitude > 0.01f);
        Movement.Move(direction);
    }

    private void OnDestroy() => Unsubscribe();

    public void PlayHitEffect() { }

    public void PlayDeathEffect() { }

    public void PlayTamingEffect() { }
}
