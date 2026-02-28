using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 한쪽에 미니맵 UI를 표시한다. FogOfWar 데이터 기반으로 탐색 영역을 시각화하고
/// 플레이어·아군·적 위치를 아이콘으로 표시한다.
/// GameController.Update()에서 매 프레임 Refresh()를 호출한다.
/// </summary>
public class Minimap : MonoBehaviour
{
    [SerializeField] private RawImage minimapImage;
    [SerializeField] private RectTransform playerIconPrefab;
    [SerializeField] private RectTransform allyIconPrefab;
    [SerializeField] private RectTransform enemyIconPrefab;

    [SerializeField] private FogOfWar fogOfWar;

    // 미니맵이 표현하는 월드 영역
    [SerializeField] private Vector2 worldMin = Vector2.zero;
    [SerializeField] private Vector2 worldMax = new Vector2(100f, 100f);

    private RectTransform playerIcon;
    private readonly List<RectTransform> allyIcons = new();
    private readonly List<RectTransform> enemyIcons = new();

    private RectTransform minimapRect;

    private void Awake()
    {
        minimapRect = minimapImage != null ? minimapImage.rectTransform : GetComponent<RectTransform>();

        if (playerIconPrefab != null)
            playerIcon = Instantiate(playerIconPrefab, minimapRect);
    }

    public void Refresh(
        Transform player,
        IReadOnlyList<SquadMember> allies,
        IReadOnlyList<Monster> enemies)
    {
        if (minimapRect == null) return;

        // 플레이어 아이콘
        if (playerIcon != null && player != null)
            playerIcon.anchoredPosition = WorldToMinimap(player.position);

        // 아군 아이콘 풀 조정
        AdjustIconPool(allyIcons, allies.Count, allyIconPrefab);
        for (var i = 0; i < allies.Count; i++)
        {
            var worldPos = (Vector2)allies[i].Transform.position;
            var show = fogOfWar == null || fogOfWar.IsRevealed(worldPos);
            allyIcons[i].gameObject.SetActive(show);
            if (show) allyIcons[i].anchoredPosition = WorldToMinimap(worldPos);
        }

        // 적 아이콘 풀 조정
        AdjustIconPool(enemyIcons, enemies.Count, enemyIconPrefab);
        for (var i = 0; i < enemies.Count; i++)
        {
            var worldPos = (Vector2)enemies[i].Transform.position;
            var show = fogOfWar != null && fogOfWar.IsRevealed(worldPos);
            enemyIcons[i].gameObject.SetActive(show);
            if (show) enemyIcons[i].anchoredPosition = WorldToMinimap(worldPos);
        }
    }

    private Vector2 WorldToMinimap(Vector2 worldPos)
    {
        var size = minimapRect.rect.size;
        var nx = Mathf.InverseLerp(worldMin.x, worldMax.x, worldPos.x);
        var ny = Mathf.InverseLerp(worldMin.y, worldMax.y, worldPos.y);
        return new Vector2(nx * size.x, ny * size.y);
    }

    private void AdjustIconPool(List<RectTransform> pool, int needed, RectTransform prefab)
    {
        while (pool.Count < needed)
        {
            if (prefab == null) { pool.Add(null); continue; }
            pool.Add(Instantiate(prefab, minimapRect));
        }
        for (var i = needed; i < pool.Count; i++)
        {
            if (pool[i] != null) pool[i].gameObject.SetActive(false);
        }
    }
}
