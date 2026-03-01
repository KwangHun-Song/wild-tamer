using UnityEngine;

/// <summary>
/// 유닛 종류별 군집 행동(Flocking) 파라미터를 저장하는 ScriptableObject.
/// DefaultDatabase가 Resources 폴더에서 에셋 이름으로 조회한다.
/// 예) Facade.DB.Get&lt;FlockSettingsData&gt;("Warrior_Flock")
/// </summary>
[CreateAssetMenu(menuName = "Data/FlockSettings")]
public class FlockSettingsData : ScriptableObject
{
    [Header("가중치 (Weights)")]
    [Tooltip("이웃의 평균 이동 방향에 맞추려는 힘의 세기")]
    public float alignmentWeight = 1f;

    [Tooltip("이웃의 무게중심 방향으로 모이려는 힘의 세기")]
    public float cohesionWeight = 1f;

    [Tooltip("이웃과 최소 거리를 유지하려는 반발력의 세기")]
    public float separationWeight = 1.5f;

    [Tooltip("리더(플레이어)를 따라가려는 힘의 세기")]
    public float followWeight = 2f;

    [Tooltip("장애물을 피하려는 힘의 세기")]
    public float avoidanceWeight = 2f;

    [Header("반경 (Radii)")]
    [Tooltip("이 반경 이내의 유닛을 이웃으로 간주한다")]
    public float neighborRadius = 3f;

    [Tooltip("리더와의 거리가 이 이내이면 Follow 힘을 선형 감소시켜 자연스럽게 정지한다")]
    public float arrivalRadius = 1f;

    [Tooltip("유닛 간 유지해야 할 최소 거리. 이보다 가까우면 분리력이 발생한다")]
    public float minSeparationDistance = 0.8f;
}
