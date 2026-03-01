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

    // SceneState는 MonoBehaviour이므로 Update()가 매 프레임 호출된다.
    // 이전 상태(Init, Load, EnterPage) 동안에는 null이므로 no-op으로 안전하다.
    private GameController gameController;

    protected override async UniTask OnExecuteAsync()
    {
        var playStates = (PlayStates)StateMachine;
        var playPage = playStates.PageChanger.CurrentPage as PlayPage;

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
            gameController.Cleanup();
            gameController = null;
        }
    }

    private void Update() => gameController?.Update();
}
