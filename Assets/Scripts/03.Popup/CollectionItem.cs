using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CollectionItem : MonoBehaviour
{
    [SerializeField] private Image portrait;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI rangeText;

    public void Initialize(MonsterData data)
    {
        portrait.sprite = data.portrait;
        nameText.text = $"{data.displayName}";
        hpText.text = $"HP: {data.maxHp}";
        damageText.text = $"Damage: {data.attackDamage}";
        rangeText.text = $"Range: {data.attackRange.ToString("F1")}";
    }
}
