using UnityEngine;

namespace Base
{
    public interface IInstanceLoader
    {
        GameObject Load(GameObject prefab, Transform parent = null);
        void Unload(GameObject instance);
    }
}
