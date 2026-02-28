using System;

namespace Base
{
    public interface ITimeProvider
    {
        DateTime Now { get; }
        double OffsetSeconds { get; set; }
    }
}
