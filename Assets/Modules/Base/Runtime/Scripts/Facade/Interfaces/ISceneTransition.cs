using Cysharp.Threading.Tasks;

namespace Base
{
    public interface ISceneTransition
    {
        UniTask TransitionInAsync();
        UniTask TransitionOutAsync();
    }
}
