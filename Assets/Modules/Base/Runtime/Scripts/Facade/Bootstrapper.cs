using UnityEngine;

namespace Base
{
    public static class Bootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // 1. 다른 서비스에서 사용하는 기반 서비스 우선 초기화
            // Facade.Logger = new DefaultLogger();
            // Facade.Json = new DefaultJsonSerializer();
            // Facade.Coroutine = CreateCoroutineRunner();

            // 2. 기반 서비스에 의존하는 서비스 초기화
            // Facade.Data = new DefaultDataStore();
            // Facade.DB = new DefaultDatabase();
            // Facade.Time = new DefaultTimeProvider();

            // 3. Unity 씬/오브젝트 관련 서비스 초기화
            // Facade.Pool = new DefaultObjectPool();
            // Facade.Sound = new DefaultSoundManager();
            // Facade.Scene = new DefaultSceneChanger();
            // Facade.Transition = new DefaultSceneTransition();
        }
    }
}
