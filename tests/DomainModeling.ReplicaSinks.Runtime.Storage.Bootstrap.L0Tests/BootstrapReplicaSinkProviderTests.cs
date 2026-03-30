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
        Assert.IsType<BootstrapReplicaSinkProvider>(sinkProvider);
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
        ReplicaTargetDescriptor target = new(
            new("bootstrap-client", "orders-read"),
            ReplicaProvisioningMode.ValidateOnly);
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sinkProvider.EnsureTargetAsync(target, CancellationToken.None));
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
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sinkProvider.WriteAsync(request, CancellationToken.None));
        Assert.Contains("has not been provisioned", exception.Message, StringComparison.Ordinal);
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

        InvalidOperationException ensureException = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sinkProvider.EnsureTargetAsync(mismatchedTarget, CancellationToken.None));
        InvalidOperationException inspectException = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sinkProvider.InspectAsync(mismatchedTarget, CancellationToken.None));
        InvalidOperationException writeException = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sinkProvider.WriteAsync(mismatchedRequest, CancellationToken.None));

        Assert.Contains("bootstrap-client", ensureException.Message, StringComparison.Ordinal);
        Assert.Contains("other-client", ensureException.Message, StringComparison.Ordinal);
        Assert.Contains("bootstrap-client", inspectException.Message, StringComparison.Ordinal);
        Assert.Contains("other-client", inspectException.Message, StringComparison.Ordinal);
        Assert.Contains("bootstrap-client", writeException.Message, StringComparison.Ordinal);
        Assert.Contains("other-client", writeException.Message, StringComparison.Ordinal);
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
}