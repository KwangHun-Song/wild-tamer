using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Base
{
    public class PopupManager : IPopupManager
    {
        private readonly Stack<Popup> popupStack = new();
        private readonly Transform container;

        public PopupManager(Transform container)
        {
            this.container = container;
        }

        public async UniTask<T> ShowAsync<T>(string popupName, object enterParam = null)
        {
            var prefab = Resources.Load<GameObject>(popupName);
            if (prefab == null)
            {
                Facade.Logger?.Log($"[PopupManager] Popup '{popupName}' not found in Resources", LogLevel.Warning);
                return default;
            }

            var instance = Facade.Loader.Load(prefab, container);
            var popup = instance.GetComponent<Popup>();
            if (popup == null)
            {
                Facade.Logger?.Log($"[PopupManager] Popup component not found on '{popupName}'", LogLevel.Warning);
                Facade.Loader.Unload(instance);
                return default;
            }

            popupStack.Push(popup);
            popup.Open(popupStack.Count);
            await popup.ShowAsync(enterParam);

            var result = await popup.WaitForCloseAsync();

            popupStack.Pop();
            Facade.Loader.Unload(popup.GetGameObject());

            return result is T casted ? casted : default;
        }

        public bool IsPopupOpen(string popupName)
        {
            foreach (var popup in popupStack)
            {
                if (popup.PopupName == popupName && popup.IsOpen)
                    return true;
            }
            return false;
        }

        public bool IsAnyPopupOpen()
        {
            return popupStack.Count > 0;
        }
    }
}
