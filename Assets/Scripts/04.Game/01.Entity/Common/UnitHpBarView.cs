using UnityEngine;

/// <summary>
/// 유닛 위에 표시되는 월드스페이스 HP 바.
/// MaxHp 상태에서는 비활성화, 처음 피해를 받으면 활성화된다.
/// </summary>
public class UnitHpBarView : MonoBehaviour
{
    [SerializeField] private Transform fill;

    private UnitHealth boundHealth;
    private float fullWidth;

    private void Awake()
    {
        var sr = fill?.GetComponent<SpriteRenderer>();
        if (sr?.sprite != null)
            fullWidth = sr.sprite.bounds.size.x;
        gameObject.SetActive(false);
    }

    public void Bind(UnitHealth health)
    {
        if (boundHealth != null)
            boundHealth.OnDamaged -= OnDamaged;
        boundHealth = health;
        boundHealth.OnDamaged += OnDamaged;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnDamaged(int _)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        Refresh();
    }

    private void Refresh()
    {
        if (boundHealth == null || fill == null) return;
        float ratio = (float)boundHealth.CurrentHp / boundHealth.MaxHp;
        fill.localScale = new Vector3(ratio, 1f, 1f);
        var pos = fill.localPosition;
        fill.localPosition = new Vector3(fullWidth * (ratio - 1f) * 0.5f, pos.y, pos.z);
    }

    private void OnDestroy()
    {
        if (boundHealth != null)
            boundHealth.OnDamaged -= OnDamaged;
    }
}
