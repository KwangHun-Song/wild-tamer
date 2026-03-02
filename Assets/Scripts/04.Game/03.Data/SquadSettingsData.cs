using UnityEngine;

/// <summary>
/// 스쿼드 공통 상수를 저장하는 ScriptableObject.
/// DefaultDatabase가 Resources 폴더에서 "SquadSettings" 이름으로 조회한다.
/// 예) Facade.DB.Get&lt;SquadSettingsData&gt;("SquadSettings")
/// </summary>
[CreateAssetMenu(menuName = "Data/SquadSettings")]
public class SquadSettingsData : ScriptableObject
{
    [Header("정지 반경 — 리더와 이 거리 이내이면 팔로워가 완전 정지한다")]
    public float stopRadius = 1.0f;

    [Header("연쇄 정지 반경 — 이미 정지한 멤버와 이 거리 이내이면 연쇄 정지가 적용된다 (플레이어 스쿼드 전용)")]
    public float memberStopRadius = 1.0f;
}
