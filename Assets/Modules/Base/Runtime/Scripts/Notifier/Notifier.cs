using System;
using System.Collections.Generic;
using System.Linq;

namespace Base
{
    public class Notifier
    {
        private Dictionary<Type, List<object>> Listeners { get; } = new();

        private IEnumerable<T> GetListeners<T>() where T : IListener
        {
            if (Listeners.TryGetValue(typeof(T), out var listeners))
            {
                return listeners.OfType<T>();
            }

            return Enumerable.Empty<T>();
        }

        public void Subscribe<T>(T listener) where T : IListener
        {
            var listenerInterfaces = listener.GetType()
                .GetInterfaces()
                .Where(i => typeof(IListener).IsAssignableFrom(i) && i != typeof(IListener));

            foreach (var listenerInterface in listenerInterfaces)
            {
                if (!Listeners.ContainsKey(listenerInterface))
                {
                    Listeners[listenerInterface] = new List<object>();
                }

                if (!Listeners[listenerInterface].Contains(listener))
                {
                    Listeners[listenerInterface].Add(listener);
                }
            }
        }

        public void Unsubscribe<T>(T listener) where T : IListener
        {
            var listenerInterfaces = listener.GetType()
                .GetInterfaces()
                .Where(i => typeof(IListener).IsAssignableFrom(i) && i != typeof(IListener));

            foreach (var listenerInterface in listenerInterfaces)
            {
                if (!Listeners.ContainsKey(listenerInterface))
                {
                    continue;
                }

                Listeners[listenerInterface].Remove(listener);

                if (Listeners[listenerInterface].Count == 0)
                {
                    Listeners.Remove(listenerInterface);
                }
            }
        }

        public void Notify<T>(Action<T> action) where T : IListener
        {
            var snapshot = GetListeners<T>().ToList();

            foreach (var listener in snapshot)
            {
                action?.Invoke(listener);
            }
        }
    }
}