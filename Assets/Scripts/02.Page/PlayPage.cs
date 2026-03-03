using Base;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class PlayPage : Page
{
    public override string PageName => "PlayPage";

    [SerializeField] private Button settingButton;

    [SerializeField] private Transform worldMapRoot;
    [SerializeField] private Canvas canvas;
    [SerializeField] private WorldMap worldMap;
    [SerializeField] private PlayerView playerView;
    [SerializeField] private PlayerHpBarView playerHpBar;
    [SerializeField] private FogOfWar fogOfWar;
    [SerializeField] private Minimap minimap;
    [SerializeField] private BossTimerView   bossTimerView;
    [SerializeField] private BossWarningView bossWarningView;
    [SerializeField] private BossHpBarView   bossHpBarView;

    public Transform WorldMapRoot => worldMapRoot;
    public Canvas Canvas => canvas;
    public WorldMap WorldMap => worldMap;
    public PlayerView PlayerView => playerView;
    public PlayerHpBarView PlayerHpBar => playerHpBar;
    public FogOfWar FogOfWar => fogOfWar;
    public Minimap Minimap => minimap;
    public BossTimerView BossTimerView => bossTimerView;
    public BossWarningView BossWarningView => bossWarningView;
    public BossHpBarView BossHpBarView => bossHpBarView;

    public void OnClickSettingButton()
    {
        Facade.PopupManager.ShowAsync<SettingPopup>("Popups/SettingPopup").Forget();
    }
}
