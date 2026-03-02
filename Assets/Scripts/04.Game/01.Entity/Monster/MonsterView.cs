using UnityEngine;

public class MonsterView : CharacterView
{
    [SerializeField] private UnitHpBarView hpBar;

    public override void BindHpBar(UnitHealth health) => hpBar?.Bind(health);

    protected override void HideHpBar() => hpBar?.Hide();

    protected override void OnSpawnedFromPool() => hpBar?.Hide();

    public void PlayHitEffect() { }
    public void PlayDeathEffect() { }
    public void PlayTamingEffect() { }
}
