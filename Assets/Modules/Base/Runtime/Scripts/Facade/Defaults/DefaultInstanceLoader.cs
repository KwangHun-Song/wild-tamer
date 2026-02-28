using UnityEngine;

namespace Base
{
    public class DefaultInstanceLoader : IInstanceLoader
    {
        public GameObject Load(GameObject prefab, Transform parent = null)
        {
            return Object.Instantiate(prefab, parent);
        }

        public void Unload(GameObject instance)
        {
            Object.Destroy(instance);
        }
    }
}
