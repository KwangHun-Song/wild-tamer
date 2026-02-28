using Cysharp.Threading.Tasks;

namespace Base
{
    public class DefaultSceneTransition : ISceneTransition
    {
        public UniTask TransitionInAsync()
        {
            return UniTask.CompletedTask;
        }

        public UniTask TransitionOutAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}
