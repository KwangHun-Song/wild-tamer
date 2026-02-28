using Cysharp.Threading.Tasks;

namespace Base
{
    public interface ISceneChanger
    {
        UniTask ChangeSceneAsync(string sceneName);
        string CurrentScene { get; }
    }
}
