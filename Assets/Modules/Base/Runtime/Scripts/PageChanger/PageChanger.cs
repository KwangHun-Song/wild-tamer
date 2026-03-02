using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Base
{
    public class PageChanger : IPageChanger
    {
        public IPage CurrentPage { get; private set; }

        private readonly Transform container;
        private readonly Notifier notifier;
        private readonly Stack<string> history = new();

        public PageChanger(Transform container, Notifier notifier = null)
        {
            this.container = container;
            this.notifier = notifier;
        }

        public async UniTask ChangePageAsync(string pageName, object param = null)
        {
            await ChangePageInternal(pageName, param, true);
        }

        public void GoBack()
        {
            if (history.Count == 0)
                return;

            ChangePageInternal(history.Pop(), null, false).Forget();
        }

        private async UniTask ChangePageInternal(string pageName, object param, bool addToHistory)
        {
            if (CurrentPage != null)
            {
                if (addToHistory)
                    history.Push(CurrentPage.PageName);

                CurrentPage.Hide();
                Facade.Loader.Unload(CurrentPage.GetGameObject());
                CurrentPage = null;
            }

            var prefab = Resources.Load<GameObject>(pageName);
            if (prefab == null)
            {
                Facade.Logger?.Log($"[PageChanger] Page '{pageName}' not found in Resources", LogLevel.Warning);
                return;
            }

            var instance = Facade.Loader.Load(prefab, container);
            var page = instance.GetComponent<IPage>();
            if (page == null)
            {
                Facade.Logger?.Log($"[PageChanger] IPage component not found on '{pageName}'", LogLevel.Warning);
                Facade.Loader.Unload(instance);
                return;
            }

            CurrentPage = page;
            if (notifier != null && page is Page basePage)
                basePage.Notifier = notifier;
            await page.ShowAsync(param);
        }
    }
}
