using System;
using System.Threading;

namespace KeyedSemaphores
{
    internal sealed class KeyedSemaphore : IKeyedSemaphore
    {
        private readonly IKeyedSemaphoreOwner _owner;
        private int _consumers;

        public KeyedSemaphore(string key, int consumers, IKeyedSemaphoreOwner owner)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            _consumers = consumers;
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Semaphore = new SemaphoreSlim(1, 1);
        }

        public string Key { get; }

        public SemaphoreSlim Semaphore { get; }
        
        int IKeyedSemaphore.Consumers => _consumers;

        int IKeyedSemaphore.IncreaseConsumers() => ++_consumers;

        int IKeyedSemaphore.DecreaseConsumers() => --_consumers;

        public void Dispose() => _owner.Return(this);
    }
}