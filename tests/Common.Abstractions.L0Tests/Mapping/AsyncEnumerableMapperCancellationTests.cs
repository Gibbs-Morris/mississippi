using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Common.Abstractions.Mapping;

using Moq;


namespace MississippiTests.Common.Abstractions.L0Tests.Mapping;

/// <summary>
///     Provides public xUnit tests for mapped-stream enumeration cancellation and unchanged lazy behavior.
/// </summary>
public sealed class AsyncEnumerableMapperCancellationTests
{
    private static async Task ConsumeAsync<T>(
        IAsyncEnumerable<T> source,
        CancellationToken cancellationToken
    )
    {
        await foreach (T item in source.WithCancellation(cancellationToken))
        {
            Assert.Fail($"The pending test source should not produce an item: {item}.");
        }
    }

    /// <summary>
    ///     Establishes that the test source cancels a pending move when directly enumerated.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task DirectSourceShouldCancelPendingMoveAndDisposeAsync()
    {
        using CancellationTokenSource cancellation = new();
        await using CancellationAwareAsyncEnumerable source = new();
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            Task pendingMove = ConsumeAsync(source, cancellation.Token);
            try
            {
                await source.Started.WaitAsync(TestContext.Current.CancellationToken);
                await cancellation.CancelAsync();
                Assert.Equal(cancellation.Token, source.EnumerationToken);
                await pendingMove;
            }
            finally
            {
                source.Release();
                await Task.WhenAny(pendingMove);
            }
        });
        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.True(source.IsDisposed);
    }

    /// <summary>
    ///     Keeps a null input failure deferred until enumeration begins.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task MapShouldKeepNullValidationDeferredAsync()
    {
        Mock<IMapper<int, string>> itemMapper = new();
        AsyncEnumerableMapper<int, string> mapper = new(itemMapper.Object);
        IAsyncEnumerable<string> output = mapper.Map(null!);
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await output.ToListAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>
    ///     Preserves lazy source enumeration before the first mapped move.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task MapShouldRemainLazyUntilFirstMoveAsync()
    {
        await using CancellationAwareAsyncEnumerable source = new();
        Mock<IMapper<int, string>> itemMapper = new();
        AsyncEnumerableMapper<int, string> mapper = new(itemMapper.Object);
        IAsyncEnumerable<string> output = mapper.Map(source);
        Assert.False(source.IsEnumeratorCreated);
        IAsyncEnumerator<string> enumerator = output.GetAsyncEnumerator(TestContext.Current.CancellationToken);
        Assert.False(source.IsEnumeratorCreated);
        Task<bool> pendingMove = enumerator.MoveNextAsync().AsTask();
        try
        {
            await source.Started.WaitAsync(TestContext.Current.CancellationToken);
            Assert.True(source.IsEnumeratorCreated);
        }
        finally
        {
            source.Release();
            await Task.WhenAny(pendingMove);
            await enumerator.DisposeAsync();
        }

        Assert.True(source.IsDisposed);
        itemMapper.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Forwards WithCancellation's token to pending source work and releases its enumerator after cancellation.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task MappedSourceShouldCancelPendingMoveAndDisposeAsync()
    {
        using CancellationTokenSource cancellation = new();
        await using CancellationAwareAsyncEnumerable source = new();
        Mock<IMapper<int, string>> itemMapper = new();
        AsyncEnumerableMapper<int, string> mapper = new(itemMapper.Object);
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            Task pendingMove = ConsumeAsync(mapper.Map(source), cancellation.Token);
            try
            {
                await source.Started.WaitAsync(TestContext.Current.CancellationToken);
                await cancellation.CancelAsync();
                Assert.Equal(cancellation.Token, source.EnumerationToken);
                await pendingMove;
            }
            finally
            {
                source.Release();
                await Task.WhenAny(pendingMove);
            }
        });
        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.True(source.IsDisposed);
        itemMapper.VerifyNoOtherCalls();
    }
}