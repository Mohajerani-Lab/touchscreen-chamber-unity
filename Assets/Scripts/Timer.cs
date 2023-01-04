using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class Timer
    {
        private long _startTime;
        private long _wait_time;
        public bool _started { get; private set; } = false;

        public void Begin(float seconds)
        {
            _started = true;
            _wait_time = (int) seconds * 1000;
            _startTime = CurrentTimeMillis();
        }

        public long TimePassedInSeconds()
        {
            return (CurrentTimeMillis() - _startTime) / 1000;
        }

        public bool IsFinished()
        {
            if (CurrentTimeMillis() - _startTime < _wait_time) return false;
            _started = false;
            return true;
        }

        public void Clear()
        {
            _started = false;
        }
        
        private long CurrentTimeMillis()
        {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }
    }
}