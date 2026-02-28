# 2.9 미니맵

> 상위 문서: [Phase 2 설계](../design.md)

화면 한쪽에 미니맵 UI를 표시하여 실시간 탐색 현황을 반영한다. FogOfWar 데이터를 기반으로 탐색 영역을 시각화하고 플레이어·아군·적 위치를 아이콘으로 표시한다.

---

## Minimap (MonoBehaviour)

```csharp
public class Minimap : MonoBehaviour
{
    [SerializeField] private RawImage minimapImage;
    [SerializeField] private RectTransform playerIconPrefab;
    [SerializeField] private RectTransform allyIconPrefab;
    [SerializeField] private RectTransform enemyIconPrefab;

    private FogOfWar fogOfWar;

    public void Refresh(
        Transform player,
        IReadOnlyList<SquadMember> allies,
        IReadOnlyList<Monster> enemies) { ... }
}
```

`GameController.Update()`에서 매 프레임 `Refresh()`를 호출하여 아이콘 위치를 갱신한다.
