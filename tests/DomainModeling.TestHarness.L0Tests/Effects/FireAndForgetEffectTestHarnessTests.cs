using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.TestHarness.Effects;

using Moq;


namespace Mississippi.DomainModeling.TestHarness.L0Tests.Effects;

/// <summary>
///     Verifies worker-effect metadata, dependency construction, and aggregate dispatch capture.
/// </summary>
public sealed class FireAndForgetEffectTestHarnessTests
{
    /// <summary>
    ///     Logger-only effects should receive default metadata, and their failures should reach the caller.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task LoggerOnlyEffectsShouldUseDefaultsAndPropagateFailures()
    {
        Mock<IFireAndForgetEventEffect<string, List<string>>> effect = new();
        InvalidOperationException failure = new("Effect failed.");
        effect.Setup(value => value.HandleAsync(
                "event",
                It.IsAny<List<string>>(),
                "TEST.DOMAIN.ENTITY|test-entity",
                1,
                CancellationToken.None))
            .ThrowsAsync(failure);
        FireAndForgetEffectTestHarness<IFireAndForgetEventEffect<string, List<string>>, string, List<string>> harness =
            FireAndForgetEffectTestHarness<IFireAndForgetEventEffect<string, List<string>>, string, List<string>>
                .Create();
        IFireAndForgetEventEffect<string, List<string>> instance = harness.Build(logger =>
        {
            Assert.Same(harness.LoggerMock.Object, logger);
            return effect.Object;
        });
        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() => harness.InvokeAsync(instance, "event", []));
        Assert.Same(failure, exception);
        Assert.Equal("TEST.DOMAIN.ENTITY|test-entity", harness.BrookKey);
        Assert.Equal(1, harness.EventPosition);
        Assert.Empty(harness.DispatchedCommands);
    }

    /// <summary>
    ///     Worker effects should receive custom metadata and a configured target aggregate response.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task WorkerEffectsShouldCaptureDispatchesWithConfiguredMetadata()
    {
        using CancellationTokenSource cancellation = new();
        List<string> state = ["current"];
        OperationResult response = OperationResult.Fail("denied", "Rejected.");
        FireAndForgetEffectTestHarness<IFireAndForgetEventEffect<string, List<string>>, string, List<string>> harness =
            FireAndForgetEffectTestHarness<IFireAndForgetEventEffect<string, List<string>>, string, List<string>>
                .Create()
                .WithEntityId("source")
                .WithEventPosition(37)
                .WithAggregateGrainResponse<List<string>>("target", response);
        Assert.Equal("TEST.DOMAIN.ENTITY|source", harness.BrookKey);
        harness.WithBrookKey("APP.DOMAIN.ACCOUNT|custom");
        Mock<IFireAndForgetEventEffect<string, List<string>>> effect = new();
        IFireAndForgetEventEffect<string, List<string>> instance = harness.Build((factory, logger) =>
        {
            Assert.Same(harness.LoggerMock.Object, logger);
            effect.Setup(value => value.HandleAsync(
                    "event",
                    state,
                    "APP.DOMAIN.ACCOUNT|custom",
                    37,
                    cancellation.Token))
                .Returns(async () =>
                {
                    OperationResult actual = await factory.GetGenericAggregate<List<string>>("target")
                        .ExecuteAsync("dispatch", cancellation.Token);
                    Assert.Equal(response, actual);
                });
            return effect.Object;
        });
        await harness.InvokeAsync(instance, "event", state, cancellation.Token);
        Assert.Equal(37, harness.EventPosition);
        Assert.Equal((typeof(List<string>), "target", (object)"dispatch"), Assert.Single(harness.DispatchedCommands));
        effect.Verify(
            value => value.HandleAsync("event", state, "APP.DOMAIN.ACCOUNT|custom", 37, cancellation.Token),
            Times.Once);
    }

    /// <summary>
    ///     Missing factories and effects should be rejected.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact]
    public async Task WorkerHarnessShouldRejectNullInputs()
    {
        FireAndForgetEffectTestHarness<IFireAndForgetEventEffect<string, List<string>>, string, List<string>> harness =
            FireAndForgetEffectTestHarness<IFireAndForgetEventEffect<string, List<string>>, string, List<string>>
                .Create();
        Assert.Throws<ArgumentNullException>(() => harness.Build(
            (Func<ILogger<IFireAndForgetEventEffect<string, List<string>>>,
                IFireAndForgetEventEffect<string, List<string>>>)null!));
        Assert.Throws<ArgumentNullException>(() => harness.Build(
            (Func<IAggregateGrainFactory, ILogger<IFireAndForgetEventEffect<string, List<string>>>,
                IFireAndForgetEventEffect<string, List<string>>>)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => harness.InvokeAsync(null!, "event", []));
    }
}