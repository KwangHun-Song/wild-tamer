namespace Base
{
    public enum LogLevel
    {
        Verbose,
        Debug,
        Info,
        Warning,
        Error
    }

    public enum DebugColor
    {
        Default,
        Red,
        Green,
        Blue,
        Yellow,
        Cyan,
        Magenta,
        Orange,
        White,
        Gray
    }

    public interface ILogger
    {
        LogLevel MinLogLevel { get; set; }
        void Log(string message, LogLevel level = LogLevel.Info, DebugColor color = DebugColor.Default);
    }
}
