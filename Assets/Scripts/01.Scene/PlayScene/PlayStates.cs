using Base;
using UnityEngine;

public class PlayStates : SceneStateMachine
{
    [SerializeField] private Transform pageRoot;
    [SerializeField] private Transform popupRoot;
    [SerializeField] private Camera uiCamera;
    [SerializeField] private PlayerInput playerInput;

    public IPageChanger PageChanger { get; private set; }
    public IPopupManager PopupManager { get; private set; }
    public Camera UICamera => uiCamera;
    public PlayerInput PlayerInput => playerInput;

    private void Awake()
    {
        PageChanger = new PageChanger(pageRoot);
        PopupManager = new PopupManager(popupRoot);
    }
}
