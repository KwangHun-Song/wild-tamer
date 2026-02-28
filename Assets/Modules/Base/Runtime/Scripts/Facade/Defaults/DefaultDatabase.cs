using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Base
{
    public class DefaultDatabase : IDatabase
    {
        private readonly Dictionary<Type, List<ScriptableObject>> cache = new();

        public T Get<T>(string id) where T : class
        {
            if (TryGet<T>(id, out var result))
                return result;

            Facade.Logger?.Log($"[Database] '{id}' not found for type {typeof(T).Name}", LogLevel.Warning);
            return null;
        }

        public IReadOnlyList<T> GetAll<T>() where T : class
        {
            EnsureLoaded<T>();

            if (cache.TryGetValue(typeof(T), out var list))
                return list.OfType<T>().ToList();

            return Array.Empty<T>();
        }

        public bool TryGet<T>(string id, out T result) where T : class
        {
            EnsureLoaded<T>();

            if (cache.TryGetValue(typeof(T), out var list))
            {
                foreach (var item in list)
                {
                    if (item.name == id && item is T typed)
                    {
                        result = typed;
                        return true;
                    }
                }
            }

            result = null;
            return false;
        }

        private void EnsureLoaded<T>() where T : class
        {
            if (cache.ContainsKey(typeof(T)))
                return;

            var assets = Resources.LoadAll<ScriptableObject>("")
                .Where(so => so is T)
                .ToList();

            cache[typeof(T)] = assets;
        }
    }
}
