namespace Base
{
    public static class Facade
    {
        public static ILogger Logger { get; set; } = new DefaultLogger();
        public static IJsonSerializer Json { get; set; } = new DefaultJsonSerializer();
        public static ITimeProvider Time { get; set; } = new DefaultTimeProvider();
        public static IDataStore Data { get; set; } = new DefaultDataStore();
        public static IDatabase DB { get; set; } = new DefaultDatabase();
        public static IObjectPool Pool { get; set; } = new DefaultObjectPool();
        public static ISceneChanger Scene { get; set; } = new DefaultSceneChanger();
        public static ISceneTransition Transition { get; set; } = new DefaultSceneTransition();
        public static IInstanceLoader Loader { get; set; } = new DefaultInstanceLoader();

        // MonoBehaviour 기반 서비스는 Bootstrapper에서 초기화
        public static ICoroutineRunner Coroutine { get; set; }
        public static ISoundManager Sound { get; set; }
        public static IEscapeHandler Escape { get; set; }
    }
}
