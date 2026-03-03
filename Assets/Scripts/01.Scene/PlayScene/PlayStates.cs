using Base;
using UnityEngine;

public class PlayStates : SceneStateMachine
{
    [SerializeField] private Transform pageRoot;
    [SerializeField] private Transform popupRoot;
    [SerializeField] private Camera uiCamera;

    public Camera UICamera => uiCamera;
    public GameResult? LastResult { get; set; }

    private void Awake()
    {
        Facade.PageChanger = new PageChanger(pageRoot, Notifier);
        Facade.PopupManager = new PopupManager(popupRoot);
    }

    private void OnDestroy()
    {
        Facade.PageChanger = null;
        Facade.PopupManager = null;
    }
}
