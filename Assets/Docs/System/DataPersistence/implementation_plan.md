# 데이터 영속성 시스템 구현 계획

## 전제 조건

- 맵은 고정 Tilemap 기반 → 시드 불필요
- `FogOfWar`에 `CopyFogGrid()` / `RestoreFogGrid()` 이미 존재
- `Facade.DataStore` (PlayerPrefs + JSON) 이미 구현됨
- `GameController.RestoreFromSnapshot()` 이미 존재 (활용 방향 전환)

---

## Step 1. DTO 파일 생성

**신규 파일**: `Assets/Scripts/04.Game/03.Data/Save/GameSaveData.cs`

```csharp
using System;

[Serializable]
public class GameSaveData
{
    public float   playerPosX;
    public float   playerPosY;
    public int     playerHp;
    public SquadMemberSaveData[] squadMembers;
    public float   bossElapsedTime;
    public float   bossRespawnTimer;
    public FogSaveData fog;
}

[Serializable]
public class SquadMemberSaveData
{
    public string monsterId;
    public float  offsetX;
    public float  offsetY;
    public int    currentHp;
}

[Serializable]
public class FogSaveData
{
    public int   width;
    public int   height;
    public int[] exploredIndices;
}
```

---

## Step 2. GameSaveManager 생성

**신규 파일**: `Assets/Scripts/04.Game/03.Data/Save/GameSaveManager.cs`

```csharp
using Base;

public static class GameSaveManager
{
    private const string SaveKey = "game_save";

    public static bool HasSave()
        => Facade.DataStore.HasKey(SaveKey);

    public static void Save(GameSaveData data)
        => Facade.DataStore.Save(SaveKey, data);

    /// <summary>로드 후 즉시 삭제 (1회용 Resume).</summary>
    public static GameSaveData Load()
    {
        var data = Facade.DataStore.Load<GameSaveData>(SaveKey, null);
        Facade.DataStore.Delete(SaveKey);
        return data;
    }

    public static void Delete()
        => Facade.DataStore.Delete(SaveKey);
}
```

---

## Step 3. UnitHealth.SetHp 추가

**수정 파일**: `Assets/Scripts/04.Game/01.Entity/Common/UnitHealth.cs`

`Initialize(maxHp)` 아래에 추가:

```csharp
/// <summary>저장 데이터 복원 전용. CurrentHp를 직접 설정한다.</summary>
public void SetHp(int hp)
{
    CurrentHp = Mathf.Clamp(hp, 0, MaxHp);
}
```

---

## Step 4. BossSpawnSystem 타이머 공개

**수정 파일**: `Assets/Scripts/04.Game/02.System/Boss/BossSpawnSystem.cs`

기존 private 필드 옆에 읽기 전용 프로퍼티 추가:

```csharp
public float ElapsedTime  => elapsedTime;
public float RespawnTimer => respawnTimer;
public bool  IsBossActive => activeBoss != null;

public void RestoreTimers(float elapsed, float respawn)
{
    elapsedTime  = elapsed;
    respawnTimer = respawn;
}
```

---

## Step 5. FogOfWar 직렬화 메서드 추가

**수정 파일**: `Assets/Scripts/04.Game/02.System/Map/FogOfWar.cs`

기존 `CopyFogGrid()` 아래에 추가:

```csharp
/// <summary>Hidden이 아닌 셀의 선형 인덱스를 FogSaveData로 반환한다.</summary>
public FogSaveData CreateSaveData()
{
    if (fogGrid == null) return null;
    var indices = new System.Collections.Generic.List<int>();
    for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            if (fogGrid[x, y] != FogState.Hidden)
                indices.Add(y * width + x);
    return new FogSaveData { width = width, height = height, exploredIndices = indices.ToArray() };
}

/// <summary>FogSaveData로 fogGrid를 복원한다. 모두 Explored로 설정 (Visible은 RevealAround에서 갱신).</summary>
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

---

## Step 6. GameController 저장/복원 메서드 추가

**수정 파일**: `Assets/Scripts/04.Game/02.System/Game/GameController.cs`

### 6-1. CanSave 프로퍼티 추가

```csharp
/// <summary>보스 활성 중에는 저장 불가.</summary>
public bool CanSave => bossSpawnSystem == null || !bossSpawnSystem.IsBossActive;
```

### 6-2. CreateSaveData() 추가

```csharp
public GameSaveData CreateSaveData()
{
    var playerPos = (Vector2)Player.Transform.position;
    var members = new SquadMemberSaveData[Squad.Members.Count];
    for (int i = 0; i < Squad.Members.Count; i++)
    {
        var m = Squad.Members[i];
        members[i] = new SquadMemberSaveData
        {
            monsterId = m.Data.id,
            offsetX   = m.Transform.position.x - playerPos.x,
            offsetY   = m.Transform.position.y - playerPos.y,
            currentHp = m.Health.CurrentHp
        };
    }
    return new GameSaveData
    {
        playerPosX       = playerPos.x,
        playerPosY       = playerPos.y,
        playerHp         = Player.Health.CurrentHp,
        squadMembers     = members,
        bossElapsedTime  = bossSpawnSystem?.ElapsedTime  ?? 0f,
        bossRespawnTimer = bossSpawnSystem?.RespawnTimer ?? -1f,
        // fog는 InPlayState에서 주입
    };
}
```

### 6-3. RestoreFrom(GameSaveData) 추가

```csharp
public void RestoreFrom(GameSaveData data)
{
    // 기존 엔티티 제거
    foreach (var sq in entitySpawner.ActiveSquads.ToList())
        entitySpawner.DespawnMonsterSquad(sq);
    foreach (var m in entitySpawner.ActiveMonsters.ToList())
        entitySpawner.DespawnMonster(m);

    // 플레이어 복원
    var playerPos = new Vector2(data.playerPosX, data.playerPosY);
    Player.SetPosition(playerPos);
    Player.Health.SetHp(data.playerHp);

    // 스쿼드 복원
    Squad.Clear();
    if (data.squadMembers != null)
    {
        foreach (var ms in data.squadMembers)
        {
            var monsterData = Facade.DB.Get<MonsterData>(ms.monsterId);
            if (monsterData == null)
            {
                Debug.LogWarning($"[GameController] MonsterData '{ms.monsterId}' 조회 실패. 멤버 스킵.");
                continue;
            }
            var pos    = playerPos + new Vector2(ms.offsetX, ms.offsetY);
            var member = entitySpawner.SpawnSquadMember(monsterData, pos);
            member.Health.SetHp(ms.currentHp);
            Squad.AddMember(member);
        }
    }

    // 보스 타이머 복원
    bossSpawnSystem?.RestoreTimers(data.bossElapsedTime, data.bossRespawnTimer);
}
```

---

## Step 7. InPlayState 저장/로드 연결

**수정 파일**: `Assets/Scripts/01.Scene/PlayScene/States/InPlayState.cs`

### 7-1. OnExecuteAsync() — 로드 분기

GameController 생성 직후:

```csharp
var saveData = GameSaveManager.HasSave() ? GameSaveManager.Load() : null;

if (saveData != null)
{
    playPage.FogOfWar?.RestoreFrom(saveData.fog);
    gameController.RestoreFrom(saveData);
}
else
{
    gameController.SpawnTestEntities(initialSquadData, initialMonsterData, spawnOrigin);
}
```

### 7-2. TrySave() 추가

```csharp
private void TrySave()
{
    if (gameController == null || !gameController.CanSave) return;
    var data = gameController.CreateSaveData();
    data.fog = playPage?.FogOfWar?.CreateSaveData();
    GameSaveManager.Save(data);
}
```

### 7-3. 저장 트리거 추가

```csharp
private void OnApplicationPause(bool paused)
{
    if (paused) TrySave();
}

private void OnApplicationFocus(bool focused)
{
    if (!focused) TrySave();
}
```

---

## Step 8. 게임 오버 시 세이브 삭제 연결

**수정 위치**: 플레이어 사망 처리 로직 (현재 `Player.OnGameOver` 이벤트 구독처)

`InPlayState` 또는 `GameController`에서 `OnGameOver` 수신 시:

```csharp
GameSaveManager.Delete();
```

---

## 구현 순서 요약

| 순서 | 파일 | 작업 | 비고 |
|------|------|------|------|
| 1 | `GameSaveData.cs` (신규) | DTO 3종 정의 | 의존성 없음 |
| 2 | `GameSaveManager.cs` (신규) | Save/Load/Delete/HasSave | Step 1 필요 |
| 3 | `UnitHealth.cs` | `SetHp()` 추가 | 독립적 |
| 4 | `BossSpawnSystem.cs` | 프로퍼티 + `RestoreTimers()` | 독립적 |
| 5 | `FogOfWar.cs` | `CreateSaveData()` + `RestoreFrom()` | Step 1 필요 |
| 6 | `GameController.cs` | `CanSave` + `CreateSaveData()` + `RestoreFrom()` | Step 1~5 필요 |
| 7 | `InPlayState.cs` | 로드 분기 + 저장 트리거 | Step 1~6 필요 |
| 8 | 사망 처리 위치 | `GameSaveManager.Delete()` 연결 | Step 2 필요 |

---

## 검증 체크리스트

- [ ] 신규 게임 시작 → 정상 플레이
- [ ] 플레이 중 앱 종료 → PlayerPrefs에 `game_save` 키 생성 확인
- [ ] 재실행 → 플레이어 위치, HP 복원 확인
- [ ] 재실행 → 스쿼드 멤버 종류/HP/위치 복원 확인
- [ ] 재실행 → FogOfWar 탐험 상태 복원 확인
- [ ] 재실행 → 보스 타이머 연속 진행 확인
- [ ] 보스 전투 중 종료 → 저장 안 됨 확인 (재실행 시 신규 게임)
- [ ] 게임 오버 → 세이브 삭제 확인 (재실행 시 신규 게임)
