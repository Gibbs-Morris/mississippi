using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.Aqueduct.Abstractions.Messages;

using NSubstitute;
using NSubstitute.Core;

using Orleans.Streams;


namespace Mississippi.Aqueduct.Gateway.L0Tests;

/// <summary>
///     Verifies partial stream setup recovery. Public for xUnit discovery.
/// </summary>
public sealed class StreamSubscriptionRecoveryTests
{
    private static void AssertFailureLogged(
        StreamSubscriptionRecoveryFixture fixture,
        Exception exception,
        int eventId,
        string eventName
    )
    {
        ICall call = Assert.Single(
            fixture.Logger.ReceivedCalls(),
            call => (call.GetMethodInfo().Name == "Log") && ReferenceEquals(call.GetArguments()[3], exception));
        object?[] arguments = call.GetArguments();
        Assert.Equal(LogLevel.Error, arguments[0]);
        EventId actualEvent = Assert.IsType<EventId>(arguments[1]);
        Assert.Equal(eventId, actualEvent.Id);
        Assert.Equal(eventName, actualEvent.Name);
        IReadOnlyList<KeyValuePair<string, object?>> fields =
            Assert.IsType<IReadOnlyList<KeyValuePair<string, object?>>>(arguments[2], false);
        Assert.Contains(fields, field => (field.Key == "HubName") && Equals(field.Value, "RecoveryHub"));
        Assert.Contains(fields, field => (field.Key == "ServerId") && Equals(field.Value, "recovery-server"));
    }

    /// <summary>
    ///     A broadcast setup failure must remove the accepted server subscription.
    /// </summary>
    /// <param name="failLookup">Whether broadcast lookup fails instead of subscription.</param>
    /// <returns>A task representing the test operation.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BroadcastSetupFailureShouldUnsubscribeAcceptedServer(
        bool failLookup
    )
    {
        using StreamSubscriptionRecoveryFixture fixture = new();
        InvalidOperationException failure = new("Broadcast unavailable");
        fixture.AllLookupFailure = failLookup ? failure : null;
        fixture.AllSubscribeFailure = failLookup ? null : failure;
        Exception actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.InitializeAsync(TestContext.Current.CancellationToken));
        Assert.Same(failure, actual);
        Assert.False(fixture.Manager.IsInitialized);
        Assert.Equal(0, fixture.ActiveServerSubscriptions);
        Assert.Equal(1, fixture.UnsubscribeCalls);
        AssertFailureLogged(fixture, failure, 3, "StreamInitializationFailed");
    }

    /// <summary>
    ///     A canceled broadcast subscription must still compensate the accepted server subscription.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task CanceledBroadcastSubscriptionShouldCompensateServer()
    {
        using StreamSubscriptionRecoveryFixture fixture = new();
        fixture.AllSubscribeFailure = new OperationCanceledException("Broadcast subscription canceled");
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.InitializeAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, fixture.ActiveServerSubscriptions);
        Assert.Equal(1, fixture.UnsubscribeCalls);
        Assert.False(fixture.Manager.IsInitialized);
    }

    /// <summary>
    ///     Cancellation before the initialization lock is acquired must not create a subscription.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task CanceledInitializationShouldNotSubscribe()
    {
        using StreamSubscriptionRecoveryFixture fixture = new();
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.InitializeAsync(cancellation.Token));
        Assert.Equal(0, fixture.ServerSubscribeCalls);
        Assert.Equal(0, fixture.UnsubscribeCalls);
        Assert.False(fixture.Manager.IsInitialized);
    }

    /// <summary>
    ///     Cleanup failure retains ownership and blocks fresh subscriptions until cleanup succeeds.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task CleanupFailureShouldRetainHandleUntilRetryCanUnsubscribe()
    {
        using StreamSubscriptionRecoveryFixture fixture = new();
        InvalidOperationException setupFailure = new("Broadcast unavailable");
        InvalidOperationException cleanupFailure = new("Cleanup unavailable");
        fixture.AllSubscribeFailure = setupFailure;
        fixture.UnsubscribeFailure = cleanupFailure;
        AggregateException failure = await Assert.ThrowsAsync<AggregateException>(() =>
            fixture.InitializeAsync(TestContext.Current.CancellationToken));
        Assert.Collection(
            failure.InnerExceptions,
            exception => Assert.Same(setupFailure, exception),
            exception => Assert.Same(cleanupFailure, exception));
        AssertFailureLogged(fixture, setupFailure, 3, "StreamInitializationFailed");
        AssertFailureLogged(fixture, cleanupFailure, 4, "StreamSubscriptionCleanupFailed");
        Assert.Equal(1, fixture.ActiveServerSubscriptions);
        Assert.False(fixture.Manager.IsInitialized);
        Exception retryFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.InitializeAsync(TestContext.Current.CancellationToken));
        Assert.Same(cleanupFailure, retryFailure);
        Assert.Equal(1, fixture.ServerSubscribeCalls);
        Assert.Equal(2, fixture.UnsubscribeCalls);
        fixture.UnsubscribeFailure = null;
        fixture.AllSubscribeFailure = null;
        await fixture.InitializeAsync(TestContext.Current.CancellationToken);
        Assert.True(fixture.Manager.IsInitialized);
        Assert.Equal(1, fixture.ActiveServerSubscriptions);
        Assert.Equal(2, fixture.ServerSubscribeCalls);
        Assert.Equal(3, fixture.UnsubscribeCalls);
    }

    /// <summary>
    ///     A concurrent retry must wait for the previous attempt's cleanup.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task ConcurrentRetryShouldWaitForCompensation()
    {
        using StreamSubscriptionRecoveryFixture fixture = new();
        TaskCompletionSource cleanupEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseCleanup = new(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.AllSubscribeFailure = new InvalidOperationException("Broadcast unavailable");
        fixture.BeforeUnsubscribe = async () =>
        {
            cleanupEntered.SetResult();
            await releaseCleanup.Task.WaitAsync(TestContext.Current.CancellationToken);
        };
        async Task InitializeAsync() => await fixture.InitializeAsync(TestContext.Current.CancellationToken);
        Task initialization = Assert.ThrowsAsync<InvalidOperationException>(InitializeAsync);
        Task retry = Task.CompletedTask;
        try
        {
            Task firstCompleted = await Task.WhenAny(cleanupEntered.Task, initialization);
            Assert.Same(cleanupEntered.Task, firstCompleted);
            fixture.AllSubscribeFailure = null;
            retry = InitializeAsync();
            Assert.False(retry.IsCompleted);
            Assert.Equal(1, fixture.ServerSubscribeCalls);
            releaseCleanup.SetResult();
            await initialization;
            await retry;
            Assert.True(fixture.Manager.IsInitialized);
            Assert.Equal(1, fixture.ActiveServerSubscriptions);
            Assert.Equal(2, fixture.ServerSubscribeCalls);
        }
        finally
        {
            releaseCleanup.TrySetResult();
            await initialization;
            await retry;
        }
    }

    /// <summary>
    ///     Disabled failure logging must preserve compensation and the original exception.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task DisabledFailureLoggingShouldPreserveCompensation()
    {
        using StreamSubscriptionRecoveryFixture fixture = new();
        fixture.Logger.IsEnabled(LogLevel.Error).Returns(false);
        InvalidOperationException failure = new("Broadcast unavailable");
        fixture.AllSubscribeFailure = failure;
        Exception actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.InitializeAsync(TestContext.Current.CancellationToken));
        Assert.Same(failure, actual);
        Assert.Equal(0, fixture.ActiveServerSubscriptions);
        Assert.Equal(1, fixture.UnsubscribeCalls);
        Assert.DoesNotContain(fixture.Logger.ReceivedCalls(), call => call.GetMethodInfo().Name == "Log");
    }

    /// <summary>
    ///     Repeated broadcast failures must not accumulate accepted server subscriptions.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task RepeatedFailuresShouldNotAccumulateSubscriptions()
    {
        using StreamSubscriptionRecoveryFixture fixture = new();
        fixture.AllSubscribeFailure = new InvalidOperationException("Broadcast unavailable");
        for (int attempt = 0; attempt < 3; attempt++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                fixture.InitializeAsync(TestContext.Current.CancellationToken));
        }

        Assert.Equal(0, fixture.ActiveServerSubscriptions);
        Assert.Equal(3, fixture.UnsubscribeCalls);
        Assert.False(fixture.Manager.IsInitialized);
    }

    /// <summary>
    ///     Retrying partial setup must deliver a server message through only one callback.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task RetryShouldDeliverServerMessageOnce()
    {
        using StreamSubscriptionRecoveryFixture fixture = new();
        fixture.AllSubscribeFailure = new InvalidOperationException("Broadcast unavailable");
        int deliveries = 0;

        Task OnServerMessageAsync(
            ServerMessage message
        )
        {
            deliveries++;
            return Task.CompletedTask;
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.InitializeAsync(
            TestContext.Current.CancellationToken,
            OnServerMessageAsync));
        fixture.AllSubscribeFailure = null;
        await fixture.InitializeAsync(TestContext.Current.CancellationToken, OnServerMessageAsync);
        await fixture.DeliverServerAsync(
            new()
            {
                ConnectionId = "connection",
                MethodName = "Notify",
            });
        Assert.True(fixture.Manager.IsInitialized);
        Assert.Equal(1, deliveries);
        Assert.Equal(1, fixture.ActiveServerSubscriptions);
    }

    /// <summary>
    ///     Failure before any subscription is accepted requires no compensation.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task ServerSubscribeFailureShouldAllowFreshRetry()
    {
        using StreamSubscriptionRecoveryFixture fixture = new();
        fixture.ServerSubscribeFailure = new InvalidOperationException("Server unavailable");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.InitializeAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, fixture.ActiveServerSubscriptions);
        Assert.Equal(0, fixture.UnsubscribeCalls);
        Assert.Equal(0, fixture.AllSubscribeCalls);
        fixture.ServerSubscribeFailure = null;
        await fixture.InitializeAsync(TestContext.Current.CancellationToken);
        Assert.True(fixture.Manager.IsInitialized);
        Assert.Equal(1, fixture.ActiveServerSubscriptions);
    }

    /// <summary>
    ///     Completed initialization remains idempotent and preserves broadcast callbacks and publication.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task SuccessfulInitializationShouldRemainIdempotent()
    {
        using StreamSubscriptionRecoveryFixture fixture = new();
        AllMessage message = new()
        {
            MethodName = "Notify",
        };
        int deliveries = 0;
        await fixture.InitializeAsync(
            TestContext.Current.CancellationToken,
            onAllMessage: _ =>
            {
                deliveries++;
                return Task.CompletedTask;
            });
        await fixture.InitializeAsync(TestContext.Current.CancellationToken);
        await Assert.IsType<IAsyncObserver<AllMessage>>(fixture.AllObserver, false).OnNextAsync(message);
        await fixture.Manager.PublishToAllAsync(message);
        Assert.Equal(1, deliveries);
        Assert.Equal(1, fixture.ServerSubscribeCalls);
        Assert.Equal(1, fixture.AllSubscribeCalls);
        Assert.Equal(0, fixture.UnsubscribeCalls);
        await fixture.AllStream.Received(1).OnNextAsync(message);
    }
}