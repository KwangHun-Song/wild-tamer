# 데이터 영속성 시스템 컨셉

## 1. 개요

앱 종료 시 현재 게임 상태를 저장하고, 다음 실행 시 그 상태에서 재개한다.
저장 데이터는 재개 즉시 삭제(1회용 Resume 세이브)한다.

### 기반 인프라

| 역할 | 구성요소 | 특성 |
|------|----------|------|
| 정적 데이터 조회 | `Facade.DB` | 읽기 전용, 런타임 불변 |
| 런타임 데이터 저장/로드 | `Facade.DataStore` | PlayerPrefs + JSON 직렬화 |

런타임 중 변경되는 데이터는 `Facade.DataStore`로만 저장한다.

---

## 2. 런타임 데이터 모델

게임 실행 중 각 엔티티는 다음과 같이 데이터를 보유한다.

```
Monster / SquadMember
├── MonsterData (DB에서 조회한 원본 참조)  ← 런타임에는 id로만 식별
└── 런타임 상태 (HP, 위치 등)              ← 저장 대상
```

`MonsterData`는 ScriptableObject(정적 에셋)이므로 직렬화하지 않는다.
저장 시에는 `MonsterData.id`(문자열)만 기록하고,
로드 시 `Facade.DB.Get<MonsterData>(id)`로 재조회하여 엔티티를 재생성한다.

---

## 3. 저장 데이터 구조

직렬화 가능한 순수 C# 클래스로 구성한다. Unity 타입(Vector2, ScriptableObject 등) 미포함.

```
GameSaveData  [Serializable]
├── playerSave
│   ├── posX, posY    (float)
│   └── currentHp     (int)
├── squadSave
│   └── members[]
│       ├── monsterId  (string)  ← MonsterData.id
│       ├── offsetX/Y  (float)   ← 플레이어 기준 상대 위치
│       └── currentHp  (int)
├── bossSave
│   ├── elapsedTime    (float)   ← 보스 첫 등장까지 경과 시간
│   └── respawnTimer   (float)   ← 리스폰 대기 시간 (-1 = 첫 등장 전)
├── mapSeed            (int)     ← 맵 재생성용 시드
└── fogSave
    ├── width, height  (int)
    └── exploredCells  (int[])   ← Explored 셀의 선형 인덱스 목록
```

---

## 4. 저장 / 로드 / 삭제 타이밍

### 저장 조건
- 보스가 활성 중이면 **저장하지 않는다.**
- 이외의 경우에만 저장한다.

### 저장 트리거
- 앱 포커스 상실 시 (`OnApplicationPause`, `OnApplicationFocus`)
- 일시정지 팝업 진입 시

### 로드 트리거
- `InPlayState.OnExecuteAsync()` 진입 시
  - `GameSaveManager.HasSave()` → true: 저장 상태로 복원
  - false: 신규 게임 시작

### 삭제 트리거
- 로드 완료 직후 (재개 후 즉시 삭제 — 1회용 Resume)
- 게임 오버(플레이어 사망) 시
- 메인 메뉴에서 "새로 시작" 선택 시

---

## 5. 아키텍처

### 5-1. GameSaveManager (순수 C# 정적 클래스)

```csharp
public static class GameSaveManager
{
    public static bool HasSave();
    public static void Save(GameController ctrl, FogOfWar fog, int mapSeed);
    public static GameSaveData Load();   // 로드 후 자동 삭제
    public static void Delete();
}
```

내부적으로 `Facade.DataStore.Save/Load`를 호출한다.

### 5-2. InPlayState 변경

```
OnExecuteAsync():
    saveData = GameSaveManager.HasSave() ? GameSaveManager.Load() : null
    seed = saveData?.mapSeed ?? Random.Range(...)
    MapGenerator.Generate(seed)           ← 시드 기반 생성 (신규)
    FogOfWar.Initialize(...)
    if (saveData != null)
        FogOfWar.RestoreFrom(saveData.fogSave)
        gameController.RestoreFrom(saveData)
    else
        gameController.SpawnTestEntities(...)
```

### 5-3. 저장 가능 여부 판단

`GameController`에 `CanSave` 프로퍼티를 추가한다.

```csharp
// GameController
public bool CanSave => bossSpawnSystem == null || !bossSpawnSystem.IsBossActive;
```

저장 트리거 발생 시 `CanSave`가 false이면 저장을 건너뛴다.

---

## 6. 주요 도전 과제

### 6-1. MapGenerator 시드 지원

현재 맵은 매 실행마다 랜덤 생성된다.
FogOfWar 복원이 의미있으려면 동일한 맵이 재생성되어야 하므로,
`MapGenerator.Generate(int seed)` 오버로드를 추가한다.

### 6-2. FogOfWar 직렬화

`FogState[,]` 2D 배열에서 `Explored` 상태인 셀의 선형 인덱스만 저장한다.

```
저장: exploredCells = [y * width + x | fogGrid[x,y] == Explored]
복원: exploredCells 인덱스를 순회하며 fogGrid 셀을 Explored로 설정
```

### 6-3. BossSpawnSystem 상태 노출

저장/로드를 위해 현재 private인 `elapsedTime`, `respawnTimer`, `IsBossActive`를
읽기 가능하도록 공개하고, 복원 메서드를 추가한다.

```csharp
public float ElapsedTime { get; private set; }
public float RespawnTimer { get; private set; }
public bool  IsBossActive => activeBoss != null;
public void  RestoreTimers(float elapsed, float respawn) { ... }
```

---

## 7. 미저장 항목 (의도적 제외)

| 항목 | 이유 |
|------|------|
| MonsterData 전체 | DB에서 id로 재조회 가능 |
| 일반 몬스터(적) | 로드 후 자연 스폰으로 대체 |
| 보스 상태/HP | 보스 활성 중 저장 미지원 |
| 미니맵 텍스처 | FogOfWar + 맵에서 런타임 재생성 |
| 전투 쿨다운 등 휘발성 상태 | 재시작 시 초기화 허용 |

---

## 8. 구현 단계

| 단계 | 작업 |
|------|------|
| 1 | `GameSaveData` 등 DTO 클래스 정의 |
| 2 | `GameSaveManager` (Save / Load / Delete / HasSave) |
| 3 | `MapGenerator`에 시드 파라미터 추가 |
| 4 | `BossSpawnSystem`에 타이머 프로퍼티 + `RestoreTimers()` 추가 |
| 5 | `GameController.RestoreFrom(GameSaveData)` 구현 |
| 6 | `FogOfWar` 직렬화/역직렬화 |
| 7 | `InPlayState`에서 로드 분기 + 저장 트리거 연결 |
| 8 | 게임 오버/새로 시작 시 삭제 연결 |
