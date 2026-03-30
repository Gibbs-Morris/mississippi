using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.DomainModeling.ReplicaSinks.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Bootstrap;


namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.Storage.Bootstrap.L0Tests;

/// <summary>
///     Tests the bootstrap replica sink provider and its registration extensions.
/// </summary>
public sealed class BootstrapReplicaSinkProviderTests
{
    /// <summary>
    ///     Ensures the standalone bootstrap delivery-state store registration resolves the proof store.
    /// </summary>
    [Fact]
    public void AddBootstrapReplicaSinkDeliveryStateStoreShouldRegisterProofStore()
    {
        ServiceCollection services = [];
        services.AddBootstrapReplicaSinkDeliveryStateStore();
        using ServiceProvider provider = services.BuildServiceProvider();
        IReplicaSinkDeliveryStateStore stateStore = provider.GetRequiredService<IReplicaSinkDeliveryStateStore>();
        Assert.IsType<BootstrapReplicaSinkDeliveryStateStore>(stateStore);
    }

    /// <summary>
    ///     Ensures provider registration contributes a keyed provider and a sink descriptor.
    /// </summary>
    [Fact]
    public void AddBootstrapReplicaSinkShouldRegisterProviderAndDescriptor()
    {
        ServiceCollection services = [];
        services.AddBootstrapReplicaSink(
            "bootstrap",
            "bootstrap-client",
            options => options.ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing);
        using ServiceProvider provider = services.BuildServiceProvider();
        IReplicaSinkProvider sinkProvider = provider.GetRequiredKeyedService<IReplicaSinkProvider>("bootstrap");
        ReplicaSinkRegistrationDescriptor descriptor =
            provider.GetServices<ReplicaSinkRegistrationDescriptor>().Single();
        IReplicaSinkDeliveryStateStore stateStore = provider.GetRequiredService<IReplicaSinkDeliveryStateStore>();
        Assert.IsType<BootstrapReplicaSinkProvider>(sinkProvider);
        Assert.IsType<BootstrapReplicaSinkDeliveryStateStore>(stateStore);
        Assert.Equal("bootstrap", descriptor.SinkKey);
        Assert.Equal("bootstrap-client", descriptor.ClientKey);
        Assert.Equal(BootstrapReplicaSinkProvider.FormatName, descriptor.Format);
        Assert.Equal(ReplicaProvisioningMode.CreateIfMissing, descriptor.ProvisioningMode);
    }

    /// <summary>
    ///     Ensures configuration binding uses named options and preserves the explicit client key parameter.
    /// </summary>
    [Fact]
    public void AddBootstrapReplicaSinkWithConfigurationShouldBindNamedOptions()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ProvisioningMode"] = nameof(ReplicaProvisioningMode.CreateIfMissing),
                    ["ClientKey"] = "ignored-config-client",
                })
            .Build();
        ServiceCollection services = [];
        services.AddBootstrapReplicaSink("bootstrap", "bootstrap-client", configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        IOptionsMonitor<BootstrapReplicaSinkOptions> optionsMonitor =
            provider.GetRequiredService<IOptionsMonitor<BootstrapReplicaSinkOptions>>();
        BootstrapReplicaSinkOptions options = optionsMonitor.Get("bootstrap");
        Assert.Equal("bootstrap-client", options.ClientKey);
        Assert.Equal(ReplicaProvisioningMode.CreateIfMissing, options.ProvisioningMode);
    }

    /// <summary>
    ///     Ensures the bootstrap delivery-state store round-trips durable latest-state snapshots.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task BootstrapReplicaSinkDeliveryStateStoreShouldRoundTripPersistedState()
    {
        ServiceCollection services = [];
        services.AddBootstrapReplicaSinkDeliveryStateStore();
        await using ServiceProvider provider = services.BuildServiceProvider();
        IReplicaSinkDeliveryStateStore stateStore = provider.GetRequiredService<IReplicaSinkDeliveryStateStore>();
        ReplicaSinkDeliveryState state = new(
            "delivery-key",
            42,
            42,
            41,
            new(
                42,
                2,
                "transient_failure",
                "Transient failure.",
                new(2026, 3, 29, 12, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 3, 29, 12, 1, 0, TimeSpan.Zero)));
        await stateStore.WriteAsync(state, CancellationToken.None);
        ReplicaSinkDeliveryState? roundTripped = await stateStore.ReadAsync("delivery-key", CancellationToken.None);
        Assert.NotNull(roundTripped);
        Assert.Equal(42, roundTripped.DesiredSourcePosition);
        Assert.Equal(42, roundTripped.BootstrapUpperBoundSourcePosition);
        Assert.Equal(41, roundTripped.CommittedSourcePosition);
        Assert.NotNull(roundTripped.Retry);
        Assert.Equal("transient_failure", roundTripped.Retry.FailureCode);
    }

    /// <summary>
    ///     Ensures the bootstrap delivery-state store pages persisted dead-letter snapshots in newest-first order.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task BootstrapReplicaSinkDeliveryStateStoreShouldPageDeadLetterSnapshots()
    {
        ServiceCollection services = [];
        services.AddBootstrapReplicaSinkDeliveryStateStore();
        await using ServiceProvider provider = services.BuildServiceProvider();
        IReplicaSinkDeliveryStateStore stateStore = provider.GetRequiredService<IReplicaSinkDeliveryStateStore>();
        await stateStore.WriteAsync(
            new(
                "delivery-a",
                desiredSourcePosition: 10,
                deadLetter: new(
                    10,
                    1,
                    "dead_letter_a",
                    "Dead letter A.",
                    new(2026, 3, 30, 12, 0, 0, TimeSpan.Zero))),
            CancellationToken.None);
        await stateStore.WriteAsync(
            new(
                "delivery-b",
                desiredSourcePosition: 11,
                deadLetter: new(
                    11,
                    1,
                    "dead_letter_b",
                    "Dead letter B.",
                    new(2026, 3, 30, 12, 1, 0, TimeSpan.Zero))),
            CancellationToken.None);
        ReplicaSinkDeliveryStatePage firstPage = await stateStore.ReadDeadLetterPageAsync(1, null, CancellationToken.None);
        ReplicaSinkDeliveryStatePage secondPage = await stateStore.ReadDeadLetterPageAsync(
            1,
            firstPage.ContinuationToken,
            CancellationToken.None);
        Assert.Single(firstPage.Items);
        Assert.Equal("delivery-b", firstPage.Items.Single().DeliveryKey);
        Assert.NotNull(firstPage.ContinuationToken);
        Assert.Single(secondPage.Items);
        Assert.Equal("delivery-a", secondPage.Items.Single().DeliveryKey);
        Assert.Null(secondPage.ContinuationToken);
    }

    /// <summary>
    ///     Ensures the bootstrap delivery-state store returns bounded due retries in due-time order.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task BootstrapReplicaSinkDeliveryStateStoreShouldReadDueRetriesInDueOrder()
    {
        ServiceCollection services = [];
        services.AddBootstrapReplicaSinkDeliveryStateStore();
        await using ServiceProvider provider = services.BuildServiceProvider();
        IReplicaSinkDeliveryStateStore stateStore = provider.GetRequiredService<IReplicaSinkDeliveryStateStore>();
        await stateStore.WriteAsync(
            new(
                "delivery-a",
                10,
                null,
                null,
                new(
                    10,
                    1,
                    "retry_a",
                    "Retry A.",
                    new(2026, 3, 30, 12, 0, 0, TimeSpan.Zero),
                    new(2026, 3, 30, 12, 5, 0, TimeSpan.Zero))),
            CancellationToken.None);
        await stateStore.WriteAsync(
            new(
                "delivery-b",
                11,
                null,
                null,
                new(
                    11,
                    1,
                    "retry_b",
                    "Retry B.",
                    new(2026, 3, 30, 12, 0, 0, TimeSpan.Zero),
                    new(2026, 3, 30, 12, 4, 0, TimeSpan.Zero))),
            CancellationToken.None);
        await stateStore.WriteAsync(
            new(
                "delivery-c",
                12,
                null,
                null,
                new(
                    12,
                    1,
                    "retry_c",
                    "Retry C.",
                    new(2026, 3, 30, 12, 0, 0, TimeSpan.Zero),
                    new(2026, 3, 30, 12, 7, 0, TimeSpan.Zero))),
            CancellationToken.None);
        IReadOnlyList<ReplicaSinkDeliveryState> dueStates = await stateStore.ReadDueRetriesAsync(
            new(2026, 3, 30, 12, 5, 0, TimeSpan.Zero),
            2,
            CancellationToken.None);
        Assert.Equal(["delivery-b", "delivery-a"], dueStates.Select(static state => state.DeliveryKey).ToArray());
    }

    /// <summary>
    ///     Ensures delete-style writes clear the latest payload while preserving monotonic source-position fencing.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task BootstrapReplicaSinkProviderShouldApplyDeleteWritesAsLatestState()
    {
        ServiceCollection services = [];
        services.AddBootstrapReplicaSink(
            "bootstrap",
            "bootstrap-client",
            options => options.ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing);
        await using ServiceProvider provider = services.BuildServiceProvider();
        IReplicaSinkProvider sinkProvider = provider.GetRequiredKeyedService<IReplicaSinkProvider>("bootstrap");
        ReplicaTargetDescriptor target = new(
            new("bootstrap-client", "orders-read"),
            ReplicaProvisioningMode.CreateIfMissing);
        await sinkProvider.EnsureTargetAsync(target, CancellationToken.None);
        await sinkProvider.WriteAsync(
            new(target, "order-1", 10, ReplicaWriteMode.LatestState, "TestApp.Orders.MappedReplica.V1", "payload-1"),
            CancellationToken.None);
        ReplicaWriteResult deleteResult = await sinkProvider.WriteAsync(
            new(target, "order-1", 11, ReplicaWriteMode.LatestState, "TestApp.Orders.MappedReplica.V1", null)
            {
                IsDeleted = true,
            },
            CancellationToken.None);
        ReplicaTargetInspection inspection = await sinkProvider.InspectAsync(target, CancellationToken.None);
        Assert.Equal(ReplicaWriteOutcome.Applied, deleteResult.Outcome);
        Assert.Equal(11, inspection.LatestSourcePosition);
        Assert.Null(inspection.LatestPayload);
        Assert.Equal(2, inspection.WriteCount);
    }

    /// <summary>
    ///     Ensures duplicate and superseded writes are ignored.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task BootstrapReplicaSinkProviderShouldIgnoreDuplicateAndSupersededWrites()
    {
        ServiceCollection services = [];
        services.AddBootstrapReplicaSink(
            "bootstrap",
            "bootstrap-client",
            options => options.ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing);
        await using ServiceProvider provider = services.BuildServiceProvider();
        IReplicaSinkProvider sinkProvider = provider.GetRequiredKeyedService<IReplicaSinkProvider>("bootstrap");
        ReplicaTargetDescriptor target = new(
            new("bootstrap-client", "orders-read"),
            ReplicaProvisioningMode.CreateIfMissing);
        await sinkProvider.EnsureTargetAsync(target, CancellationToken.None);
        await sinkProvider.WriteAsync(
            new(target, "order-1", 10, ReplicaWriteMode.LatestState, "TestApp.Orders.MappedReplica.V1", "payload-1"),
            CancellationToken.None);
        ReplicaWriteResult duplicate = await sinkProvider.WriteAsync(
            new(target, "order-1", 10, ReplicaWriteMode.LatestState, "TestApp.Orders.MappedReplica.V1", "payload-1"),
            CancellationToken.None);
        ReplicaWriteResult superseded = await sinkProvider.WriteAsync(
            new(target, "order-1", 9, ReplicaWriteMode.LatestState, "TestApp.Orders.MappedReplica.V1", "payload-0"),
            CancellationToken.None);
        Assert.Equal(ReplicaWriteOutcome.DuplicateIgnored, duplicate.Outcome);
        Assert.Equal(ReplicaWriteOutcome.SupersededIgnored, superseded.Outcome);
    }

    /// <summary>
    ///     Ensures the provider supports the happy-path onboarding flow.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task BootstrapReplicaSinkProviderShouldProvisionWriteAndInspect()
    {
        ServiceCollection services = [];
        services.AddBootstrapReplicaSink(
            "bootstrap",
            "bootstrap-client",
            options => options.ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing);
        await using ServiceProvider provider = services.BuildServiceProvider();
        IReplicaSinkProvider sinkProvider = provider.GetRequiredKeyedService<IReplicaSinkProvider>("bootstrap");
        ReplicaTargetDescriptor target = new(
            new("bootstrap-client", "orders-read"),
            ReplicaProvisioningMode.CreateIfMissing);
        ReplicaWriteRequest request = new(
            target,
            "order-1",
            10,
            ReplicaWriteMode.LatestState,
            "TestApp.Orders.MappedReplica.V1",
            "payload-1");
        await sinkProvider.EnsureTargetAsync(target, CancellationToken.None);
        ReplicaWriteResult writeResult = await sinkProvider.WriteAsync(request, CancellationToken.None);
        ReplicaTargetInspection inspection = await sinkProvider.InspectAsync(target, CancellationToken.None);
        Assert.Equal(ReplicaWriteOutcome.Applied, writeResult.Outcome);
        Assert.True(inspection.TargetExists);
        Assert.Equal(10, inspection.LatestSourcePosition);
        Assert.Equal("payload-1", inspection.LatestPayload);
        Assert.Equal(1, inspection.WriteCount);
    }

    /// <summary>
    ///     Ensures all provider operations reject mismatched client keys.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task BootstrapReplicaSinkProviderShouldRejectClientKeyMismatchAcrossOperations()
    {
        ServiceCollection services = [];
        services.AddBootstrapReplicaSink(
            "bootstrap",
            "bootstrap-client",
            options => options.ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing);
        await using ServiceProvider provider = services.BuildServiceProvider();
        IReplicaSinkProvider sinkProvider = provider.GetRequiredKeyedService<IReplicaSinkProvider>("bootstrap");
        ReplicaTargetDescriptor mismatchedTarget = new(
            new("other-client", "orders-read"),
            ReplicaProvisioningMode.CreateIfMissing);
        ReplicaWriteRequest mismatchedRequest = new(
            mismatchedTarget,
            "order-1",
            10,
            ReplicaWriteMode.LatestState,
            "TestApp.Orders.MappedReplica.V1",
            "payload-1");
        InvalidOperationException ensureException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sinkProvider.EnsureTargetAsync(mismatchedTarget, CancellationToken.None));
        InvalidOperationException inspectException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sinkProvider.InspectAsync(mismatchedTarget, CancellationToken.None));
        InvalidOperationException writeException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sinkProvider.WriteAsync(mismatchedRequest, CancellationToken.None));
        Assert.Contains("bootstrap-client", ensureException.Message, StringComparison.Ordinal);
        Assert.Contains("other-client", ensureException.Message, StringComparison.Ordinal);
        Assert.Contains("bootstrap-client", inspectException.Message, StringComparison.Ordinal);
        Assert.Contains("other-client", inspectException.Message, StringComparison.Ordinal);
        Assert.Contains("bootstrap-client", writeException.Message, StringComparison.Ordinal);
        Assert.Contains("other-client", writeException.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Ensures validation-only onboarding fails when the target does not already exist.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task BootstrapReplicaSinkProviderShouldRejectValidateOnlyWhenTargetIsMissing()
    {
        ServiceCollection services = [];
        services.AddBootstrapReplicaSink(
            "bootstrap",
            "bootstrap-client",
            options => options.ProvisioningMode = ReplicaProvisioningMode.ValidateOnly);
        await using ServiceProvider provider = services.BuildServiceProvider();
        IReplicaSinkProvider sinkProvider = provider.GetRequiredKeyedService<IReplicaSinkProvider>("bootstrap");
        ReplicaTargetDescriptor target = new(new("bootstrap-client", "orders-read"));
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sinkProvider.EnsureTargetAsync(target, CancellationToken.None));
        Assert.Contains("does not exist", exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ReplicaProvisioningMode.ValidateOnly), exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Ensures writes fail until the target has been provisioned.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task BootstrapReplicaSinkProviderShouldRejectWritesBeforeProvisioning()
    {
        ServiceCollection services = [];
        services.AddBootstrapReplicaSink(
            "bootstrap",
            "bootstrap-client",
            options => options.ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing);
        await using ServiceProvider provider = services.BuildServiceProvider();
        IReplicaSinkProvider sinkProvider = provider.GetRequiredKeyedService<IReplicaSinkProvider>("bootstrap");
        ReplicaTargetDescriptor target = new(
            new("bootstrap-client", "orders-read"),
            ReplicaProvisioningMode.CreateIfMissing);
        ReplicaWriteRequest request = new(
            target,
            "order-1",
            10,
            ReplicaWriteMode.LatestState,
            "TestApp.Orders.MappedReplica.V1",
            "payload-1");
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sinkProvider.WriteAsync(request, CancellationToken.None));
        Assert.Contains("has not been provisioned", exception.Message, StringComparison.Ordinal);
    }
}