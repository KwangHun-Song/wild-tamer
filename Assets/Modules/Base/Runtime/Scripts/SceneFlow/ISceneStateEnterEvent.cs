namespace Base
{
    public interface ISceneStateEnterEvent : IListener
    {
        void OnSceneStateEnter(SceneState state);
    }
}
