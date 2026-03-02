using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 미니맵 UI를 담당한다.
/// Texture2D로 지형을 페인팅하고, Image 아이콘 풀로 유닛 위치를 매 프레임 갱신한다.
/// PlayPage에 직렬화 참조로 포함되며, InPlayState에서 Initialize/Refresh를 호출한다.
/// </summary>
public class Minimap : MonoBehaviour
{
    [Header("UI 레퍼런스")]
    [SerializeField] private RawImage      mapImage;
    [SerializeField] private RectTransform iconContainer;

    [Header("아이콘 프리팹")]
    [SerializeField] private Image playerIconPrefab;
    [SerializeField] private Image allyIconPrefab;
    [SerializeField] private Image enemyIconPrefab;

    [Header("설정")]
    [SerializeField] private int maxTextureResolution  = 256;
    [SerializeField] private int textureRefreshInterval = 30;

    // 런타임 상태
    private ObstacleGrid obstacleGrid;
    private FogOfWar     fogOfWar;     // null 허용 — 없으면 모두 Visible

    private Texture2D    mapTexture;
    private int          texWidth;
    private int          texHeight;

    private Image              playerIcon;
    private readonly List<Image> allyIcons  = new();
    private readonly List<Image> enemyIcons = new();

    private Vector2 mapOrigin;
    private float   mapWorldWidth;
    private float   mapWorldHeight;

    private int frameCounter;

    private static readonly Color ColorWalkable = new Color(0.25f, 0.45f, 0.25f, 1f);
    private static readonly Color ColorObstacle = new Color(0.15f, 0.15f, 0.15f, 1f);
    private static readonly Color ColorHidden   = new Color(0f,    0f,    0f,    1f);

    /// <summary>GameController 생성 직후 InPlayState에서 호출한다.</summary>
    public void Initialize(ObstacleGrid obstacleGrid, FogOfWar fogOfWar = null)
    {
        this.obstacleGrid = obstacleGrid;
        this.fogOfWar     = fogOfWar;

        texWidth  = Mathf.Min(obstacleGrid.Width,  maxTextureResolution);
        texHeight = Mathf.Min(obstacleGrid.Height, maxTextureResolution);

        mapTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, mipChain: false);
        mapTexture.filterMode = FilterMode.Point;
        if (mapImage != null) mapImage.texture = mapTexture;

        mapOrigin      = obstacleGrid.Origin;
        mapWorldWidth  = obstacleGrid.Width  * obstacleGrid.CellSize;
        mapWorldHeight = obstacleGrid.Height * obstacleGrid.CellSize;

        if (playerIconPrefab != null && iconContainer != null)
            playerIcon = Instantiate(playerIconPrefab, iconContainer);

        PaintTexture();
    }

    /// <summary>InPlayState.Update()에서 매 프레임 호출한다.</summary>
    public void Refresh(
        Player                    player,
        IReadOnlyList<SquadMember> allies,
        IReadOnlyList<Monster>     enemies)
    {
        if (mapTexture == null) return;

        // 지형·안개 텍스처 주기적 재페인팅
        frameCounter++;
        if (frameCounter >= textureRefreshInterval)
        {
            frameCounter = 0;
            PaintTexture();
        }

        RefreshIcons(player, allies, enemies);
    }

    private void PaintTexture()
    {
        if (obstacleGrid == null || mapTexture == null) return;

        for (int x = 0; x < texWidth; x++)
        {
            for (int y = 0; y < texHeight; y++)
            {
                int gx = x * obstacleGrid.Width  / texWidth;
                int gy = y * obstacleGrid.Height / texHeight;

                bool walkable = obstacleGrid.IsWalkableAtGrid(gx, gy);
                var  fog      = fogOfWar?.GetState(gx, gy) ?? FogState.Visible;

                var baseColor = walkable ? ColorWalkable : ColorObstacle;
                Color pixel = fog switch
                {
                    FogState.Hidden   => ColorHidden,
                    FogState.Explored => Color.Lerp(baseColor, ColorHidden, 0.6f),
                    _                 => baseColor   // Visible
                };
                mapTexture.SetPixel(x, y, pixel);
            }
        }
        mapTexture.Apply();
    }

    private void RefreshIcons(
        Player                    player,
        IReadOnlyList<SquadMember> allies,
        IReadOnlyList<Monster>     enemies)
    {
        if (iconContainer == null) return;

        // 플레이어 (1개 고정)
        if (playerIcon != null && player != null)
            playerIcon.rectTransform.anchoredPosition =
                WorldToMinimapPos(player.Transform.position);

        // 아군 — 항상 표시
        AdjustPool(allyIcons, allies.Count, allyIconPrefab);
        for (int i = 0; i < allies.Count; i++)
        {
            allyIcons[i].gameObject.SetActive(true);
            allyIcons[i].rectTransform.anchoredPosition =
                WorldToMinimapPos(allies[i].Transform.position);
        }
        HideExcess(allyIcons, allies.Count);

        // 적 — Visible 셀에 있는 적만 표시 (이동하므로 Explored는 의미 없음)
        int shown = 0;
        foreach (var enemy in enemies)
        {
            var g = WorldToGrid(enemy.Transform.position);
            bool visible = fogOfWar == null
                || fogOfWar.GetState(g.x, g.y) == FogState.Visible;
            if (!visible) continue;

            AdjustPool(enemyIcons, shown + 1, enemyIconPrefab);
            enemyIcons[shown].gameObject.SetActive(true);
            enemyIcons[shown].rectTransform.anchoredPosition =
                WorldToMinimapPos(enemy.Transform.position);
            shown++;
        }
        HideExcess(enemyIcons, shown);
    }

    private Vector2 WorldToMinimapPos(Vector2 worldPos)
    {
        float u = Mathf.Clamp01((worldPos.x - mapOrigin.x) / mapWorldWidth);
        float v = Mathf.Clamp01((worldPos.y - mapOrigin.y) / mapWorldHeight);
        var size = iconContainer.rect.size;
        return new Vector2(u * size.x, v * size.y);
    }

    private Vector2Int WorldToGrid(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt((worldPos.x - mapOrigin.x) / obstacleGrid.CellSize),
            Mathf.FloorToInt((worldPos.y - mapOrigin.y) / obstacleGrid.CellSize));
    }

    private void AdjustPool(List<Image> pool, int needed, Image prefab)
    {
        while (pool.Count < needed && prefab != null)
            pool.Add(Instantiate(prefab, iconContainer));
    }

    private void HideExcess(List<Image> pool, int activeCount)
    {
        for (int i = activeCount; i < pool.Count; i++)
            if (pool[i] != null) pool[i].gameObject.SetActive(false);
    }
}
