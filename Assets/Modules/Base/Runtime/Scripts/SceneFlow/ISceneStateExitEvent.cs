namespace Base
{
    public interface ISceneStateExitEvent : IListener
    {
        void OnSceneStateExit(SceneState state);
    }
}
