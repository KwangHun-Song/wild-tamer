using Base;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 게임 플레이 상태의 오케스트레이터.
/// GameController를 생성·관리하고, Unity Update()로 매 프레임 구동한다.
/// PlayPage Canvas에 UICamera를 연결한다.
/// </summary>
public class InPlayState : SceneState
{
    [SerializeField] private PlayerInput   playerInput;
    [SerializeField] private MonsterData[] initialSquadData;
    [SerializeField] private MonsterData[] initialMonsterData;
    [SerializeField] private MonsterData[] monsterSquadSpawnTable;
    [Header("치트 — 숫자키 1·2·3…으로 해당 인덱스의 스쿼드 멤버를 플레이어 주변에 스폰")]
    [SerializeField] private MonsterData[] cheatSquadTypes;

    // SceneState는 MonoBehaviour이므로 Update()가 매 프레임 호출된다.
    // 이전 상태(Init, Load, EnterPage) 동안에는 null이므로 no-op으로 안전하다.
    private GameController gameController;
    private PlayPage playPage;

    protected override async UniTask OnExecuteAsync()
    {
        var playStates = (PlayStates)StateMachine;
        playPage = playStates.PageChanger.CurrentPage as PlayPage;

        if (playPage == null)
        {
            Facade.Logger?.Log("[InPlayState] PlayPage를 찾을 수 없습니다.", LogLevel.Warning);
            return;
        }

        // Canvas에 UICamera 연결 (Screen Space - Camera)
        playPage.Canvas.worldCamera = playStates.UICamera;

        // 카메라가 PlayerView를 추적하도록 연결
        var quarterViewCamera = Camera.main?.GetComponent<QuarterViewCamera>();
        if (quarterViewCamera != null)
            quarterViewCamera.Target = playPage.PlayerView.transform;

        // 맵 생성
        playPage.WorldMap.MapGenerator.Generate();

        // FogOfWar 초기화 (ObstacleGrid 치수를 그대로 복사해 그리드 일치 보장)
        var obstacleGrid = playPage.WorldMap.MapGenerator.ObstacleGrid;
        playPage.FogOfWar?.Initialize(obstacleGrid);

        // Minimap 초기화 (FogOfWar와 동일한 그리드 공유)
        playPage.Minimap?.Initialize(obstacleGrid, playPage.FogOfWar);

        // 플레이어 스폰 위치 적용
        if (playPage.WorldMap.PlayerSpawn != null)
            playPage.PlayerView.transform.position = playPage.WorldMap.PlayerSpawn.position;

        // GameController 생성 → Update()가 즉시 구동 시작
        gameController = new GameController(
            playPage.PlayerView,
            playerInput,
            playPage.WorldMap.MapGenerator.ObstacleGrid,
            Camera.main,
            monsterSquadSpawnTable,
            playPage.WorldMap.UnitRoot);

        // 카메라 셰이크 (플레이어 피격 시)
        CameraShake cameraShake = null;
        if (quarterViewCamera != null)
        {
            var shakeData = Facade.DB.Get<CameraShakeData>("CameraShakeData");
            var intensity = shakeData != null ? shakeData.intensity : 0.1f;
            var duration  = shakeData != null ? shakeData.duration  : 0.2f;
            cameraShake = new CameraShake(quarterViewCamera, gameController.Player, intensity, duration, gameController.Notifier);
        }

        // HP 바 바인딩
        playPage.PlayerHpBar?.Bind(gameController.Player.Health);

        // 테스트용: 초기 부대원(Purple) · 몬스터(Red) 스폰
        var spawnOrigin = playPage.WorldMap.PlayerSpawn != null
            ? (Vector2)playPage.WorldMap.PlayerSpawn.position
            : Vector2.zero;
        gameController.SpawnTestEntities(initialSquadData, initialMonsterData, spawnOrigin);

        try
        {
            await UniTask.WaitUntil(() => false, cancellationToken: this.GetCancellationTokenOnDestroy());
        }
        finally
        {
            cameraShake?.Dispose();
            gameController.Cleanup();
            gameController = null;
        }
    }

    private void Update()
    {
        gameController?.Update();

        if (gameController != null && playPage != null)
        {
            playPage.FogOfWar?.RevealAround(gameController.Player.Transform.position);
            playPage.Minimap?.Refresh(
                gameController.Player,
                gameController.Squad.Members,
                gameController.ActiveMonsters);
        }

        HandleCheatInput();
    }

    private void HandleCheatInput()
    {
        if (gameController == null) return;
        if (cheatSquadTypes == null || cheatSquadTypes.Length == 0) return;

        int count = Mathf.Min(cheatSquadTypes.Length, 9);
        for (int i = 0; i < count; i++)
        {
            if (!Input.GetKeyDown(KeyCode.Alpha1 + i)) continue;
            var data = cheatSquadTypes[i];
            if (data == null) continue;
            var playerPos = (Vector2)gameController.Player.Transform.position;
            var spawnPos  = playerPos + (Vector2)Random.insideUnitCircle.normalized * 2f;
            gameController.CheatSpawnSquadMember(data, spawnPos);
            break;
        }
    }
}
