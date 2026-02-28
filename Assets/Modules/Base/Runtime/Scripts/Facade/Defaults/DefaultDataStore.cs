using System;
using UnityEngine;

namespace Base
{
    public class DefaultDataStore : IDataStore
    {
        public void Save<T>(string key, T data)
        {
            string json = Facade.Json.Serialize(data);
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }

        public T Load<T>(string key, T defaultValue = default)
        {
            if (!PlayerPrefs.HasKey(key))
                return defaultValue;

            string json = PlayerPrefs.GetString(key);

            try
            {
                return Facade.Json.Deserialize<T>(json);
            }
            catch (Exception e)
            {
                Facade.Logger?.Log(
                    $"[DataStore] Failed to deserialize key '{key}' as {typeof(T).Name}: {e.Message}",
                    LogLevel.Warning);
                return defaultValue;
            }
        }

        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        public void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}
