using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Base
{
    public abstract class SceneState : MonoBehaviour
    {
        public SceneStateMachine StateMachine { get; private set; }

        public void OnSetUp(SceneStateMachine stateMachine)
        {
            StateMachine = stateMachine;
        }

        public virtual bool CanEnter()
        {
            return true;
        }

        protected abstract UniTask OnExecuteAsync();
        public virtual void OnLeave() { }

        internal UniTask ExecuteAsync()
        {
            return OnExecuteAsync();
        }
    }
}
