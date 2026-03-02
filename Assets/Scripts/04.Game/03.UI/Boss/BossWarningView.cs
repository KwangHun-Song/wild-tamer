using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>보스 등장 경고 연출 UI. 페이드 인 → 유지 → 페이드 아웃 후 onComplete 호출.</summary>
public class BossWarningView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private Image          bossIcon;
    [SerializeField] private CanvasGroup    canvasGroup;

    public void Show(string bossName, Sprite icon, float duration, Action onComplete)
    {
        bossNameText.text = bossName;
        bossIcon.sprite   = icon;
        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;

        DOTween.Sequence()
            .Append(canvasGroup.DOFade(1f, 0.3f))
            .AppendInterval(Mathf.Max(0f, duration - 0.6f))
            .Append(canvasGroup.DOFade(0f, 0.3f))
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            });
    }
}
