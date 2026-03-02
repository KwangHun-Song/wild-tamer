using Base;
using UnityEngine;

public class PlayStates : SceneStateMachine
{
    [SerializeField] private Transform pageRoot;
    [SerializeField] private Transform popupRoot;
    [SerializeField] private Camera uiCamera;

    public IPageChanger PageChanger { get; private set; }
    public IPopupManager PopupManager { get; private set; }
    public Notifier SceneNotifier { get; private set; }
    public Camera UICamera => uiCamera;

    private void Awake()
    {
        SceneNotifier = new Notifier();
        PageChanger = new PageChanger(pageRoot, SceneNotifier);
        PopupManager = new PopupManager(popupRoot);
    }
}
