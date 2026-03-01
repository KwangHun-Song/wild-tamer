# 월드맵(WorldMap) 구현 계획

## 개요

월드맵은 플레이어, 부대원, 몬스터가 활동하는 게임 월드의 물리적 공간이다.
**스프라이트 타일맵** 기반의 Unity 프리팹으로 구성하며, `PlayPage`의 월드맵 루트에 배치된다.
`MapGenerator`가 타일맵 참조를 받아 `ObstacleGrid`를 빌드하고, `GameLoop`가 이를 `GameController`에 전달한다.

---

## 사용 에셋

| 종류 | 경로 | 용도 |
|------|------|------|
| 지형 타일셋 | `Graphic/Sprites/Terrain/Tileset/Tilemap_color1~5.png` | Ground 타일맵 페인팅 |
| 물 배경 | `Graphic/Sprites/Terrain/Tileset/Water Background color.png` | 수면 타일 |
| 물 거품 | `Graphic/Sprites/Terrain/Tileset/Water Foam.png` | 물가 장식 |
| 그림자 | `Graphic/Sprites/Terrain/Tileset/Shadow.png` | 장애물 그림자 |
| 나무 | `Graphic/Sprites/Terrain/Resources/Wood/Trees/Tree1~4.png` | 장애물/장식 |
| 바위 | `Graphic/Sprites/Terrain/Decorations/Rocks/Rock1~4.png` | 장애물/장식 |
| 덤불 | `Graphic/Sprites/Terrain/Decorations/Bushes/Bushe1~4.png` | 장식 |
| 건물 | `Graphic/Sprites/Buildings/Blue Buildings/House1~3, Castle, Tower.png` | 배경 건물 |

---

## 프리팹 하이에라키 구조

```
WorldMap (프리팹 루트)
├── Grid                          [Grid 컴포넌트]
│   ├── Ground                    [Tilemap + TilemapRenderer] (order: 0)
│   └── Obstacles                 [Tilemap + TilemapRenderer] (order: 1, 반투명)
├── Decorations                   [빈 Transform — 시각 장식 오브젝트]
│   ├── Trees                     [빈 Transform]
│   ├── Rocks                     [빈 Transform]
│   └── Bushes                    [빈 Transform]
├── Buildings                     [빈 Transform — 건물 스프라이트]
├── MapGenerator                  [MapGenerator 컴포넌트]
└── SpawnPoints                   [빈 Transform]
    └── PlayerSpawn               [빈 Transform — 플레이어 초기 위치]
```

---

## 단계별 구현 순서

### Step 1 — 타일셋 스프라이트 설정

타일맵 페인팅에 사용할 스프라이트를 Unity에서 올바르게 설정한다.

**대상 스프라이트:** `Tilemap_color1~5.png`, `Water Background color.png`

Inspector 설정:
- Texture Type: **Sprite (2D and UI)**
- Sprite Mode: **Single** (각 파일이 하나의 타일)
- Pixels Per Unit: **64** (프로젝트 PPU와 일치시킬 것)
- Filter Mode: **Point (no filter)** (픽셀 아트)
- Compression: **None**

> `Tilemap_color1~5`는 색상 변형이므로 각각 독립 타일로 사용한다.

---

### Step 2 — Tile 에셋 생성

Tile Palette에서 사용할 Tile 에셋을 생성한다.

1. `Assets/Prefabs/WorldMap/Tiles/` 폴더 생성
2. Project 창에서 각 스프라이트를 Tile Palette 창으로 드래그 → Tile 에셋 자동 생성
3. 생성된 Tile 에셋이 `Tiles/` 폴더에 저장되었는지 확인

**Tile Palette 생성:**
- Window → 2D → Tile Palette → Create New Palette
- 이름: `TerrainPalette`
- 저장 위치: `Assets/Prefabs/WorldMap/Tiles/`

---

### Step 3 — WorldMap 프리팹 구조 생성

씬에서 WorldMap 오브젝트를 구성하고 프리팹으로 저장한다.

**순서:**

1. Hierarchy에 빈 GameObject 생성 → 이름: `WorldMap`
2. `WorldMap` 하위에 `Grid` 오브젝트 생성 (Grid 컴포넌트 자동 추가됨)
3. `Grid` 하위에 `Ground` Tilemap 오브젝트 생성
   - Tilemap Renderer: Order in Layer = **0**
   - Sorting Layer: **Ground** (없으면 생성)
4. `Grid` 하위에 `Obstacles` Tilemap 오브젝트 생성
   - Tilemap Renderer: Order in Layer = **1**
   - Sorting Layer: **Ground**
   - Tilemap Renderer Material: 반투명 처리 또는 알파 조정 (개발 중 시각화용)
5. `WorldMap` 하위에 빈 GameObject: `Decorations`, `Buildings`, `SpawnPoints` 생성
6. `Decorations` 하위에 `Trees`, `Rocks`, `Bushes` 빈 오브젝트 생성
7. `SpawnPoints` 하위에 `PlayerSpawn` 빈 오브젝트 생성 → 원하는 위치로 이동

---

### Step 4 — MapGenerator 컴포넌트 연결

`WorldMap` 오브젝트에 `MapGenerator` 컴포넌트를 추가하고 타일맵을 연결한다.

1. `WorldMap` (또는 별도 `MapGenerator` 오브젝트)에 `MapGenerator` 컴포넌트 추가
2. Inspector에서:
   - `groundTilemap` → `Grid/Ground` 타일맵 연결
   - `obstacleTilemap` → `Grid/Obstacles` 타일맵 연결

---

### Step 5 — 지형 타일 페인팅 (Ground)

Tile Palette를 사용해 Ground 타일맵에 기본 지형을 배치한다.

**권장 지형 구성:**
- 메인 지형: `Tilemap_color1` (기본 잔디색) — 플레이 가능 영역 전체
- 강/연못: `Water Background color` — 맵 외곽 또는 내부 장애물 구역
- 포인트 지형: `Tilemap_color2~5` — 경로, 광장, 특수 지역 구분용

**페인팅 팁:**
- Active Tilemap: `Ground` 선택 후 페인팅
- 맵 크기 기준: 50×50 타일 (추후 `MapGenerator.Generate()`가 이 크기를 기반으로 ObstacleGrid 생성)

---

### Step 6 — 장애물 타일 배치 (Obstacles)

`Obstacles` 타일맵에 통행 불가 영역을 표시한다.

- Active Tilemap: `Obstacles` 선택
- 물 영역 위에 임시 타일(또는 불투명 타일) 배치
- 건물 위치, 큰 바위 위치에 장애물 타일 배치
- `MapGenerator.Generate()`가 이 타일맵을 읽어 `ObstacleGrid.walkable[x,y]`를 false로 설정

---

### Step 7 — Decoration 오브젝트 배치

스프라이트 기반 장식 오브젝트를 배치한다. Tilemap이 아닌 일반 SpriteRenderer 오브젝트.

**Trees** (`Decorations/Trees` 하위):
- `Tree1~4.png` 스프라이트를 사용한 SpriteRenderer 오브젝트 생성
- Sorting Layer: **Entities**, Order in Layer: Y-Sort (Y좌표 기반 정렬)
- 맵 곳곳에 배치, 장애물 타일과 위치 일치

**Rocks** (`Decorations/Rocks` 하위):
- `Rock1~4.png` SpriteRenderer 오브젝트
- 장애물 영역 위에 배치

**Bushes** (`Decorations/Bushes` 하위):
- `Bushe1~4.png` SpriteRenderer 오브젝트
- 통행 가능 영역에 장식용으로 배치

---

### Step 8 — Buildings 배치

`Buildings` 하위에 건물 스프라이트 오브젝트를 배치한다.

대상 에셋: `Blue Buildings/House1~3.png`, `Castle.png`, `Tower.png`

- 각 건물: SpriteRenderer 오브젝트, Sorting Layer: **Entities**
- 건물 위치에 해당하는 Obstacles 타일 확인 (장애물로 막혀 있어야 함)
- 건물 하단 기준으로 배치 (쿼터뷰 특성상 Y축 정렬)

---

### Step 9 — PlayerSpawn 위치 설정

`SpawnPoints/PlayerSpawn` 오브젝트를 플레이어 초기 소환 위치로 이동한다.

- 맵 중앙 또는 지정 시작 지점으로 이동
- `GameLoop.Start()`에서 `playerView`의 위치를 이 Transform 기준으로 설정해야 함
  (현재 `GameLoop`는 위치 설정 코드가 없으므로, 추후 `Player.SetPosition(spawnPoint.position)` 추가)

---

### Step 10 — 프리팹 저장 및 최종 확인

1. `WorldMap` 오브젝트 선택 → `Assets/Prefabs/WorldMap/WorldMap.prefab`으로 저장
2. 씬에서 WorldMap 인스턴스 제거 (PlayPage 루트에서 런타임 배치)
3. 확인 사항:
   - Grid > Ground/Obstacles 타일맵이 프리팹 내부에 포함되어 있는지
   - MapGenerator의 SerializeField가 프리팹 내부 참조를 가리키는지
   - PlayerSpawn Transform 위치가 유효한 타일 위인지

---

## 검증 체크리스트

- [ ] `TerrainPalette` Tile Palette 생성 및 타일 에셋 존재
- [ ] Ground 타일맵에 50×50 이상의 지형 페인팅 완료
- [ ] Obstacles 타일맵에 장애물 구역 표시 완료
- [ ] Tree, Rock, Bush 오브젝트가 `Decorations` 하위에 배치됨
- [ ] Building 오브젝트가 `Buildings` 하위에 배치됨
- [ ] `PlayerSpawn` 위치가 통행 가능 영역에 있음
- [ ] `MapGenerator`의 `groundTilemap`, `obstacleTilemap` 연결 완료
- [ ] `WorldMap.prefab`으로 저장됨
- [ ] Play Mode에서 `MapGenerator.Generate()` 호출 시 콘솔 에러 없음

---

## 관련 파일

| 파일 | 역할 |
|------|------|
| `Assets/Scripts/04.Game/02.System/Map/MapGenerator.cs` | 타일맵 → ObstacleGrid 변환 |
| `Assets/Scripts/04.Game/02.System/Map/ObstacleGrid.cs` | 보행 가능 여부 O(1) 쿼리 |
| `Assets/Scripts/01.Scene/PlayScene/GameLoop.cs` | MapGenerator.Generate() 호출 |
| `Assets/Scripts/04.Game/02.System/Game/GameController.cs` | ObstacleGrid 수신 및 사용 |
| `Assets/Prefabs/WorldMap/WorldMap.prefab` | (생성 대상) |
| `Assets/Prefabs/WorldMap/Tiles/` | (생성 대상 — Tile 에셋 저장소) |
