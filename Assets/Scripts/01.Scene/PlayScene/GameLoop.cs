using UnityEngine;

/// <summary>
/// GameController를 소유하고 Unity Update를 브리지하는 MonoBehaviour.
/// 씬에 배치하고 SerializeField를 Inspector에서 연결한다.
/// </summary>
public class GameLoop : MonoBehaviour
{
    [SerializeField] private PlayerView playerView;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private MapGenerator mapGenerator;

    private GameController gameController;

    private void Start()
    {
        mapGenerator.Generate();
        gameController = new GameController(playerView, playerInput, mapGenerator.ObstacleGrid);
    }

    private void Update() => gameController?.Update();
}
