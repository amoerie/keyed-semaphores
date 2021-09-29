using System;
using System.Threading;
using System.Threading.Tasks;

namespace KeyedSemaphores
{
    internal sealed class InternalKeyedSemaphore : IKeyedSemaphore
    {
        private readonly IKeyedSemaphoreOwner _owner;
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;
        
        private int _consumers;

        public string Key { get; }

        public InternalKeyedSemaphore(string key, int consumers, IKeyedSemaphoreOwner owner)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            _consumers = consumers;
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _semaphoreSlim = new SemaphoreSlim(1, 1);
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
        }
        
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

        int IKeyedSemaphore.Consumers => _consumers;

        int IKeyedSemaphore.IncreaseConsumers() => ++_consumers;

        int IKeyedSemaphore.DecreaseConsumers() => --_consumers;

        void IKeyedSemaphore.InternalDispose() => InternalDispose();
        
        public void Dispose() => _owner.Return(this);

        public void InternalDispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _semaphoreSlim.Dispose();
        }
    }
}