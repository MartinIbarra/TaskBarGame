using System;
using TaskbarTactics.Core.Services;

namespace TaskbarTactics.Infrastructure.Time
{
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
