using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Base
{
    public abstract class Popup : MonoBehaviour, IPopup
    {
        [SerializeField] protected Button curtain;

        private const int SortingOrderInterval = 1000;

        public abstract string PopupName { get; }
        public bool IsOpen { get; private set; }

        protected virtual bool CanClickCurtainToClose => true;

        protected UniTaskCompletionSource<object> CompletionSource { get; private set; }

        private Canvas canvas;
        protected Canvas Canvas => canvas ??= GetComponentInChildren<Canvas>(true);

        public GameObject GetGameObject() => gameObject;

        public virtual UniTask ShowAsync(object enterParam = null)
        {
            gameObject.SetActive(true);
            curtain.onClick.AddListener(OnCurtainClicked);
            return UniTask.CompletedTask;
        }

        public virtual void Close(object leaveParam = null)
        {
            IsOpen = false;
            gameObject.SetActive(false);
            curtain.onClick.RemoveListener(OnCurtainClicked);
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

        internal void OnCurtainClicked()
        {
            if (CanClickCurtainToClose)
                Close(false);
        }

        private void OnValidate()
        {
            if (curtain == null)
            {
                var curtainObject = transform.Find("Curtain");
                if (curtainObject != null)
                {
                    curtain = curtainObject.GetComponent<Button>();
                }
            }
        }

        public void OnClickCloseButton()
        {
            Close(false);
        }
    }
}
