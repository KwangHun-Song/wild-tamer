using Base;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class PlayPage : Page
{
    public override string PageName => "PlayPage";

    [SerializeField] private Button settingButton;

    public event System.Action OnSettingClicked;

    [SerializeField] private Transform worldMapRoot;
    [SerializeField] private Canvas canvas;
    [SerializeField] private WorldMap worldMap;
    [SerializeField] private PlayerView playerView;
    [SerializeField] private PlayerHpBarView playerHpBar;
    [SerializeField] private FogOfWar fogOfWar;
    [SerializeField] private Minimap minimap;

    public Transform WorldMapRoot => worldMapRoot;
    public Canvas Canvas => canvas;
    public WorldMap WorldMap => worldMap;
    public PlayerView PlayerView => playerView;
    public PlayerHpBarView PlayerHpBar => playerHpBar;
    public FogOfWar FogOfWar => fogOfWar;
    public Minimap Minimap => minimap;

    public override UniTask ShowAsync(object param = null)
    {
        settingButton.onClick.AddListener(OnSettingButtonClicked);
        return base.ShowAsync(param);
    }

    public override void Hide()
    {
        settingButton.onClick.RemoveListener(OnSettingButtonClicked);
        base.Hide();
    }

    private void OnSettingButtonClicked()
    {
        OnSettingClicked?.Invoke();
    }
}
