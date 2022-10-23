using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace KeyedSemaphores.Tests;

public class TestForCancellationTokenAndTimeout
{
    [Fact]
    public async Task TestLockReleasedOnCancellation()
    {
        var collection = new KeyedSemaphoresCollection<string>();

        using var cts = new CancellationTokenSource(0);

        var action = async () =>
        {
            using var locking = await collection.LockAsync("test", default, cts.Token);
        };

        await action.Should().ThrowAsync<OperationCanceledException>();

        collection.Index.Should().NotContainKey("test");
    }

    [Fact]
    public async Task TestTimeoutException()
    {
        var collection = new KeyedSemaphoresCollection<string>();

        using var cts = new CancellationTokenSource();

        var task = Task.Run(async () =>
        {
            using var _ = await collection.LockAsync("test", default, cts.Token);
            while (true)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10), cts.Token);
            }
        }, cts.Token);
        var action = async () => { await collection.LockAsync("test", TimeSpan.FromMilliseconds(10), cts.Token); };

        await action.Should().ThrowAsync<TimeoutException>();

        cts.Cancel();

        await Task.Delay(TimeSpan.FromMilliseconds(50));

        collection.Index.Should().NotContainKey("test");
    }

    [Fact]
    public async Task TestNoTimeoutException()
    {
        var collection = new KeyedSemaphoresCollection<string>();

        using var cts = new CancellationTokenSource();

        var action = async () =>
        {
            using var _ = await collection.LockAsync("test", TimeSpan.FromMilliseconds(10), cts.Token);
        };

        await action.Should().NotThrowAsync<TimeoutException>();

        cts.Cancel();

        collection.Index.Should().NotContainKey("test");
    }
}