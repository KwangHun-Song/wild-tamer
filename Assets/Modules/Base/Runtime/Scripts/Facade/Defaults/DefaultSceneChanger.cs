using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Base
{
    public class DefaultSceneChanger : ISceneChanger
    {
        public string CurrentScene => SceneManager.GetActiveScene().name;

        public async UniTask ChangeSceneAsync(string sceneName)
        {
            if (Facade.Transition != null)
                await Facade.Transition.TransitionInAsync();

            await SceneManager.LoadSceneAsync(sceneName).ToUniTask();

            if (Facade.Transition != null)
                await Facade.Transition.TransitionOutAsync();
        }
    }
}
