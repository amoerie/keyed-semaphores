using System;

namespace KeyedSemaphores
{
    internal class Unlocker : IDisposable
    {
        private readonly int[] _locks;
        private readonly int _index;

        public Unlocker(int[] locks, int index)
        {
            _locks = locks ?? throw new ArgumentNullException(nameof(locks));
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (index >= locks.Length) throw new ArgumentOutOfRangeException(nameof(index));
            _index = index;
        }

        public void Dispose()
        {
            _locks[_index] = 0;
        }
    }
}
