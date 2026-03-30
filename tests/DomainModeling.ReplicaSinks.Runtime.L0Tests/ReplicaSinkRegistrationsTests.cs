using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.DomainModeling.ReplicaSinks.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Bootstrap;

using MississippiTests.DomainModeling.ReplicaSinks.Runtime.L0Tests.Fixtures;


namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.L0Tests;

/// <summary>
///     Tests the thin runtime registration and onboarding validation shell.
/// </summary>
public sealed class ReplicaSinkRegistrationsTests
{
    /// <summary>
    ///     Ensures the runtime shell and bootstrap provider create a runnable happy-path onboarding proof.
    /// </summary>
    [Fact]
    public void AddReplicaSinksAndBootstrapProviderShouldSupportHappyPathValidation()
    {
        ServiceCollection services = [];
        services.AddReplicaSinks();
        services.ScanReplicaSinkAssemblies(typeof(MappedReplicaProjection).Assembly);
        services.AddBootstrapReplicaSink(
            "bootstrap-mapped",
            "bootstrap-client",
            options => options.ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing);
        services.AddBootstrapReplicaSink(
            "bootstrap-direct",
            "bootstrap-client",
            options => options.ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing);
        using ServiceProvider provider = services.BuildServiceProvider();
        IReplicaSinkStartupValidator validator = provider.GetRequiredService<IReplicaSinkStartupValidator>();
        Exception? exception = Record.Exception(() => validator.Validate());
        Assert.Null(exception);
        Assert.Equal(2, provider.GetServices<ReplicaSinkRegistrationDescriptor>().Count());
    }

    /// <summary>
    ///     Ensures the thin shell can be registered repeatedly.
    /// </summary>
    [Fact]
    public void AddReplicaSinksCanBeCalledMultipleTimes()
    {
        ServiceCollection services = [];
        services.AddReplicaSinks();
        IServiceCollection result = services.AddReplicaSinks();
        Assert.Same(services, result);
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<IReplicaSinkProjectionRegistry>());
        Assert.NotNull(provider.GetRequiredService<IReplicaSinkStartupValidator>());
    }

    /// <summary>
    ///     Ensures bindings without a matching sink registration are rejected.
    /// </summary>
    [Fact]
    public void ReplicaSinkStartupValidatorShouldRejectMissingSinkRegistration()
    {
        ServiceCollection services = [];
        services.AddSingleton<IReplicaSinkProjectionRegistry>(
            new ReplicaSinkProjectionRegistry(
            [
                new(
                    typeof(MappedReplicaProjection),
                    "bootstrap-missing",
                    "orders-missing",
                    typeof(MappedReplicaContract),
                    false,
                    ReplicaWriteMode.LatestState),
            ]));
        services.AddReplicaSinks();
        using ServiceProvider provider = services.BuildServiceProvider();
        IReplicaSinkStartupValidator validator = provider.GetRequiredService<IReplicaSinkStartupValidator>();
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => validator.Validate());
        Assert.Contains("no sink registration was found", exception.Message, StringComparison.Ordinal);
        Assert.Contains("bootstrap-missing", exception.Message, StringComparison.Ordinal);
        Assert.Contains("orders-missing", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Ensures direct replication without explicit opt-in is rejected.
    /// </summary>
    [Fact]
    public void ReplicaSinkStartupValidatorShouldRejectDirectProjectionReplicationWithoutOptIn()
    {
        ServiceCollection services = [];
        services.AddSingleton<IReplicaSinkProjectionRegistry>(
            new ReplicaSinkProjectionRegistry(
            [
                new(
                    typeof(MappedReplicaProjection),
                    "bootstrap-direct",
                    "orders-direct",
                    null,
                    false,
                    ReplicaWriteMode.LatestState),
            ]));
        services.AddSingleton(
            new ReplicaSinkRegistrationDescriptor(
                "bootstrap-direct",
                "bootstrap-client",
                BootstrapReplicaSinkProvider.FormatName,
                typeof(BootstrapReplicaSinkProvider),
                ReplicaProvisioningMode.ValidateOnly));
        services.AddReplicaSinks();
        using ServiceProvider provider = services.BuildServiceProvider();
        IReplicaSinkStartupValidator validator = provider.GetRequiredService<IReplicaSinkStartupValidator>();
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => validator.Validate());
        Assert.Contains(
            "Direct projection replication must be explicitly enabled",
            exception.Message,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Ensures history mode is rejected for the runnable onboarding slice.
    /// </summary>
    [Fact]
    public void ReplicaSinkStartupValidatorShouldRejectHistoryWriteMode()
    {
        ServiceCollection services = [];
        services.AddSingleton<IReplicaSinkProjectionRegistry>(
            new ReplicaSinkProjectionRegistry(
            [
                new(
                    typeof(MappedReplicaProjection),
                    "bootstrap-history",
                    "orders-history",
                    typeof(MappedReplicaContract),
                    false,
                    ReplicaWriteMode.History),
            ]));
        services.AddSingleton(
            new ReplicaSinkRegistrationDescriptor(
                "bootstrap-history",
                "bootstrap-client",
                BootstrapReplicaSinkProvider.FormatName,
                typeof(BootstrapReplicaSinkProvider),
                ReplicaProvisioningMode.ValidateOnly));
        services.AddReplicaSinks();
        using ServiceProvider provider = services.BuildServiceProvider();
        IReplicaSinkStartupValidator validator = provider.GetRequiredService<IReplicaSinkStartupValidator>();
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => validator.Validate());
        Assert.Contains("Slice 1 supports only 'LatestState'", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Ensures assembly scanning discovers projection bindings.
    /// </summary>
    [Fact]
    public void ScanReplicaSinkAssembliesShouldDiscoverProjectionBindings()
    {
        ServiceCollection services = [];
        services.AddReplicaSinks();
        services.ScanReplicaSinkAssemblies(typeof(MappedReplicaProjection).Assembly);
        using ServiceProvider provider = services.BuildServiceProvider();
        IReplicaSinkProjectionRegistry registry = provider.GetRequiredService<IReplicaSinkProjectionRegistry>();
        IReadOnlyList<ReplicaSinkProjectionDescriptor> bindings = registry.GetProjectionBindings();
        Assert.Equal(2, bindings.Count);
        Assert.Contains(
            bindings,
            binding => (binding.ProjectionType == typeof(MappedReplicaProjection)) &&
                       (binding.ContractType == typeof(MappedReplicaContract)));
        Assert.Contains(
            bindings,
            binding => (binding.ProjectionType == typeof(DirectReplicaProjection)) &&
                       binding.IsDirectProjectionReplicationEnabled);
    }
}