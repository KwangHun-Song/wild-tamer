# 월드맵 / 전장의 안개 / 미니맵 — 코드 리뷰

## 리뷰 대상 파일

| 파일 | 역할 | LOC |
|------|------|-----|
| `MapGenerator.cs` | 타일맵 → ObstacleGrid 빌드 (MonoBehaviour) | 40 |
| `ObstacleGrid.cs` | 장애물 그리드, 좌표 변환, 보행 가능 조회 (pure C#) | 56 |
| `FogOfWar.cs` | 시야 관리, 안개 렌더링, 스냅샷 복사/복원 (MonoBehaviour) | 137 |
| `Minimap.cs` | 미니맵 UI, FogOfWar 연동 아이콘 표시 (MonoBehaviour) | 89 |

---

## 긍정적인 점

1. **ObstacleGrid 좌표 변환 정확성**: `WorldToGrid`에서 `FloorToInt((local) / cellSize)` 사용, `GridToWorld`에서 `cellSize * 0.5f` 오프셋으로 셀 중심 반환하는 구현이 수학적으로 정확하다. 경계 밖 좌표에 대해 `IsWalkable`이 `false`를 반환하고, `SetWalkable`이 무시하는 방어 코드도 적절하다.

2. **FogOfWar 3단계 상태 관리**: `FogState.Hidden → Explored → Visible` 전환이 설계 문서와 일치하며, `RevealAround()`에서 기존 Visible을 Explored로 전환한 후 새 시야를 Visible로 설정하는 2-pass 로직이 올바르다. 원형 시야(`dx*dx + dy*dy > r*r`) 판정도 정확하다.

3. **Dirty 플래그 패턴 적용**: `isDirty` 플래그를 사용하여 실제로 상태가 변경된 경우에만 `UpdateTexture()`를 호출한다. 불필요한 `Texture2D.Apply()` 호출을 방지하는 의도가 명확하다.

4. **CopyFogGrid/RestoreFogGrid 구현**: `System.Array.Copy`를 사용한 얕은 복사로 `FogState` enum 배열을 효율적으로 복사한다. GameSnapshot과의 연동 인터페이스(`CopyFogGrid()` → `GameSnapshot.FogGrid` → `RestoreFogGrid()`)가 설계 문서와 일치한다.

5. **Minimap FogOfWar 연동**: 아군은 `fogOfWar == null || IsRevealed`(안개 없으면 표시), 적은 `fogOfWar != null && IsRevealed`(안개 밖이면 숨김)로 분기하여 아군/적의 가시성 정책이 의미상 올바르다.

6. **MapGenerator-ObstacleGrid 연동**: `CompressBounds()` → `cellBounds` → 타일 순회 → `SetWalkable(!hasObstacle)` 흐름이 Tilemap과 walkable 배열을 정확히 연결한다. null 체크 후 기본 그리드를 생성하는 방어 코드도 포함되어 있다.

---

## 이슈

### 1. GameController.CreateSnapshot()에서 FogGrid에 null 전달 (중요도: 높음)

**파일**: `GameController.cs:92`

설계 문서(`game_controller.md:129`)에서는 `fogOfWar.CopyFogGrid()`를 호출하여 안개 상태를 스냅샷에 포함하도록 명시되어 있으나, 실제 구현에서는 `null`을 전달한다.

```csharp
// 현재 구현
return new GameSnapshot(playerPos, squadSnaps, monsterSnaps, null);

// 설계 문서 기준
fogGrid: fogOfWar.CopyFogGrid()
```

이로 인해 `RestoreFromSnapshot()`에서 `fogOfWar.RestoreFogGrid(snapshot.FogGrid)`를 호출하면 `grid == null` 조건에 걸려 안개 상태가 복원되지 않는다. 스냅샷 Save/Load 시 안개 진행 상태가 유실된다.

**제안**: GameController 생성자에 `FogOfWar fogOfWar` 파라미터를 추가하고, `CreateSnapshot()`에서 `fogOfWar.CopyFogGrid()`를 전달해야 한다. `RestoreFromSnapshot()`에도 `fogOfWar.RestoreFogGrid(snapshot.FogGrid)` 호출을 추가해야 한다.

---

### 2. GameController에 FogOfWar, Minimap 참조 누락 — 설계 불일치 (중요도: 높음)

**파일**: `GameController.cs:27-30`

설계 문서(`game_controller.md:40-43, 48-54`)에서는 GameController가 `FogOfWar`, `Minimap`, `cameraTransform`을 생성자에서 주입받고 `Update()`에서 `fogOfWar.RevealAround()`, `minimap.Refresh()`를 호출하도록 명시되어 있다. 그러나 현재 구현에는 이 세 필드와 관련 로직이 모두 빠져 있다.

```csharp
// 설계 문서의 Update()
fogOfWar.RevealAround(Player.Transform.position);        // 누락
minimap.Refresh(Player.Transform, Squad.Members, ...);   // 누락
```

데이터 흐름도(`design.md:160-161`)에도 `fogOfWar.RevealAround(player.position)`과 `minimap.Refresh(...)`가 명시되어 있다.

**제안**: GameController 생성자에 `FogOfWar fogOfWar`, `Minimap minimap` 파라미터를 추가하고, `Update()` 말미에 시야 갱신과 미니맵 갱신 호출을 추가해야 한다.

---

### 3. RestoreFogGrid 배열 크기 불일치 시 예외 발생 가능 (중요도: 중간)

**파일**: `FogOfWar.cs:102-108`

`RestoreFogGrid()`에서 `System.Array.Copy(grid, fogGrid, fogGrid.Length)`를 사용하는데, 전달된 `grid`의 크기가 현재 `fogGrid`의 크기와 다르면 `ArgumentException`이 발생한다. 맵 크기가 변경된 스냅샷을 로드하거나, 잘못된 데이터가 전달될 경우 크래시로 이어진다.

```csharp
public void RestoreFogGrid(FogState[,] grid)
{
    if (grid == null) return;
    // grid.Length != fogGrid.Length이면 ArgumentException
    System.Array.Copy(grid, fogGrid, fogGrid.Length);
    ...
}
```

**제안**: 크기 일치 여부를 검증하는 가드 조건을 추가한다.

```csharp
if (grid.GetLength(0) != gridWidth || grid.GetLength(1) != gridHeight)
{
    Debug.LogWarning("[FogOfWar] RestoreFogGrid: 크기 불일치로 복원을 건너뜁니다.");
    return;
}
```

---

### 4. FogOfWar와 ObstacleGrid 간 그리드 파라미터 불일치 위험 (중요도: 중간)

**파일**: `FogOfWar.cs:12-15`, `MapGenerator.cs:8`

FogOfWar의 `gridWidth`, `gridHeight`, `cellSize`, `origin`은 Inspector에서 직접 설정하는 `[SerializeField]` 값이고, ObstacleGrid는 MapGenerator가 Tilemap의 `cellBounds`에서 동적으로 계산한다. 두 시스템이 동일한 월드 공간을 다른 그리드 파라미터로 표현할 수 있다.

예를 들어 FogOfWar의 `gridWidth=100, origin=(0,0)`이지만 ObstacleGrid가 `width=80, origin=(-10,-10)`이면 좌표 변환 결과가 달라져 시야와 장애물 영역이 어긋난다.

**제안**: MapGenerator.Generate() 이후 FogOfWar가 ObstacleGrid의 파라미터를 참조하여 초기화하는 `Initialize(ObstacleGrid)` 메서드를 추가하거나, 최소한 GameLoop.Start()에서 파라미터 일치를 검증하는 어서션을 추가한다.

---

### 5. Minimap.AdjustIconPool에서 prefab이 null일 때 null 아이콘 추가 (중요도: 중간)

**파일**: `Minimap.cs:77-83`

`prefab == null`인 경우 리스트에 `null`을 추가한다. 이후 `Refresh()`에서 `allyIcons[i].anchoredPosition`이나 `allyIcons[i].gameObject.SetActive()`를 호출할 때 `NullReferenceException`이 발생한다.

```csharp
// AdjustIconPool
if (prefab == null) { pool.Add(null); continue; }  // null 추가

// Refresh (line 54)
allyIcons[i].gameObject.SetActive(show);  // NullReferenceException
```

`Refresh()` 내 아이콘 접근 시 null 체크가 없으므로, prefab이 설정되지 않은 상태에서 아군이나 적이 존재하면 런타임 오류가 발생한다.

**제안**: `AdjustIconPool`에서 prefab이 null이면 아이콘을 생성하지 않고 풀 크기를 늘리지 않도록 하거나, `Refresh()`에서 아이콘 접근 전 null 체크를 추가한다.

```csharp
// 방법 1: AdjustIconPool에서 차단
if (prefab == null) return;

// 방법 2: Refresh에서 null 체크
if (allyIcons[i] != null)
{
    allyIcons[i].gameObject.SetActive(show);
    if (show) allyIcons[i].anchoredPosition = WorldToMinimap(worldPos);
}
```

---

### 6. Minimap.WorldToMinimap 좌표 계산 시 앵커 기준 미고려 (중요도: 중간)

**파일**: `Minimap.cs:69-75`

`WorldToMinimap()`이 `(0, 0) ~ (size.x, size.y)` 범위의 `anchoredPosition`을 반환한다. 이는 RectTransform의 `pivot`이 `(0, 0)` (좌하단)일 때만 올바르다. `pivot`이 `(0.5, 0.5)` (중앙, Unity 기본값)이면 아이콘이 미니맵 영역 밖으로 배치된다.

```csharp
private Vector2 WorldToMinimap(Vector2 worldPos)
{
    var size = minimapRect.rect.size;
    var nx = Mathf.InverseLerp(worldMin.x, worldMax.x, worldPos.x);
    var ny = Mathf.InverseLerp(worldMin.y, worldMax.y, worldPos.y);
    return new Vector2(nx * size.x, ny * size.y);
    // pivot=(0.5, 0.5)이면 (-size/2 ~ +size/2) 범위가 되어야 함
}
```

**제안**: pivot 오프셋을 적용하거나, 미니맵 RectTransform의 pivot을 (0,0)으로 강제하는 방어 코드를 추가한다.

```csharp
var pivot = minimapRect.pivot;
return new Vector2(
    (nx - pivot.x) * size.x,
    (ny - pivot.y) * size.y
);
```

---

### 7. FogOfWar.RevealAround()에서 전체 그리드 순회 — 성능 비효율 (중요도: 중간)

**파일**: `FogOfWar.cs:54-60`

매 호출 시 `Visible → Explored` 전환을 위해 전체 그리드(`gridWidth * gridHeight`)를 순회한다. 기본 설정 `100x100 = 10,000`셀이면 큰 문제가 아니지만, 그리드 크기가 커질 경우(예: 500x500) 매 프레임 250,000회 순회가 발생한다.

```csharp
// 전체 그리드 순회
for (var x = 0; x < gridWidth; x++)
    for (var y = 0; y < gridHeight; y++)
        if (fogGrid[x, y] == FogState.Visible)
        {
            fogGrid[x, y] = FogState.Explored;
            isDirty = true;
        }
```

**제안**: 현재 Visible인 셀 좌표를 `HashSet<Vector2Int>` 또는 `List<Vector2Int>`로 별도 추적하여, Explored 전환 시 해당 셀만 순회하도록 최적화한다.

---

### 8. FogOfWar.UpdateTexture()에서 SetPixel 루프 — GC 및 성능 (중요도: 중간)

**파일**: `FogOfWar.cs:110-128`

`SetPixel(x, y, color)`를 `gridWidth * gridHeight`회 호출한다. Unity의 `SetPixel`은 내부적으로 매번 유효성 검사를 수행하므로, 대량 호출 시 `SetPixels(Color[])` 또는 `SetPixelData<T>()`를 사용하는 것이 상당히 더 효율적이다.

```csharp
for (var x = 0; x < gridWidth; x++)
    for (var y = 0; y < gridHeight; y++)
        fogTexture.SetPixel(x, y, color);  // 10,000회 개별 호출
fogTexture.Apply();
```

**제안**: `Color[]` 배열을 한 번 만들어 `SetPixels()`로 일괄 적용한다.

```csharp
private Color[] colorBuffer; // 생성자에서 new Color[gridWidth * gridHeight]

private void UpdateTexture()
{
    for (var y = 0; y < gridHeight; y++)
        for (var x = 0; x < gridWidth; x++)
            colorBuffer[y * gridWidth + x] = FogStateToColor(fogGrid[x, y]);
    fogTexture.SetPixels(colorBuffer);
    fogTexture.Apply();
    isDirty = false;
}
```

---

### 9. MapGenerator에서 groundTilemap 미사용 (중요도: 낮음)

**파일**: `MapGenerator.cs:6`

`[SerializeField] private Tilemap groundTilemap;` 필드가 선언되어 있으나 `Generate()` 메서드에서 사용되지 않는다. 향후 지면 타일 생성에 사용할 예정이라면 주석으로 의도를 명시하는 것이 좋고, 그렇지 않다면 제거하여 혼동을 방지한다.

**제안**: 미사용 필드라면 제거하거나, 향후 사용 예정이면 `// TODO: 지면 타일 절차 생성 시 사용 예정` 주석을 추가한다.

---

### 10. Minimap.Refresh()에서 SquadMember, Monster 타입 직접 참조 (중요도: 낮음)

**파일**: `Minimap.cs:39-40`

설계 문서에서 Minimap은 MonoBehaviour UI 컴포넌트로 정의되어 있으며, `SquadMember`와 `Monster`라는 Presenter(pure C#) 타입을 직접 참조한다. Minimap이 필요한 정보는 위치(`Transform.position`)뿐이므로, `IReadOnlyList<Transform>` 같은 인터페이스로 받으면 커플링을 줄일 수 있다.

```csharp
public void Refresh(
    Transform player,
    IReadOnlyList<SquadMember> allies,    // Presenter 직접 참조
    IReadOnlyList<Monster> enemies)       // Presenter 직접 참조
```

다만 설계 문서에서도 동일한 시그니처를 사용하고 있으므로 현 시점에서는 설계와 일치한다. 향후 리팩터링 시 고려할 사항이다.

**제안**: 현 단계에서는 설계 일관성을 유지하되, 추후 리팩터링 시 `IReadOnlyList<Transform>` 또는 위치 데이터만 받는 구조로 개선을 고려한다.

---

### 11. FogOfWar에 gridWidth/gridHeight 외부 접근 프로퍼티 부재 (중요도: 낮음)

**파일**: `FogOfWar.cs`

`CopyFogGrid()`가 `FogState[,]`를 반환하지만, 호출 측에서 그리드의 크기를 알 수 없다. `GetLength(0)`, `GetLength(1)`로 배열에서 추출할 수는 있으나, 명시적으로 `GridWidth`, `GridHeight` 프로퍼티를 공개하면 미니맵이나 다른 시스템과의 연동 시 편의성이 높아진다.

**제안**: 읽기 전용 프로퍼티를 추가한다.

```csharp
public int GridWidth => gridWidth;
public int GridHeight => gridHeight;
```

---

## 설계 문서와 구현의 차이

| 항목 | 설계 문서 | 실제 구현 | 판단 |
|------|-----------|-----------|------|
| GameController → FogOfWar 참조 | 생성자에서 주입, Update에서 RevealAround 호출 | 참조 없음, 호출 없음 | **누락** — 안개가 작동하지 않음 |
| GameController → Minimap 참조 | 생성자에서 주입, Update에서 Refresh 호출 | 참조 없음, 호출 없음 | **누락** — 미니맵이 갱신되지 않음 |
| CreateSnapshot → fogGrid | `fogOfWar.CopyFogGrid()` 전달 | `null` 전달 | **누락** — 안개 스냅샷 유실 |
| RestoreFromSnapshot → fogGrid | `fogOfWar.RestoreFogGrid(snapshot.FogGrid)` 호출 | 호출 없음 | **누락** — 안개 복원 불가 |
| VFX 시스템 (HitStop 등) | GameController 생성자에서 생성 | 미구현 | 별도 구현 단계로 추정 (이슈 아님) |
| MapGenerator.cellSize | 설계에 직접 명시 없음 | `[SerializeField]` 기본값 1f | 구현이 합리적 |
| FogOfWar.origin | 설계에 직접 명시 없음 | `[SerializeField]` 기본값 Vector2.zero | 구현이 합리적 |

---

## 코드 리뷰 체크리스트

| 항목 | 충족 | 비고 |
|------|------|------|
| 설계 일관성 | △ | Map 4개 파일 자체는 설계와 일치하나, GameController와의 통합(FogOfWar/Minimap 참조, 스냅샷 연동)이 누락됨 |
| 코딩 컨벤션 준수 | O | Allman 스타일, 접근 제한자 명시, camelCase 필드명, PascalCase 메서드/프로퍼티 등 컨벤션 준수 |
| 에러 처리 | △ | MapGenerator null 체크 양호. RestoreFogGrid 크기 검증 부재, Minimap null 아이콘 접근 위험 |
| 캡슐화 | O | ObstacleGrid 필드 전부 private readonly, FogOfWar 내부 그리드 비공개, 적절한 공개 인터페이스 제공 |
| 테스트 존재 | X | ObstacleGrid는 pure C#로 EditMode 테스트 가능하나 테스트 파일 없음. FogOfWar/Minimap은 MonoBehaviour라 PlayMode 테스트 필요 |

---

## 종합 평가

| 항목 | 등급 | 설명 |
|------|------|------|
| 구조 설계 | **A** | 각 클래스의 책임 분리가 명확하고, ObstacleGrid(pure C#) / MapGenerator(MonoBehaviour) 분리가 설계 기준에 부합 |
| 설계 일관성 | **B** | Map 모듈 자체 구현은 설계와 일치하나, GameController와의 통합 지점(FogOfWar/Minimap 주입, 스냅샷 fogGrid)이 누락됨 |
| 구현 품질 | **B+** | 좌표 변환, 안개 상태 전환, Dirty 플래그 등 핵심 로직이 정확하나, 방어 코드와 성능 최적화에 보완이 필요 |
| 컨벤션 준수 | **A** | 네이밍, 코드 스타일, 접근 제한자 등 코딩 컨벤션을 잘 따르고 있음 |

### 우선 보강이 필요한 3가지

1. **GameController에 FogOfWar/Minimap 통합**: 생성자 주입 + Update() 호출 + CreateSnapshot/RestoreFromSnapshot 연동이 모두 빠져 있어 FogOfWar와 Minimap이 실질적으로 작동하지 않는다. 설계 문서대로 통합이 필요하다. (이슈 #1, #2)
2. **RestoreFogGrid 크기 검증 및 Minimap null 방어**: 배열 크기 불일치 시 예외 방지, null 아이콘 접근 방지 등 런타임 안정성 보강이 필요하다. (이슈 #3, #5)
3. **FogOfWar 텍스처 업데이트 성능 최적화**: SetPixel 루프를 SetPixels 일괄 호출로 전환하고, Visible→Explored 전체 순회를 제거하면 그리드 확장 시 성능 저하를 방지할 수 있다. (이슈 #7, #8)
