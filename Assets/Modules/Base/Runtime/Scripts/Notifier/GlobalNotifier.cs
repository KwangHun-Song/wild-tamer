using System;

namespace Base
{
    /// <summary>
    /// 전역 Notifier. 어디서든 구독·알림을 수행할 수 있다.
    /// 씬 전환 후에도 리스너가 남아 있으므로, OnDestroy 시점에 Unsubscribe를 호출해야 한다.
    /// </summary>
    public static class GlobalNotifier
    {
        private static readonly Notifier notifier = new();

        public static void Subscribe<T>(T listener) where T : IListener
            => notifier.Subscribe(listener);

        public static void Unsubscribe<T>(T listener) where T : IListener
            => notifier.Unsubscribe(listener);

        public static void Notify<T>(Action<T> action) where T : IListener
            => notifier.Notify(action);
    }
}
