# GameController — 중앙 오케스트레이터

> 상위 문서: [Phase 2 설계](../design.md)

모든 pure C# 시스템을 소유하고 게임 페이즈에 따라 Update와 입력을 게이팅하는 중앙 오케스트레이터. `GameLoop(MonoBehaviour)`가 이 클래스를 생성하고 Unity Update를 브리지한다.

---

## GamePhase

```csharp
public enum GamePhase
{
    Play,               // 정상 플레이
    Paused,             // 일시정지
    UpgradeSelection    // 업그레이드 선택 (Phase 3 대비)
}
```

---

## GameController (pure C#)

```csharp
public class GameController
{
    // 개체
    public Player Player { get; }
    public Squad Squad { get; }

    // 시스템
    private readonly CombatSystem combatSystem;
    private readonly TamingSystem tamingSystem;
    private readonly EntitySpawner entitySpawner;
    private readonly HitStop hitStop;
    private readonly CameraShake cameraShake;
    private readonly HitEffectPlayer hitEffectPlayer;

    // 씬 참조 (MonoBehaviour)
    private readonly PlayerInput playerInput;
    private readonly FogOfWar fogOfWar;
    private readonly Minimap minimap;
    private readonly ObstacleGrid obstacleGrid;

    public Notifier Notifier { get; } = new();
    public GamePhase Phase { get; private set; } = GamePhase.Play;

    public GameController(
        PlayerView playerView,
        PlayerInput playerInput,
        FogOfWar fogOfWar,
        Minimap minimap,
        ObstacleGrid obstacleGrid,
        Transform cameraTransform)
    {
        this.playerInput  = playerInput;
        this.fogOfWar     = fogOfWar;
        this.minimap      = minimap;
        this.obstacleGrid = obstacleGrid;

        // 개체 생성
        var playerCombat = new UnitCombat(10, 1.5f, 5f, 1f);
        Player = new Player(playerView, playerCombat);
        Squad  = new Squad();

        // 시스템 생성
        combatSystem    = new CombatSystem(Notifier);
        entitySpawner   = new EntitySpawner();
        tamingSystem    = new TamingSystem(Squad, entitySpawner, Notifier);
        hitStop         = new HitStop(0.05f, Notifier);
        cameraShake     = new CameraShake(cameraTransform, 0.2f, 0.1f, Notifier);
        hitEffectPlayer = new HitEffectPlayer(/* prefab, sfx */, Notifier);

        // CombatSystem 유닛 등록
        // Player는 생성 시 즉시 등록
        combatSystem.RegisterUnit(Player);

        // Squad 멤버 추가/제거 시 CombatSystem에 자동 등록
        Squad.OnMemberAdded   += combatSystem.RegisterUnit;
        Squad.OnMemberRemoved += combatSystem.UnregisterUnit;

        // Monster 스폰/디스폰 시 CombatSystem에 자동 등록
        entitySpawner.OnMonsterSpawned   += combatSystem.RegisterUnit;
        entitySpawner.OnMonsterDespawned += combatSystem.UnregisterUnit;
    }

    /// <summary>GameLoop(MonoBehaviour)에서 매 프레임 호출한다.</summary>
    public void Update()
    {
        if (Phase != GamePhase.Play) return;

        var dt = Time.deltaTime;

        // 1. 입력 → Player
        Player.Move(playerInput.MoveDirection);
        Player.Combat.Tick(dt);

        // 2. 부대 이동 및 쿨다운
        Squad.Update(Player.Transform, obstacleGrid, dt);

        // 3. 몬스터 AI + 스폰
        entitySpawner.Update(dt);

        // 4. 전투
        combatSystem.Update();

        // 5. 시야 갱신
        fogOfWar.RevealAround(Player.Transform.position);

        // 6. 미니맵
        minimap.Refresh(Player.Transform, Squad.Members, entitySpawner.ActiveMonsters);
    }

    public void SetPhase(GamePhase phase) => Phase = phase;

    // --- 스냅샷 ---

    public GameSnapshot CreateSnapshot()
    {
        var playerPos = (Vector2)Player.Transform.position;
        return new GameSnapshot(
            playerPosition:   playerPos,
            squadMembers:     Squad.Members
                                   .Select(m => new SquadMemberSnapshot(m, playerPos))
                                   .ToList(),
            monsterSnapshots: entitySpawner.ActiveMonsters
                                           .Select(m => new MonsterSnapshot(m))
                                           .ToList(),
            fogGrid:          fogOfWar.CopyFogGrid()
        );
    }

    public void RestoreFromSnapshot(GameSnapshot snapshot)
    {
        // 기존 개체 정리
        foreach (var monster in entitySpawner.ActiveMonsters.ToList())
            entitySpawner.DespawnMonster(monster);

        // 플레이어 위치 복원 (Character.SetPosition 경유, View.transform 직접 접근 금지)
        Player.SetPosition(snapshot.PlayerPosition);

        // 부대원 복원
        Squad.Clear();
        foreach (var memberSnap in snapshot.SquadMembers)
        {
            var pos    = snapshot.PlayerPosition + memberSnap.PositionOffset;
            var member = entitySpawner.SpawnSquadMember(memberSnap.Data, pos);
            Squad.AddMember(member);
        }

        // 안개 복원
        fogOfWar.RestoreFogGrid(snapshot.FogGrid);
    }
}
```

---

## GameLoop (MonoBehaviour)

GameController를 소유하고 Unity Update를 브리지한다.

```csharp
public class GameLoop : MonoBehaviour
{
    [SerializeField] private PlayerView playerView;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private FogOfWar fogOfWar;
    [SerializeField] private Minimap minimap;
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private Transform cameraTransform;

    private GameController gameController;

    private void Start()
    {
        mapGenerator.Generate();
        gameController = new GameController(
            playerView, playerInput, fogOfWar, minimap,
            mapGenerator.ObstacleGrid, cameraTransform);
    }

    private void Update() => gameController?.Update();
}
```

---

## GameSnapshot (pure C#)

GameController의 pure C# 상태를 담는 직렬화 가능한 데이터 클래스. View 참조를 포함하지 않는다.

```csharp
public class GameSnapshot
{
    public Vector2 PlayerPosition { get; }
    public List<SquadMemberSnapshot> SquadMembers { get; }
    public List<MonsterSnapshot> Monsters { get; }
    public FogState[,] FogGrid { get; }

    public GameSnapshot(
        Vector2 playerPosition,
        List<SquadMemberSnapshot> squadMembers,
        List<MonsterSnapshot> monsterSnapshots,
        FogState[,] fogGrid) { ... }
}

public class SquadMemberSnapshot
{
    public MonsterData Data { get; }
    public Vector2 PositionOffset { get; }  // 플레이어 기준 상대 위치
    public int CurrentHp { get; }

    public SquadMemberSnapshot(SquadMember member, Vector2 playerPos)
    {
        Data           = member.Data;
        PositionOffset = (Vector2)member.Transform.position - playerPos;
        CurrentHp      = member.Health.CurrentHp;
    }
}

public class MonsterSnapshot
{
    public MonsterData Data { get; }
    public Vector2 Position { get; }
    public int CurrentHp { get; }

    public MonsterSnapshot(Monster monster)
    {
        Data      = monster.Data;
        Position  = monster.Transform.position;
        CurrentHp = monster.Health.CurrentHp;
    }
}
```
