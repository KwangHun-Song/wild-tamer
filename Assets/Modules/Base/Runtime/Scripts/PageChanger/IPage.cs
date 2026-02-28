using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Base
{
    public interface IPage
    {
        string PageName { get; }
        GameObject GetGameObject();
        UniTask ShowAsync(object param = null);
        void Hide();
        bool IsVisible { get; }
    }
}
