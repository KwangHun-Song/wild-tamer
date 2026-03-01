using UnityEngine;

public class SquadMemberView : CharacterView
{
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

    private void OnMoveRequested(Vector2 direction) => HandleMoveRequested(direction);

    private void OnDestroy() => Unsubscribe();

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (subscribedMember == null)
            return;

        var data   = subscribedMember.FlockDebug;
        var origin = (Vector2)transform.position;

        DrawDebugVector(origin, data.Cohesion,   Color.green,  "C");
        DrawDebugVector(origin, data.Separation, Color.red,    "S");
        DrawDebugVector(origin, data.Follow,     Color.blue,   "F");
        DrawDebugVector(origin, data.Avoidance,  Color.yellow, "A");
        DrawDebugVector(origin, data.Combined,   Color.white,  "X");
    }

    private static void DrawDebugVector(Vector2 origin, Vector2 vec, Color color, string label)
    {
        if (vec == Vector2.zero)
            return;

        Gizmos.color = color;
        Gizmos.DrawLine(origin, origin + vec);
        UnityEditor.Handles.Label(origin + vec, $"{label}:({vec.x:F2}, {vec.y:F2})");
    }
#endif
}
