using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Base
{
    public class SceneLauncher : MonoBehaviour
    {
        [SerializeField]
        private SceneStateMachine stateMachine;

        private void Start()
        {
            stateMachine.ExecuteAsync().Forget();
        }
    }
}
