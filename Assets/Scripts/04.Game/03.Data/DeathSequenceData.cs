using DG.Tweening;
using UnityEngine;

/// <summary>
/// 캐릭터 사망 연출 파라미터를 저장하는 ScriptableObject.
/// Facade.DB.Get&lt;DeathSequenceData&gt;("DeathSequence")로 조회한다.
/// </summary>
[CreateAssetMenu(menuName = "Data/DeathSequence")]
public class DeathSequenceData : ScriptableObject
{
    [Header("Z축 회전 각도 — 양수: 반시계, 음수: 시계 방향")]
    public float rotationDegrees = 90f;

    [Header("회전 연출 시간 (초)")]
    public float rotateDuration = 0.4f;

    [Header("회전 이징")]
    public Ease rotateEase = Ease.OutQuad;

    [Header("회전 완료 후 FadeOut 시작까지의 대기 시간 (초)")]
    public float fadeDelay = 0.1f;

    [Header("FadeOut 시간 (초)")]
    public float fadeDuration = 0.5f;

    [Header("FadeOut 이징")]
    public Ease fadeEase = Ease.InQuad;
}
