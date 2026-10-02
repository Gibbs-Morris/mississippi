using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.Runtime.Grains;
using Mississippi.Testing.Utilities.Mocks;

using NSubstitute;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.Aqueduct.Runtime.L0Tests;

/// <summary>
///     Verifies bounded orphan sweeps, membership ownership and retry after a failed lookup.
/// </summary>
public sealed class SignalRGroupLivenessTests
{
    /// <summary>
    ///     Creates a group whose owned timer can be executed without a scheduler or wall-clock delay.
    /// </summary>
    /// <param name="connectionIds">The initial members.</param>
    /// <param name="logger">An optional logger used to inspect cleanup failures.</param>
    /// <returns>The group, substituted clients, timer callback and owned registration.</returns>
    private static async Task<(SignalRGroupGrain Group, Dictionary<string, ISignalRClientGrain> Clients,
        Func<Task> Cleanup, IGrainTimer Timer)> CreateGroupAsync(
        string[] connectionIds,
        ILogger<SignalRGroupGrain>? logger = null
    )
    {
        IGrainContext context = GrainContextMockBuilder.Create().WithGrainKey("hub:group").BuildObject();
        IGrainRuntime runtime = Substitute.For<IGrainRuntime>();
        IGrainFactory factory = Substitute.For<IGrainFactory>();
        Dictionary<string, ISignalRClientGrain> clients = [];
        foreach (string connectionId in connectionIds)
        {
            ISignalRClientGrain client = Substitute.For<ISignalRClientGrain>();
            client.GetServerIdAsync().Returns(Task.FromResult<string?>("server"));
            factory.GetGrain<ISignalRClientGrain>($"hub:{connectionId}").Returns(client);
            clients.Add(connectionId, client);
        }

        IGrainTimer timer = Substitute.For<IGrainTimer>();
        Func<SignalRGroupGrain, CancellationToken, Task>? callback = null;
        using IGrainTimer registration = runtime.TimerRegistry.RegisterGrainTimer(
            context,
            Arg.Any<Func<SignalRGroupGrain, CancellationToken, Task>>(),
            Arg.Any<SignalRGroupGrain>(),
            Arg.Any<GrainTimerCreationOptions>());
        registration.Returns(call =>
        {
            callback = call.Arg<Func<SignalRGroupGrain, CancellationToken, Task>>();
            return timer;
        });
        SignalRGroupGrain group = new(
            context,
            runtime,
            factory,
            Options.Create(new AqueductOptions()),
            logger ?? NullLogger<SignalRGroupGrain>.Instance);
        foreach (string connectionId in connectionIds)
        {
            await group.AddConnectionAsync(connectionId);
        }

        Assert.NotNull(callback);
        return (group, clients, () => callback(group, CancellationToken.None), timer);
    }

    /// <summary>
    ///     A slow member cannot cause the sweep to launch the entire membership at once.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task CleanupShouldBoundOutstandingLookups()
    {
        (SignalRGroupGrain group, Dictionary<string, ISignalRClientGrain> clients, Func<Task> cleanup, IGrainTimer _) =
            await CreateGroupAsync(["one", "two", "three", "four", "five"]);
        TaskCompletionSource<string?> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int started = 0;
        foreach (ISignalRClientGrain client in clients.Values)
        {
            client.GetServerIdAsync().Returns(pending.Task).AndDoes(_ => started++);
        }

        using IDisposable groupLifetime = group;
        Task sweep = cleanup();
        try
        {
            Assert.Equal(1, started);
        }
        finally
        {
            pending.TrySetResult("server");
            await sweep;
        }

        Assert.Equal(5, started);
        Assert.Equal(5, (await group.GetConnectionsAsync()).Count);
    }

    /// <summary>
    ///     The generated warning respects the logger's disabled level.
    /// </summary>
    [Fact]
    public void DisabledWarningShouldNotEmitCleanupLog()
    {
        ILogger<SignalRGroupGrain> logger = Substitute.For<ILogger<SignalRGroupGrain>>();
        logger.IsEnabled(LogLevel.Warning).Returns(false);
        logger.ConnectionLivenessCheckFailed("hub:group", "failed", new OrleansException("client unavailable"));
        Assert.DoesNotContain(logger.ReceivedCalls(), call => call.GetMethodInfo().Name == "Log");
    }

    /// <summary>
    ///     A retained member's remote failure emits the structured warning and original exception.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task FailedLookupShouldLogRetainedMembership()
    {
        ILogger<SignalRGroupGrain> logger = Substitute.For<ILogger<SignalRGroupGrain>>();
        logger.IsEnabled(LogLevel.Warning).Returns(true);
        (SignalRGroupGrain group, Dictionary<string, ISignalRClientGrain> clients, Func<Task> cleanup, IGrainTimer _) =
            await CreateGroupAsync(["failed"], logger);
        using IDisposable groupLifetime = group;
        OrleansException failure = new("client unavailable");
        clients["failed"].GetServerIdAsync().Returns(Task.FromException<string?>(failure));
        await cleanup();
        object?[] arguments = Assert.Single(
                logger.ReceivedCalls(),
                call => (call.GetMethodInfo().Name == "Log") &&
                        (Assert.IsType<EventId>(call.GetArguments()[1]).Id == 11))
            .GetArguments();
        Assert.Equal(LogLevel.Warning, Assert.IsType<LogLevel>(arguments[0]));
        Assert.Equal("ConnectionLivenessCheckFailed", Assert.IsType<EventId>(arguments[1]).Name);
        Assert.Same(failure, arguments[3]);
        IEnumerable<KeyValuePair<string, object?>> fields =
            Assert.IsType<IEnumerable<KeyValuePair<string, object?>>>(arguments[2], false);
        Dictionary<string, object?> values = fields.ToDictionary(field => field.Key, field => field.Value);
        Assert.Equal("hub:group", values["GroupKey"]);
        Assert.Equal("failed", values["ConnectionId"]);
        Assert.Equal(
            "Liveness check failed for connection '{ConnectionId}' in group '{GroupKey}'; retaining membership for retry",
            values["{OriginalFormat}"]);
        Assert.Equal("failed", Assert.Single(await group.GetConnectionsAsync()));
    }

    /// <summary>
    ///     A failed lookup preserves that member without failing cleanup or suppressing a later retry.
    /// </summary>
    /// <param name="timedOut">Whether the remote request timed out rather than reporting an Orleans failure.</param>
    /// <returns>The test operation.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedMemberShouldPreserveOwnershipAndAllowRetry(
        bool timedOut
    )
    {
        (SignalRGroupGrain group, Dictionary<string, ISignalRClientGrain> clients, Func<Task> cleanup,
            IGrainTimer timer) = await CreateGroupAsync(["healthy", "missing", "failed"]);
        using IDisposable groupLifetime = group;
        clients["missing"].GetServerIdAsync().Returns(Task.FromResult<string?>(null));
        clients["failed"]
            .GetServerIdAsync()
            .Returns(
                Task.FromException<string?>(
                    timedOut
                        ? new TimeoutException("client unavailable")
                        : new OrleansException("client unavailable")));
        await cleanup();
        ImmutableHashSet<string> retained = await group.GetConnectionsAsync();
        Assert.Contains("healthy", retained);
        Assert.Contains("failed", retained);
        Assert.DoesNotContain("missing", retained);
        timer.DidNotReceive().Dispose();
        clients["failed"].GetServerIdAsync().Returns(Task.FromResult<string?>(null));
        await cleanup();
        Assert.Equal("healthy", Assert.Single(await group.GetConnectionsAsync()));
        await clients["failed"].Received(2).GetServerIdAsync();
    }

    /// <summary>
    ///     Live clients retain membership and the timer remains owned by the nonempty group.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task HealthyMembersShouldRemainAfterCleanup()
    {
        (SignalRGroupGrain group, Dictionary<string, ISignalRClientGrain> clients, Func<Task> cleanup,
            IGrainTimer timer) = await CreateGroupAsync(["first", "second"]);
        using IDisposable groupLifetime = group;
        await cleanup();
        Assert.Equal(2, (await group.GetConnectionsAsync()).Count);
        await clients["first"].Received(1).GetServerIdAsync();
        await clients["second"].Received(1).GetServerIdAsync();
        timer.DidNotReceive().Dispose();
    }

    /// <summary>
    ///     Membership added during a suspended sweep remains outside its immutable snapshot.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task JoiningDuringCleanupShouldPreserveNewMembership()
    {
        (SignalRGroupGrain group, Dictionary<string, ISignalRClientGrain> clients, Func<Task> cleanup, IGrainTimer _) =
            await CreateGroupAsync(["old"]);
        using IDisposable groupLifetime = group;
        TaskCompletionSource<string?> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        clients["old"].GetServerIdAsync().Returns(pending.Task);
        Task sweep = cleanup();
        try
        {
            await group.AddConnectionAsync("new");
        }
        finally
        {
            pending.TrySetResult(null);
            await sweep;
        }

        Assert.Equal("new", Assert.Single(await group.GetConnectionsAsync()));
    }

    /// <summary>
    ///     Removing every disconnected member releases the timer and empty activation.
    /// </summary>
    /// <returns>The test operation.</returns>
    [Fact]
    public async Task MissingMembersShouldReleaseTimer()
    {
        (SignalRGroupGrain group, Dictionary<string, ISignalRClientGrain> clients, Func<Task> cleanup,
            IGrainTimer timer) = await CreateGroupAsync(["missing"]);
        using IDisposable groupLifetime = group;
        clients["missing"].GetServerIdAsync().Returns(Task.FromResult<string?>(null));
        await cleanup();
        Assert.Empty(await group.GetConnectionsAsync());
        timer.Received(1).Dispose();
    }
}