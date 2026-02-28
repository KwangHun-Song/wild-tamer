using System;

namespace Base
{
    public class DefaultTimeProvider : ITimeProvider
    {
        public DateTime Now => DateTime.UtcNow + TimeSpan.FromSeconds(OffsetSeconds);

        public double OffsetSeconds { get; set; }
    }
}
