using System;

namespace EntityStorage
{
    public interface IClock
    {
        public DateTime Now { get; }
    }
}