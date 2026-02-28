using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Base
{
    public abstract class Popup : MonoBehaviour, IPopup
    {
        private const int SortingOrderInterval = 1000;

        public abstract string PopupName { get; }
        public bool IsOpen { get; private set; }

        protected UniTaskCompletionSource<object> CompletionSource { get; private set; }

        private Canvas canvas;
        protected Canvas Canvas => canvas ??= GetComponentInChildren<Canvas>(true);

        public GameObject GetGameObject() => gameObject;

        public virtual UniTask ShowAsync(object enterParam = null)
        {
            gameObject.SetActive(true);
            return UniTask.CompletedTask;
        }

        public virtual void Close(object leaveParam = null)
        {
            IsOpen = false;
            gameObject.SetActive(false);
            CompletionSource?.TrySetResult(leaveParam);
            CompletionSource = null;
        }

        internal void Open(int stackCount)
        {
            IsOpen = true;
            CompletionSource = new UniTaskCompletionSource<object>();
            if (Canvas != null)
                Canvas.sortingOrder = SortingOrderInterval * stackCount;
        }

        internal UniTask<object> WaitForCloseAsync()
        {
            return CompletionSource.Task;
        }
    }
}
