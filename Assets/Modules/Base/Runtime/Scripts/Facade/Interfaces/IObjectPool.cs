using UnityEngine;

namespace Base
{
    public interface IObjectPool
    {
        GameObject Spawn(GameObject prefab, Transform parent = null);
        void Despawn(GameObject obj);
        void Preload(GameObject prefab, int count);
        void ClearPool(GameObject prefab = null);
    }
}
