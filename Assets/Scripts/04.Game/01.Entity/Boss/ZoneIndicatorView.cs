using DG.Tweening;
using UnityEngine;

/// <summary>
/// 보스 패턴 예고 인디케이터 프리팹 스크립트.
/// Warning(노란색) → Active(빨간색) 색 전환과 Hide(FadeOut)를 처리한다.
/// </summary>
public class ZoneIndicatorView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;

    private static readonly Color WarningColor = new Color(1f, 0.85f, 0f, 0.5f);
    private static readonly Color ActiveColor  = new Color(1f, 0.15f, 0.15f, 0.85f);

    /// <summary>지정 위치·스케일로 인디케이터를 활성화한다.</summary>
    public void Show(Vector2 worldPos, float scaleX, float scaleY)
    {
        transform.position   = new Vector3(worldPos.x, worldPos.y, worldPos.y);
        transform.localScale = new Vector3(scaleX, scaleY, 1f);
        sr.DOKill();
        sr.color = WarningColor;
        gameObject.SetActive(true);
    }

    /// <summary>위치만 갱신한다 (Warning 단계 추적 패턴 전용).</summary>
    public void UpdatePosition(Vector2 worldPos)
    {
        transform.position = new Vector3(worldPos.x, worldPos.y, worldPos.y);
    }

    /// <summary>경고색 → 빨간색 0.3초 DOTween. 활성화 직전 호출.</summary>
    public void FlashActive()
    {
        sr.DOKill();
        sr.DOColor(ActiveColor, 0.3f);
    }

    /// <summary>DOFade 0 후 비활성화.</summary>
    public void Hide()
    {
        sr.DOKill();
        sr.DOFade(0f, 0.2f).OnComplete(() => gameObject.SetActive(false));
    }
}
