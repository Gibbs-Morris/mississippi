using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.TestHarness.Effects;

using Moq;
using Moq.Protected;

using Orleans.Runtime;


namespace Mississippi.DomainModeling.TestHarness.L0Tests.Effects;

/// <summary>
///     Verifies effect construction, invocation metadata, and captured aggregate dispatches.
/// </summary>
public sealed class EffectTestHarnessTests
{
    /// <summary>
    ///     Cancelled effects should propagate cancellation to the harness caller.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task EffectInvocationShouldPropagateCancellation()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        Mock<SimpleEventEffectBase<string, List<string>>> effect = new();
        effect.Protected()
            .Setup<Task>(
                "HandleSimpleAsync",
                "event",
                ItExpr.IsAny<List<string>>(),
                ItExpr.IsAny<string>(),
                ItExpr.IsAny<long>(),
                cancellation.Token)
            .Returns(Task.FromCanceled(cancellation.Token));
        EffectTestHarness<SimpleEventEffectBase<string, List<string>>, string, List<string>> harness =
            EffectTestHarness<SimpleEventEffectBase<string, List<string>>, string, List<string>>.Create();
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            harness.InvokeAsync(effect.Object, "event", [], cancellation.Token));
        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }

    /// <summary>
    ///     Missing factories, effects, and supported entry points should fail with useful diagnostics.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task HarnessShouldRejectInvalidEffectInputs()
    {
        EffectTestHarness<object, string, List<string>> harness =
            EffectTestHarness<object, string, List<string>>.Create();
        Assert.Throws<ArgumentNullException>(() => harness.Build(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => harness.InvokeAsync(null!, "event", []));
        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() => harness.InvokeAsync(new(), "event", []));
        Assert.Contains(
            "Could not find HandleSimpleAsync or HandleAsync method on Object",
            exception.Message,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Invocation should wait for an asynchronous simple effect to finish.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task SimpleEffectInvocationShouldAwaitCompletion()
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<SimpleEventEffectBase<string, List<string>>> effect = new();
        effect.Protected()
            .Setup<Task>(
                "HandleSimpleAsync",
                "event",
                ItExpr.IsAny<List<string>>(),
                "TEST.DOMAIN.AGGREGATE|test-entity",
                1L,
                CancellationToken.None)
            .Returns(completion.Task);
        EffectTestHarness<SimpleEventEffectBase<string, List<string>>, string, List<string>> harness =
            EffectTestHarness<SimpleEventEffectBase<string, List<string>>, string, List<string>>.Create();
        Task<IReadOnlyList<object>> invocation = harness.InvokeAsync(effect.Object, "event", []);
        Assert.False(invocation.IsCompleted);
        completion.SetResult();
        Assert.Empty(await invocation);
        Assert.Same(harness.ToResult().DispatchedCommands, harness.DispatchedCommands);
    }

    /// <summary>
    ///     Simple effects should receive configured metadata and the configured aggregate response.
    /// </summary>
    /// <param name="succeeds">Whether the target aggregate accepts the dispatched command.</param>
    /// <returns>A task representing the test operation.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SimpleEffectsShouldReceiveDependenciesAndCaptureDispatches(
        bool succeeds
    )
    {
        using CancellationTokenSource cancellation = new();
        OperationResult response = succeeds ? OperationResult.Ok() : OperationResult.Fail("denied", "Rejected.");
        List<string> state = ["current"];
        EffectTestHarness<SimpleEventEffectBase<string, List<string>>, string, List<string>> harness =
            EffectTestHarness<SimpleEventEffectBase<string, List<string>>, string, List<string>>.Create()
                .WithBrookName("APP.DOMAIN.ACCOUNT")
                .WithGrainKey("source")
                .WithEventPosition(42)
                .WithAggregateGrainResponse<List<string>>("target", response);
        Mock<SimpleEventEffectBase<string, List<string>>> effect = new();
        SimpleEventEffectBase<string, List<string>> instance = harness.Build((factory, context, logger) =>
        {
            Assert.Equal(GrainId.Create("test", "source"), context.GrainId);
            Assert.Same(harness.LoggerMock.Object, logger);
            effect.Protected()
                .Setup<Task>("HandleSimpleAsync", "event", state, "APP.DOMAIN.ACCOUNT|source", 42L, cancellation.Token)
                .Returns(async () =>
                {
                    OperationResult actual = await factory.GetGenericAggregate<List<string>>("target")
                        .ExecuteAsync("dispatch", cancellation.Token);
                    Assert.Equal(response, actual);
                });
            return effect.Object;
        });
        IReadOnlyList<object> events = await harness.InvokeAsync(instance, "event", state, cancellation.Token);
        Assert.Empty(events);
        Assert.Equal((typeof(List<string>), "target", (object)"dispatch"), Assert.Single(harness.DispatchedCommands));
        EffectTestResult result = harness.ToResult();
        Assert.True(result.HasDispatches);
        Assert.Equal(1, result.DispatchCount);
        Assert.Equal("dispatch", result.ShouldHaveDispatched<string>());
    }

    /// <summary>
    ///     Yielding effects should preserve output order and receive default metadata and the caller token.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task YieldingEffectsShouldPreserveOutputsAndInvocationArguments()
    {
        using CancellationTokenSource cancellation = new();
        List<string> state = ["state"];
        Mock<EventEffectBase<string, List<string>>> effect = new();
        effect.Setup(value => value.HandleAsync(
                "event",
                state,
                "TEST.DOMAIN.AGGREGATE|test-entity",
                1,
                cancellation.Token))
            .Returns(new object[] { "first", "second" }.ToAsyncEnumerable());
        EffectTestHarness<EventEffectBase<string, List<string>>, string, List<string>> harness =
            EffectTestHarness<EventEffectBase<string, List<string>>, string, List<string>>.Create();
        IReadOnlyList<object> events = await harness.InvokeAsync(effect.Object, "event", state, cancellation.Token);
        Assert.Equal(["first", "second"], events);
        effect.Verify(
            value => value.HandleAsync("event", state, "TEST.DOMAIN.AGGREGATE|test-entity", 1, cancellation.Token),
            Times.Once);
    }
}