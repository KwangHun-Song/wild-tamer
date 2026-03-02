using UnityEngine;

/// <summary>
/// FogOfWar 시야 반경과 상태별 색상을 저장하는 ScriptableObject.
/// DefaultDatabase가 Resources 폴더에서 "FogOfWarData" 이름으로 조회한다.
/// 예) Facade.DB.Get&lt;FogOfWarData&gt;("FogOfWarData")
/// </summary>
[CreateAssetMenu(menuName = "Data/FogOfWarData")]
public class FogOfWarData : ScriptableObject
{
    [Header("시야 반경 — 플레이어 주변 Visible 처리할 셀 개수")]
    public int viewRadius = 5;

    [Header("미탐색 색상 — FogState.Hidden 셀에 적용되는 색상 (기본: 불투명 검정)")]
    public Color hiddenColor = new Color(0f, 0f, 0f, 0.95f);

    [Header("탐색 완료 색상 — FogState.Explored 셀에 적용되는 색상 (기본: 반투명 검정)")]
    public Color exploredColor = new Color(0f, 0f, 0f, 0.5f);

    [Header("현재 시야 색상 — FogState.Visible 셀에 적용되는 색상 (기본: 투명)")]
    public Color visibleColor = new Color(0f, 0f, 0f, 0f);
}
