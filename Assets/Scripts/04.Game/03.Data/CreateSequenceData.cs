using DG.Tweening;
using UnityEngine;

/// <summary>
/// 캐릭터 스폰 연출 파라미터를 저장하는 ScriptableObject.
/// Facade.DB.Get&lt;CreateSequenceData&gt;("CreateSequence")로 조회한다.
/// </summary>
[CreateAssetMenu(menuName = "Data/CreateSequence")]
public class CreateSequenceData : ScriptableObject
{
    [Header("FadeIn 시간 (초)")]
    public float fadeInDuration = 0.4f;

    [Header("FadeIn 이징")]
    public Ease fadeEase = Ease.OutQuad;

    [Header("FadeIn 완료 후 Rise 시작까지의 대기 시간 (초)")]
    public float riseDelay = 0.2f;

    [Header("Rise(회전 복귀) 시간 (초)")]
    public float riseDuration = 0.6f;

    [Header("Rise 이징")]
    public Ease riseEase = Ease.OutBack;
}
