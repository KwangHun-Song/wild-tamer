using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Base
{
    public abstract class Page : MonoBehaviour, IPage
    {
        public abstract string PageName { get; }
        public bool IsVisible => gameObject.activeSelf;

        public GameObject GetGameObject() => gameObject;

        public virtual UniTask ShowAsync(object param = null)
        {
            gameObject.SetActive(true);
            return UniTask.CompletedTask;
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
