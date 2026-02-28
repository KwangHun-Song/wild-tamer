using System.Collections.Generic;
using UnityEngine;

namespace Base
{
    public class DefaultObjectPool : IObjectPool
    {
        private readonly Dictionary<int, Queue<GameObject>> pools = new();
        private readonly Dictionary<int, int> instanceToPrefabId = new();

        public GameObject Spawn(GameObject prefab, Transform parent = null)
        {
            int id = prefab.GetInstanceID();

            if (pools.TryGetValue(id, out var pool) && pool.Count > 0)
            {
                var obj = pool.Dequeue();
                obj.transform.SetParent(parent);
                obj.SetActive(true);
                return obj;
            }

            var instance = Object.Instantiate(prefab, parent);
            instanceToPrefabId[instance.GetInstanceID()] = id;
            return instance;
        }

        public void Despawn(GameObject obj)
        {
            int instanceId = obj.GetInstanceID();

            if (!instanceToPrefabId.TryGetValue(instanceId, out int prefabId))
            {
                Object.Destroy(obj);
                return;
            }

            obj.SetActive(false);

            if (!pools.ContainsKey(prefabId))
                pools[prefabId] = new Queue<GameObject>();

            pools[prefabId].Enqueue(obj);
        }

        public void Preload(GameObject prefab, int count)
        {
            int id = prefab.GetInstanceID();

            if (!pools.ContainsKey(id))
                pools[id] = new Queue<GameObject>();

            for (int i = 0; i < count; i++)
            {
                var instance = Object.Instantiate(prefab);
                instance.SetActive(false);
                instanceToPrefabId[instance.GetInstanceID()] = id;
                pools[id].Enqueue(instance);
            }
        }

        public void ClearPool(GameObject prefab = null)
        {
            if (prefab != null)
            {
                int id = prefab.GetInstanceID();
                if (pools.TryGetValue(id, out var pool))
                {
                    while (pool.Count > 0)
                    {
                        var obj = pool.Dequeue();
                        if (obj != null)
                            Object.Destroy(obj);
                    }

                    pools.Remove(id);
                }
            }
            else
            {
                foreach (var pool in pools.Values)
                {
                    while (pool.Count > 0)
                    {
                        var obj = pool.Dequeue();
                        if (obj != null)
                            Object.Destroy(obj);
                    }
                }

                pools.Clear();
                instanceToPrefabId.Clear();
            }
        }
    }
}
