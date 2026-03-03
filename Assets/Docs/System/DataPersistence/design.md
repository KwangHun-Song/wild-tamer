# 데이터 영속성 시스템 설계

## 1. 사전 조사에서 확인된 사항

| 항목 | 현황 |
|------|------|
| 저장소 | `Facade.DataStore` (PlayerPrefs + JSON) 이미 구현됨 |
| FogOfWar | `CopyFogGrid()` / `RestoreFogGrid()` 이미 구현됨 |
| GameSnapshot | `GameController.CreateSnapshot()` / `RestoreFromSnapshot()` 이미 구현됨, 단 직렬화 불가 타입 사용 |
| 맵 생성 | Tilemap 에셋 기반 고정 생성 → 시드 불필요 |
| MonsterData 식별 | `MonsterData.id` 문자열 존재 |

---

## 2. 파일 구조

### 신규 파일 (4개)

```
Assets/Scripts/04.Game/03.Data/Save/
├── GameSaveData.cs          ← 최상위 DTO (직렬화 대상)
├── FogSaveData.cs           ← FogOfWar 탐험 셀 저장
└── GameSaveManager.cs       ← Save / Load / Delete / HasSave 진입점
```

### 수정 파일 (4개)

| 파일 | 변경 내용 |
|------|-----------|
| `BossSpawnSystem.cs` | 타이머 프로퍼티 공개 + `RestoreTimers()` 추가 |
| `GameController.cs` | `CanSave`, `CreateSaveData()`, `RestoreFrom(GameSaveData)` 추가 |
| `FogOfWar.cs` | `CreateSaveData()`, `RestoreFrom(FogSaveData)` 추가 |
| `InPlayState.cs` | 로드 분기, 저장 트리거 연결 |

---

## 3. GameSaveData DTO 설계

모든 DTO는 `[System.Serializable]`, Unity 타입 미포함, ScriptableObject 참조 미포함.

```csharp
// GameSaveData.cs
[Serializable]
public class GameSaveData
{
    public float   playerPosX;
    public float   playerPosY;
    public int     playerHp;

    public SquadMemberSaveData[] squadMembers;  // 순서 = 스쿼드 순서

    public float   bossElapsedTime;   // 보스 첫 등장 타이머
    public float   bossRespawnTimer;  // 리스폰 대기 (-1 = 첫 등장 전)

    public FogSaveData fog;
}

[Serializable]
public class SquadMemberSaveData
{
    public string monsterId;    // MonsterData.id
    public float  offsetX;     // 저장 시점 플레이어 기준 상대 위치
    public float  offsetY;
    public int    currentHp;
}

[Serializable]
public class FogSaveData
{
    public int   width;
    public int   height;
    public int[] exploredIndices;   // (y * width + x) 형태의 선형 인덱스
                                    // Hidden이 아닌 셀만 저장
}
```

> `FogSaveData.exploredIndices`: 저장 시 `Hidden`이 아닌 모든 셀(Explored + Visible)을 인덱스로 저장.
> 로드 시 전부 `Explored`로 복원. `Visible`은 첫 `RevealAround()`에서 자연 갱신.

---

## 4. GameSaveManager 설계

```csharp
// GameSaveManager.cs
public static class GameSaveManager
{
    private const string SaveKey = "game_save";

    public static bool HasSave()
        => Facade.DataStore.HasKey(SaveKey);

    public static void Save(GameSaveData data)
        => Facade.DataStore.Save(SaveKey, data);

    public static GameSaveData Load()
    {
        var data = Facade.DataStore.Load<GameSaveData>(SaveKey, null);
        Facade.DataStore.Delete(SaveKey);   // 1회용 Resume: 로드 즉시 삭제
        return data;
    }

    public static void Delete()
        => Facade.DataStore.Delete(SaveKey);
}
```

---

## 5. 컴포넌트별 변경 설계

### 5-1. BossSpawnSystem

```csharp
// 추가할 프로퍼티
public float ElapsedTime  => elapsedTime;
public float RespawnTimer => respawnTimer;
public bool  IsBossActive => activeBoss != null;

// 추가할 메서드
public void RestoreTimers(float elapsed, float respawn)
{
    elapsedTime  = elapsed;
    respawnTimer = respawn;
}
```

### 5-2. GameController

```csharp
// 저장 가능 여부 (보스 전투 중 불가)
public bool CanSave
    => bossSpawnSystem == null || !bossSpawnSystem.IsBossActive;

// 세이브 데이터 생성
public GameSaveData CreateSaveData()
{
    var playerPos = (Vector2)Player.Transform.position;
    var members = Squad.Members.Select(m => new SquadMemberSaveData
    {
        monsterId = m.Data.id,
        offsetX   = m.Transform.position.x - playerPos.x,
        offsetY   = m.Transform.position.y - playerPos.y,
        currentHp = m.Health.CurrentHp
    }).ToArray();

    return new GameSaveData
    {
        playerPosX       = playerPos.x,
        playerPosY       = playerPos.y,
        playerHp         = Player.Health.CurrentHp,
        squadMembers     = members,
        bossElapsedTime  = bossSpawnSystem?.ElapsedTime  ?? 0f,
        bossRespawnTimer = bossSpawnSystem?.RespawnTimer ?? -1f,
    };
    // fog는 InPlayState에서 FogOfWar.CreateSaveData()로 별도 주입
}

// 세이브 데이터로 복원
public void RestoreFrom(GameSaveData data)
{
    // 기존 몬스터 전부 제거
    foreach (var sq in entitySpawner.ActiveSquads.ToList())
        entitySpawner.DespawnMonsterSquad(sq);
    foreach (var m in entitySpawner.ActiveMonsters.ToList())
        entitySpawner.DespawnMonster(m);

    // 플레이어 위치/HP 복원
    Player.SetPosition(new Vector2(data.playerPosX, data.playerPosY));
    Player.Health.SetHp(data.playerHp);

    // 스쿼드 복원
    Squad.Clear();
    var playerPos = new Vector2(data.playerPosX, data.playerPosY);
    foreach (var ms in data.squadMembers)
    {
        var monsterData = Facade.DB.Get<MonsterData>(ms.monsterId);
        if (monsterData == null) continue;
        var pos    = playerPos + new Vector2(ms.offsetX, ms.offsetY);
        var member = entitySpawner.SpawnSquadMember(monsterData, pos);
        member.Health.SetHp(ms.currentHp);
        Squad.AddMember(member);
    }

    // 보스 타이머 복원
    bossSpawnSystem?.RestoreTimers(data.bossElapsedTime, data.bossRespawnTimer);
}
```

### 5-3. FogOfWar

```csharp
// 세이브 데이터 생성 (Hidden이 아닌 셀 → 인덱스 목록)
public FogSaveData CreateSaveData()
{
    var indices = new List<int>();
    for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            if (fogGrid[x, y] != FogState.Hidden)
                indices.Add(y * width + x);

    return new FogSaveData
    {
        width           = width,
        height          = height,
        exploredIndices = indices.ToArray()
    };
}

// 세이브 데이터로 복원 (모두 Explored로 복원, Visible은 RevealAround에서 갱신)
public void RestoreFrom(FogSaveData data)
{
    if (fogGrid == null || data == null) return;
    if (data.width != width || data.height != height)
    {
        Debug.LogWarning("[FogOfWar] RestoreFrom: 맵 크기 불일치로 복원을 건너뜁니다.");
        return;
    }
    foreach (var idx in data.exploredIndices)
    {
        int x = idx % width;
        int y = idx / width;
        if (x >= 0 && x < width && y >= 0 && y < height)
            fogGrid[x, y] = FogState.Explored;
    }
    isDirty = true;
    UpdateTexture();
}
```

### 5-4. InPlayState

```csharp
// OnExecuteAsync() — 맵 생성 이후

var saveData = GameSaveManager.HasSave() ? GameSaveManager.Load() : null;
// Load()는 내부에서 즉시 삭제함

// ... (기존 맵/FogOfWar/Minimap/GameController 초기화) ...

if (saveData != null)
{
    playPage.FogOfWar?.RestoreFrom(saveData.fog);
    gameController.RestoreFrom(saveData);
}
else
{
    gameController.SpawnTestEntities(initialSquadData, initialMonsterData, spawnOrigin);
}

// OnApplicationPause() 추가
private void OnApplicationPause(bool paused)
{
    if (paused) TrySave();
}

private void OnApplicationFocus(bool focused)
{
    if (!focused) TrySave();
}

private void TrySave()
{
    if (gameController == null || !gameController.CanSave) return;
    var data    = gameController.CreateSaveData();
    data.fog    = playPage.FogOfWar?.CreateSaveData();
    GameSaveManager.Save(data);
}
```

---

## 6. UnitHealth.SetHp 추가 필요

현재 `UnitHealth`에 HP를 외부에서 직접 설정하는 메서드가 없다.
복원 시 `member.Health.SetHp(ms.currentHp)`를 호출할 수 있도록 추가한다.

```csharp
/// <summary>저장 데이터 복원 전용. 초기화 없이 CurrentHp를 직접 설정한다.</summary>
public void SetHp(int hp)
{
    CurrentHp = Mathf.Clamp(hp, 0, MaxHp);
}
```

---

## 7. 데이터 흐름

### 저장 흐름

```
앱 포커스 상실 / 일시정지
  └─ InPlayState.TrySave()
       ├─ gameController.CanSave == false? → 종료 (보스 활성)
       ├─ data = gameController.CreateSaveData()
       ├─ data.fog = FogOfWar.CreateSaveData()
       └─ GameSaveManager.Save(data)
            └─ Facade.DataStore.Save("game_save", data)  [JSON → PlayerPrefs]
```

### 로드 흐름

```
앱 실행 → InPlayState.OnExecuteAsync()
  ├─ GameSaveManager.HasSave() → false? → 신규 게임
  └─ true?
       ├─ saveData = GameSaveManager.Load()   [PlayerPrefs → JSON → 역직렬화 + 즉시 삭제]
       ├─ 맵 생성 (기존과 동일, 고정 Tilemap)
       ├─ FogOfWar.Initialize() → FogOfWar.RestoreFrom(saveData.fog)
       └─ GameController 생성 → GameController.RestoreFrom(saveData)
            ├─ Player 위치/HP 복원
            ├─ Squad 멤버 재생성 (Facade.DB.Get(monsterId))
            └─ BossSpawnSystem.RestoreTimers(elapsed, respawn)
```

### 삭제 흐름

```
Load() 호출 시  → 즉시 삭제 (1회용 Resume)
게임 오버       → GameSaveManager.Delete()
새로 시작       → GameSaveManager.Delete()
```

---

## 8. 예외 처리

| 상황 | 처리 |
|------|------|
| MonsterData.id 조회 실패 | 해당 멤버 스킵, 경고 로그 |
| FogSaveData 맵 크기 불일치 | 복원 스킵, 경고 로그 |
| JSON 역직렬화 실패 | DefaultDataStore가 null 반환 → 신규 게임으로 폴백 |
| 세이브 데이터 없음 | HasSave() false → 신규 게임 |
