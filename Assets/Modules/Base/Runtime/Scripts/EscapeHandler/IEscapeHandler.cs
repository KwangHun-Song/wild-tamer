namespace Base
{
    public interface IEscapeHandler
    {
        Notifier Notifier { get; }
        void Register(IEscapeListener listener);
        void Unregister(IEscapeListener listener);
    }
}
