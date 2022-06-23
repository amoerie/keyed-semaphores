using System;
using System.Threading;
using System.Threading.Tasks;

namespace KeyedSemaphores
{
    internal sealed class InternalKeyedSemaphore<TKey> : IKeyedSemaphore<TKey>
    {
        private readonly CancellationToken _cancellationToken;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IKeyedSemaphoresCollection<TKey> _collection;
        private readonly SemaphoreSlim _semaphoreSlim;

        private int _consumers;

        public InternalKeyedSemaphore(TKey key, int consumers, IKeyedSemaphoresCollection<TKey> collection)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            _consumers = consumers;
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _semaphoreSlim = new SemaphoreSlim(1, 1);
            _cancellationTokenSource = new CancellationTokenSource();
            // We need to capture the cancellation token immediately, because _cancellationTokenSource.Token is not safe to call after it has been disposed
            _cancellationToken = _cancellationTokenSource.Token;
        }

        public TKey Key { get; }

        public Task WaitAsync()
        {
            return _semaphoreSlim.WaitAsync(_cancellationToken);
        }

        public Task<bool> WaitAsync(TimeSpan timeout)
        {
            return _semaphoreSlim.WaitAsync(timeout, _cancellationToken);
        }

        public async Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken, cancellationToken);
            return await _semaphoreSlim.WaitAsync(timeout, cts.Token);
        }

        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken, cancellationToken);

            await _semaphoreSlim.WaitAsync(cts.Token);
        }

        public void Wait()
        {
            _semaphoreSlim.Wait(_cancellationToken);
        }

        public bool Wait(TimeSpan timeout)
        {
            return _semaphoreSlim.Wait(timeout, _cancellationToken);
        }

        public bool Wait(TimeSpan timeout, CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken, cancellationToken);

            return _semaphoreSlim.Wait(timeout, cts.Token);
        }

        public void Wait(CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken, cancellationToken);

            _semaphoreSlim.Wait(cts.Token);
        }

        public void Release()
        {
            _semaphoreSlim.Release();
        }

        int IKeyedSemaphore<TKey>.Consumers => _consumers;

        int IKeyedSemaphore<TKey>.IncreaseConsumers()
        {
            return ++_consumers;
        }

        int IKeyedSemaphore<TKey>.DecreaseConsumers()
        {
            return --_consumers;
        }

        void IKeyedSemaphore<TKey>.InternalDispose()
        {
            InternalDispose();
        }

        public void Dispose()
        {
            _collection.Return(this);
        }

        public void InternalDispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _semaphoreSlim.Dispose();
        }
    }
}
