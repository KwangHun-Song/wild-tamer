using System;
using System.Threading;
using Base;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface ICheatListener : IListener
{
    void OnCheatSpawnSquadMember(string id);
    void OnCheatSetBossTimer(float seconds);
}

/// <summary>
/// 게임 플레이 상태의 오케스트레이터.
/// GameController를 생성·관리하고, Unity Update()로 매 프레임 구동한다.
/// 게임 종료(패배/승리) 시 재시작 팝업을 표시하고 PlayPage를 재생성해 루프한다.
/// </summary>
public class InPlayState : SceneState, ICheatListener
{
    [SerializeField] private PlayerInput       playerInput;
    [SerializeField] private MonsterData[]     initialSquadData;
    [SerializeField] private MonsterData[]     initialMonsterData;
    [SerializeField] private MonsterData[]     monsterSquadSpawnTable;
    [Header("보스")]
    [SerializeField] private BossMonsterData[] bossPool;
    [SerializeField] private BossSpawnConfig   bossSpawnConfig;
    [Header("치트 — 숫자키 1·2·3…으로 해당 인덱스의 스쿼드 멤버를 플레이어 주변에 스폰")]
    [SerializeField] private MonsterData[]     cheatSquadTypes;

    private enum GameResult { Win, Lose }

    // SceneState는 MonoBehaviour이므로 Update()가 매 프레임 호출된다.
    // 이전 상태(Init, Load, EnterPage) 동안에는 null이므로 no-op으로 안전하다.
    private GameController gameController;
    private PlayPage       playPage;
    private PlayStates     playStates;

    protected override async UniTask OnExecuteAsync()
    {
        playStates = (PlayStates)StateMachine;
        GlobalNotifier.Subscribe(this);
        var destroyCt = this.GetCancellationTokenOnDestroy();

        try
        {
            while (true)
            {
                playPage = Facade.PageChanger.CurrentPage as PlayPage;
                if (playPage == null)
                {
                    Facade.Logger?.Log("[InPlayState] PlayPage를 찾을 수 없습니다.", LogLevel.Warning);
                    break;
                }

                var result = await RunSessionAsync(destroyCt);

                // 승패 팝업 (버튼 하나 — 재시작)
                bool isWin = result == GameResult.Win;
                var param = new CommonPopupParam(
                    isWin ? "승리!" : "패배",
                    isWin ? "보스를 처치했습니다!" : "쓰러졌습니다.",
                    hasTwoButtons: false,
                    firstButtonText: "재시작");
                await Facade.PopupManager.ShowAsync<bool>("Popups/CommonPopup", param);

                // PlayPage 재생성 → 재시작 루프
                await Facade.PageChanger.ChangePageAsync("PlayPage");
            }
        }
        catch (OperationCanceledException)
        {
            // 정상 종료 (씬 전환 또는 오브젝트 파괴)
        }
    }

    protected override void OnLeave()
    {
        GlobalNotifier.Unsubscribe(this);
    }

    /// <summary>한 게임 세션을 실행하고 승패 결과를 반환한다.</summary>
    private async UniTask<GameResult> RunSessionAsync(CancellationToken ct)
    {
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
            obstacleGrid,
            Camera.main,
            monsterSquadSpawnTable,
            playPage.WorldMap.UnitRoot,
            bossPool,
            playPage.BossWarningView,
            playPage.BossHpBarView,
            playPage.BossTimerView,
            bossSpawnConfig);

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

        // 저장 데이터가 있으면 복원, 없으면 신규 게임 시작
        var saveData = GameSaveManager.HasSave() ? GameSaveManager.Load() : null;
        if (saveData != null)
        {
            playPage.FogOfWar?.RestoreFrom(saveData.fog);
            gameController.RestoreFrom(saveData);
        }
        else
        {
            var spawnOrigin = playPage.WorldMap.PlayerSpawn != null
                ? (Vector2)playPage.WorldMap.PlayerSpawn.position
                : Vector2.zero;
            gameController.SpawnTestEntities(initialSquadData, initialMonsterData, spawnOrigin);
        }

        // 양 경로 공통: 현재 스쿼드 멤버를 컬렉션에 등록 (중복 무시)
        foreach (var member in gameController.Squad.Members)
            UserData.AddTamedMonster(member.Data.name);

        GameSaveManager.OnSaveRequested += TrySave;
        try
        {
            return await WaitForGameEndAsync(gameController, ct);
        }
        finally
        {
            GameSaveManager.OnSaveRequested -= TrySave;
            cameraShake?.Dispose();
            gameController.Cleanup();
            gameController = null;
        }
    }

    /// <summary>OnPlayerDied 또는 OnBossDefeated 이벤트를 기다려 결과를 반환한다.</summary>
    private static async UniTask<GameResult> WaitForGameEndAsync(GameController gc, CancellationToken ct)
    {
        var tcs = new UniTaskCompletionSource<GameResult>();
        Action onDied     = () => tcs.TrySetResult(GameResult.Lose);
        Action onDefeated = () => tcs.TrySetResult(GameResult.Win);
        gc.OnPlayerDied   += onDied;
        gc.OnBossDefeated += onDefeated;
        try
        {
            return await tcs.Task.AttachExternalCancellation(ct);
        }
        finally
        {
            gc.OnPlayerDied   -= onDied;
            gc.OnBossDefeated -= onDefeated;
        }
    }

    private void Update()
    {
        if (Facade.PopupManager.IsAnyPopupOpen()) return;

        gameController?.Update();

        if (gameController != null && playPage != null)
        {
            playPage.FogOfWar?.RevealAround(gameController.Player.Transform.position);
            playPage.Minimap?.Refresh(
                gameController.Player,
                gameController.Squad.Members,
                gameController.ActiveMonsters);
        }
    }

    private void TrySave()
    {
        if (gameController == null || !gameController.CanSave) return;
        var data = gameController.CreateSaveData();
        data.fog = playPage?.FogOfWar?.CreateSaveData();
        GameSaveManager.Save(data);
    }

    #region ICheatListener

    public void OnCheatSpawnSquadMember(string id)
    {
        SpawnSquadMember(id);
    }

    public void OnCheatSetBossTimer(float seconds)
    {
        gameController?.CheatSetBossTimer(seconds);
    }

    #endregion

    private void SpawnSquadMember(string id)
    {
        var monstarData = Facade.DB.Get<MonsterData>(id);
        var playerPos   = (Vector2)gameController.Player.Transform.position;
        var spawnPos    = playerPos + (Vector2)Random.insideUnitCircle.normalized * 2f;
        gameController.CheatSpawnSquadMember(monstarData, spawnPos);
    }
}
