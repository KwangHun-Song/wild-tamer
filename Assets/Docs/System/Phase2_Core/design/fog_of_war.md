# 2.8 전장의 안개 (Fog of War)

> 상위 문서: [Phase 2 설계](../design.md)

미탐색 지역을 어둡게 처리하고 플레이어 이동에 따라 시야를 공개한다. 그리드 기반 `FogState[,]`로 상태를 관리하며 `Texture2D`로 렌더링한다.

---

## FogOfWar (MonoBehaviour)

```csharp
public class FogOfWar : MonoBehaviour
{
    [SerializeField] private int gridWidth;
    [SerializeField] private int gridHeight;
    [SerializeField] private float cellSize;
    [SerializeField] private int viewRadius;
    [SerializeField] private SpriteRenderer fogRenderer;

    private FogState[,] fogGrid;
    private Texture2D fogTexture;

    public void RevealAround(Vector2 worldPos) { ... }
    public bool IsRevealed(Vector2 worldPos) { ... }
    public FogState GetState(int x, int y) { ... }
    public FogState[,] CopyFogGrid() { ... }
    public void RestoreFogGrid(FogState[,] grid) { ... }

    private void UpdateTexture() { ... }
}
```

---

## FogState

```csharp
public enum FogState
{
    Hidden,     // 미탐색 — 완전히 어두움
    Explored,   // 탐색 완료 — 반투명 처리
    Visible     // 현재 시야 내 — 완전히 표시
}
```

| 상태 | 설명 |
|------|------|
| Hidden | 플레이어가 아직 방문하지 않은 영역 |
| Explored | 과거에 방문했으나 현재 시야 밖인 영역 |
| Visible | 현재 플레이어 시야 범위 내 영역 |
