using Cysharp.Threading.Tasks;

namespace Base
{
    public interface IPageChanger
    {
        UniTask ChangePageAsync(string pageName, object param = null);
        void GoBack();
        IPage CurrentPage { get; }
    }
}
