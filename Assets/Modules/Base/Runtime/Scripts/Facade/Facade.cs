namespace Base
{
    public static class Facade
    {
        public static IDataStore Data { get; set; }
        public static ISceneChanger Scene { get; set; }
        public static IObjectPool Pool { get; set; }
        public static ISoundManager Sound { get; set; }
        public static ILogger Logger { get; set; }
        public static ITimeProvider Time { get; set; }
        public static ISceneTransition Transition { get; set; }
        public static IJsonSerializer Json { get; set; }
        public static IDatabase DB { get; set; }
        public static ICoroutineRunner Coroutine { get; set; }
        public static IInstanceLoader Loader { get; set; }
    }
}
