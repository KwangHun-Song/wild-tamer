using UnityEngine;

public class SquadMemberView : CharacterView
{
    [SerializeField] private Animator animator;

    private static readonly int IsMoving = Animator.StringToHash("isMoving");
    private SquadMember subscribedMember;

    public void Subscribe(SquadMember member)
    {
        subscribedMember = member;
        member.OnMoveRequested += OnMoveRequested;
    }

    public void Unsubscribe()
    {
        if (subscribedMember != null)
        {
            subscribedMember.OnMoveRequested -= OnMoveRequested;
            subscribedMember = null;
        }
    }

    private void OnMoveRequested(Vector2 direction)
    {
        animator.SetBool(IsMoving, direction.sqrMagnitude > 0.01f);
        Movement.Move(direction);
    }

    private void OnDestroy() => Unsubscribe();
}
