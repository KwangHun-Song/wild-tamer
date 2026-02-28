using Base;
using UnityEngine;

public class PlayStates : SceneStateMachine
{
    [SerializeField] private Transform pageRoot;
    [SerializeField] private Transform popupRoot;

    public IPageChanger PageChanger { get; private set; }
    public IPopupManager PopupManager { get; private set; }

    private void Awake()
    {
        PageChanger = new PageChanger(pageRoot);
        PopupManager = new PopupManager(popupRoot);
    }
}
