using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Base
{
    public interface IPopup
    {
        string PopupName { get; }
        GameObject GetGameObject();
        UniTask ShowAsync(object enterParam = null);
        void Close(object leaveParam = null);
        bool IsOpen { get; }
    }
}
