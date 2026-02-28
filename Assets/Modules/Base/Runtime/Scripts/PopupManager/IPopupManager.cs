using Cysharp.Threading.Tasks;

namespace Base
{
    public interface IPopupManager
    {
        UniTask<T> ShowAsync<T>(string popupName, object enterParam = null);
        bool IsPopupOpen(string popupName);
    }
}
