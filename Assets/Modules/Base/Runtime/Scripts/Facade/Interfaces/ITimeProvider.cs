using System;

namespace Base
{
    public interface ITimeProvider
    {
        DateTime Now { get; }
        void JumpSeconds(double seconds);
        void ResetOffset();
    }
}
