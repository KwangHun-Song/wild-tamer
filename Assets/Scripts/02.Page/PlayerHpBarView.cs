using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 HP 바 UI. Bind()로 UnitHealth에 연결하면 데미지 이벤트마다 fillAmount를 갱신한다.
/// </summary>
public class PlayerHpBarView : MonoBehaviour
{
    [SerializeField] private Image fill;

    private UnitHealth boundHealth;

    public void Bind(UnitHealth health)
    {
        if (boundHealth != null)
            boundHealth.OnDamaged -= OnDamaged;

        boundHealth = health;
        boundHealth.OnDamaged += OnDamaged;
        Refresh();
    }

    private void OnDamaged(int _) => Refresh();

    private void Refresh()
    {
        if (boundHealth == null || fill == null) return;
        fill.fillAmount = (float)boundHealth.CurrentHp / boundHealth.MaxHp;
    }

    private void OnDestroy()
    {
        if (boundHealth != null)
            boundHealth.OnDamaged -= OnDamaged;
    }
}
