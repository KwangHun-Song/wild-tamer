using UnityEngine;

namespace Base
{
    public static class Bootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // 순수 클래스 기반 서비스는 Facade에서 기본값으로 초기화됨.
            // MonoBehaviour 기반 서비스만 여기서 생성한다.

            var go = new GameObject("[Base Services]");
            Object.DontDestroyOnLoad(go);

            Facade.Coroutine = go.AddComponent<DefaultCoroutineRunner>();
            Facade.Sound = go.AddComponent<DefaultSoundManager>();
            Facade.Escape = go.AddComponent<EscapeHandler>();
        }
    }
}
