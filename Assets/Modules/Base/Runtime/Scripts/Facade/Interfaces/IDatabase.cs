using System.Collections.Generic;

namespace Base
{
    public interface IDatabase
    {
        T Get<T>(string id) where T : class;
        IReadOnlyList<T> GetAll<T>() where T : class;
        bool TryGet<T>(string id, out T result) where T : class;
    }
}
