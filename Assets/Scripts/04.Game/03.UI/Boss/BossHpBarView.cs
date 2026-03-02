using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>화면 하단에 고정된 보스 HP 바 UI.</summary>
public class BossHpBarView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image          iconImage;
    [SerializeField] private Image          hpFillImage;

    private BossMonster boundBoss;

    public void Bind(BossMonster boss)
    {
        if (boundBoss != null) Unbind();
        boundBoss = boss;

        nameText.text    = boss.BossData.displayName;
        iconImage.sprite = boss.BossData.icon;
        boss.Health.OnDamaged += OnDamaged;
        boss.Health.OnDeath   += OnDeath;

        gameObject.SetActive(true);
        Refresh(boss.Health);
    }

    private void Unbind()
    {
        if (boundBoss == null) return;
        boundBoss.Health.OnDamaged -= OnDamaged;
        boundBoss.Health.OnDeath   -= OnDeath;
        boundBoss = null;
    }

    private void OnDamaged(int _) => Refresh(boundBoss?.Health);
    private void OnDeath()        => Hide();

    private void Refresh(UnitHealth h)
    {
        if (h == null) return;
        hpFillImage.DOKill();
        hpFillImage.DOFillAmount((float)h.CurrentHp / h.MaxHp, 0.15f);
    }

    public void Hide()
    {
        Unbind();
        gameObject.SetActive(false);
    }
}
