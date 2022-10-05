using System;

namespace EntityStorage.Tests
{
    public class Clock : IClock
    {
        public DateTime Now => DateTime.Now;
    }
}