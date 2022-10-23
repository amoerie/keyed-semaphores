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
}