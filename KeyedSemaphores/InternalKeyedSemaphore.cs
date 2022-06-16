using System;
using System.Threading;
using System.Threading.Tasks;

namespace KeyedSemaphores
{
    internal sealed class InternalKeyedSemaphore<TKey> : IKeyedSemaphore<TKey> where TKey : IEquatable<TKey>
    {
        private readonly IKeyedSemaphoreOwner<TKey> _owner;
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private int _consumers;

        public TKey Key { get; }

        public InternalKeyedSemaphore(TKey key, int consumers, IKeyedSemaphoreOwner<TKey> owner)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            _consumers = consumers;
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _semaphoreSlim = new SemaphoreSlim(1, 1);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public Task WaitAsync()
        {
            return _semaphoreSlim.WaitAsync(_cancellationTokenSource.Token);
        }

        public Task<bool> WaitAsync(TimeSpan timeout)
        {
            return _semaphoreSlim.WaitAsync(timeout, _cancellationTokenSource.Token);
        }

        public async Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token, cancellationToken);

            return await _semaphoreSlim.WaitAsync(timeout, cts.Token);
        }

        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token, cancellationToken);

            await _semaphoreSlim.WaitAsync(cts.Token);
        }

        public void Release()
        {
            _semaphoreSlim.Release();
        }

        int IKeyedSemaphore<TKey>.Consumers => _consumers;

        int IKeyedSemaphore<TKey>.IncreaseConsumers() => ++_consumers;

        int IKeyedSemaphore<TKey>.DecreaseConsumers() => --_consumers;

        void IKeyedSemaphore<TKey>.InternalDispose() => InternalDispose();

        public void Dispose() => _owner.Return(this);

        public void InternalDispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _semaphoreSlim.Dispose();
        }
    }
}
