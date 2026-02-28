using UnityEngine;

namespace Base
{
    public class DefaultLogger : ILogger
    {
        public LogLevel MinLogLevel { get; set; } = LogLevel.Verbose;

        public void Log(string message, LogLevel level = LogLevel.Info, DebugColor color = DebugColor.Default)
        {
            if (level < MinLogLevel)
                return;

            string colored = color != DebugColor.Default
                ? $"<color={ToColorString(color)}>{message}</color>"
                : message;

            switch (level)
            {
                case LogLevel.Warning:
                    Debug.LogWarning(colored);
                    break;
                case LogLevel.Error:
                    Debug.LogError(colored);
                    break;
                default:
                    Debug.Log(colored);
                    break;
            }
        }

        private static string ToColorString(DebugColor color)
        {
            return color switch
            {
                DebugColor.Red => "#FF6B6B",
                DebugColor.Green => "#7BED9F",
                DebugColor.Blue => "#70A1FF",
                DebugColor.Yellow => "#FFDA79",
                DebugColor.Cyan => "#7EFFF5",
                DebugColor.Magenta => "#E08FFF",
                DebugColor.Orange => "#FFB347",
                DebugColor.White => "#FFFFFF",
                DebugColor.Gray => "#B0B0B0",
                _ => "#FFFFFF",
            };
        }
    }
}
