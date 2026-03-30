using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Mississippi.Common.Abstractions.Mapping;
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
    private static async Task<InvalidOperationException> AssertValidationFailureAsync(
        IServiceCollection services
    )
    {
        services.AddReplicaSinks();
        await using ServiceProvider provider = services.BuildServiceProvider();
        IReplicaSinkStartupValidator validator = provider.GetRequiredService<IReplicaSinkStartupValidator>();
        return await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await validator.ValidateAsync(CancellationToken.None));
    }

    private static ReplicaSinkProjectionDescriptor CreateDirectBinding(
        Type projectionType,
        string sinkKey,
        string targetName,
        bool isDirectProjectionReplicationEnabled = true
    ) =>
        new(
            projectionType,
            sinkKey,
            targetName,
            null,
            isDirectProjectionReplicationEnabled,
            ReplicaWriteMode.LatestState);

    private static ReplicaSinkProjectionDescriptor CreateMappedBinding(
        Type projectionType,
        string sinkKey,
        string targetName
    ) =>
        new(projectionType, sinkKey, targetName, typeof(MappedReplicaContract), false, ReplicaWriteMode.LatestState);

    /// <summary>
    ///     Ensures the runtime shell and bootstrap provider execute a runnable happy-path onboarding proof.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task AddReplicaSinksAndBootstrapProviderShouldProvisionTargetsDuringHappyPathValidation()
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
        services.AddMapper<MappedReplicaProjection, MappedReplicaContract, MappedReplicaProjectionToContractMapper>();
        await using ServiceProvider provider = services.BuildServiceProvider();
        IReplicaSinkStartupValidator validator = provider.GetRequiredService<IReplicaSinkStartupValidator>();
        await validator.ValidateAsync(CancellationToken.None);
        IReplicaSinkProvider mappedProvider =
            provider.GetRequiredKeyedService<IReplicaSinkProvider>("bootstrap-mapped");
        IReplicaSinkProvider directProvider =
            provider.GetRequiredKeyedService<IReplicaSinkProvider>("bootstrap-direct");
        ReplicaTargetInspection mappedInspection = await mappedProvider.InspectAsync(
            new(new("bootstrap-client", "orders-read"), ReplicaProvisioningMode.CreateIfMissing),
            CancellationToken.None);
        ReplicaTargetInspection directInspection = await directProvider.InspectAsync(
            new(new("bootstrap-client", "orders-direct"), ReplicaProvisioningMode.CreateIfMissing),
            CancellationToken.None);
        Assert.True(mappedInspection.TargetExists);
        Assert.True(directInspection.TargetExists);
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
    ///     Ensures ambiguous sink registration multiplicity surfaces the stable <c>RS0008</c> diagnostic.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReplicaSinkStartupValidatorShouldRejectAmbiguousSinkRegistrationMultiplicity()
    {
        ServiceCollection services = [];
        ReplicaSinkProjectionDescriptor binding = CreateMappedBinding(
            typeof(MappedReplicaProjection),
            "bootstrap-ambiguous",
            "orders-ambiguous");
        services.AddSingleton(binding);
        services.AddSingleton(
            new ReplicaSinkRegistrationDescriptor(
                "bootstrap-ambiguous",
                "bootstrap-client-a",
                BootstrapReplicaSinkProvider.FormatName,
                typeof(BootstrapReplicaSinkProvider),
                ReplicaProvisioningMode.CreateIfMissing));
        services.AddSingleton(
            new ReplicaSinkRegistrationDescriptor(
                "bootstrap-ambiguous",
                "bootstrap-client-b",
                BootstrapReplicaSinkProvider.FormatName,
                typeof(BootstrapReplicaSinkProvider),
                ReplicaProvisioningMode.CreateIfMissing));
        services.AddMapper<MappedReplicaProjection, MappedReplicaContract, MappedReplicaProjectionToContractMapper>();
        InvalidOperationException exception = await AssertValidationFailureAsync(services);
        ReplicaSinkStartupDiagnostic expected = ReplicaSinkStartupDiagnostics.CreateInvalidSinkRegistrationMultiplicity(
            binding,
            2);
        Assert.Contains(expected.Id, exception.Message, StringComparison.Ordinal);
        Assert.Contains(expected.Message, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Ensures direct replication without explicit opt-in surfaces the stable <c>RS0005</c> diagnostic.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReplicaSinkStartupValidatorShouldRejectDirectProjectionReplicationWithoutOptIn()
    {
        ServiceCollection services = [];
        ReplicaSinkProjectionDescriptor binding = CreateDirectBinding(
            typeof(DirectReplicaProjection),
            "bootstrap-direct",
            "orders-direct",
            false);
        services.AddSingleton(binding);
        services.AddBootstrapReplicaSink(
            "bootstrap-direct",
            "bootstrap-client",
            options => options.ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing);
        InvalidOperationException exception = await AssertValidationFailureAsync(services);
        ReplicaSinkStartupDiagnostic expected =
            ReplicaSinkStartupDiagnostics.CreateDirectReplicationRequiresExplicitOptIn(binding);
        Assert.Contains(expected.Id, exception.Message, StringComparison.Ordinal);
        Assert.Contains(expected.Message, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Ensures duplicate logical bindings surface the stable <c>RS0004</c> diagnostic.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReplicaSinkStartupValidatorShouldRejectDuplicateLogicalBinding()
    {
        ServiceCollection services = [];
        ReplicaSinkProjectionDescriptor firstBinding = CreateMappedBinding(
            typeof(MappedReplicaProjection),
            "bootstrap-mapped",
            "orders-read");
        ReplicaSinkProjectionDescriptor secondBinding = CreateMappedBinding(
            typeof(MappedReplicaProjection),
            "bootstrap-mapped",
            "orders-read");
        services.AddSingleton(firstBinding);
        services.AddSingleton(secondBinding);
        services.AddBootstrapReplicaSink(
            "bootstrap-mapped",
            "bootstrap-client",
            options => options.ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing);
        services.AddMapper<MappedReplicaProjection, MappedReplicaContract, MappedReplicaProjectionToContractMapper>();
        InvalidOperationException exception = await AssertValidationFailureAsync(services);
        ReplicaSinkStartupDiagnostic expected = ReplicaSinkStartupDiagnostics.CreateDuplicateLogicalBinding(
            new(
                ReplicaSinkStartupDiagnostics.GetTypeDisplayName(typeof(MappedReplicaProjection)),
                "bootstrap-mapped",
                "orders-read"));
        Assert.Contains(expected.Id, exception.Message, StringComparison.Ordinal);
        Assert.Contains(expected.Message, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Ensures history mode is rejected for the runnable onboarding slice with stable <c>RS0006</c> diagnostics.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReplicaSinkStartupValidatorShouldRejectHistoryWriteMode()
    {
        ServiceCollection services = [];
        ReplicaSinkProjectionDescriptor binding = new(
            typeof(MappedReplicaProjection),
            "bootstrap-history",
            "orders-history",
            typeof(MappedReplicaContract),
            false,
            ReplicaWriteMode.History);
        services.AddSingleton(binding);
        services.AddBootstrapReplicaSink(
            "bootstrap-history",
            "bootstrap-client",
            options => options.ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing);
        services.AddMapper<MappedReplicaProjection, MappedReplicaContract, MappedReplicaProjectionToContractMapper>();
        InvalidOperationException exception = await AssertValidationFailureAsync(services);
        ReplicaSinkStartupDiagnostic
            expected = ReplicaSinkStartupDiagnostics.CreateUnsupportedHistoryWriteMode(binding);
        Assert.Contains(expected.Id, exception.Message, StringComparison.Ordinal);
        Assert.Contains(expected.Message, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Ensures missing mapper registrations surface the stable <c>RS0003</c> diagnostic.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReplicaSinkStartupValidatorShouldRejectMissingMapperForReplicaContract()
    {
        ServiceCollection services = [];
        ReplicaSinkProjectionDescriptor binding = CreateMappedBinding(
            typeof(MappedReplicaProjection),
            "bootstrap-mapped",
            "orders-read");
        services.AddSingleton(binding);
        services.AddBootstrapReplicaSink(
            "bootstrap-mapped",
            "bootstrap-client",
            options => options.ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing);
        InvalidOperationException exception = await AssertValidationFailureAsync(services);
        ReplicaSinkStartupDiagnostic expected = ReplicaSinkStartupDiagnostics.CreateMissingMapper(binding);
        Assert.Contains(expected.Id, exception.Message, StringComparison.Ordinal);
        Assert.Contains(expected.Message, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Ensures missing replica contract identity metadata surfaces the stable <c>RS0002</c> diagnostic.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReplicaSinkStartupValidatorShouldRejectMissingReplicaContractIdentity()
    {
        ServiceCollection services = [];
        ReplicaSinkProjectionDescriptor binding = new(
            typeof(MappedReplicaProjection),
            "bootstrap-contract",
            "orders-contract",
            typeof(UnnamedReplicaContract),
            false,
            ReplicaWriteMode.LatestState);
        services.AddSingleton(binding);
        services.AddBootstrapReplicaSink(
            "bootstrap-contract",
            "bootstrap-client",
            options => options.ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing);
        InvalidOperationException exception = await AssertValidationFailureAsync(services);
        ReplicaSinkStartupDiagnostic expected = ReplicaSinkStartupDiagnostics.CreateMissingReplicaContractName(binding);
        Assert.Contains(expected.Id, exception.Message, StringComparison.Ordinal);
        Assert.Contains(expected.Message, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Ensures missing provider handles surface the stable <c>RS0008</c> diagnostic.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReplicaSinkStartupValidatorShouldRejectMissingRuntimeProviderHandle()
    {
        ServiceCollection services = [];
        ReplicaSinkProjectionDescriptor binding = CreateMappedBinding(
            typeof(MappedReplicaProjection),
            "bootstrap-runtime",
            "orders-runtime");
        services.AddSingleton(binding);
        services.AddSingleton(
            new ReplicaSinkRegistrationDescriptor(
                "bootstrap-runtime",
                "bootstrap-client",
                BootstrapReplicaSinkProvider.FormatName,
                typeof(BootstrapReplicaSinkProvider),
                ReplicaProvisioningMode.CreateIfMissing));
        services.AddMapper<MappedReplicaProjection, MappedReplicaContract, MappedReplicaProjectionToContractMapper>();
        InvalidOperationException exception = await AssertValidationFailureAsync(services);
        ReplicaSinkStartupDiagnostic expected = ReplicaSinkStartupDiagnostics.CreateMissingProviderHandle(binding);
        Assert.Contains(expected.Id, exception.Message, StringComparison.Ordinal);
        Assert.Contains(expected.Message, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Ensures missing sink registrations surface the stable <c>RS0001</c> diagnostic.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReplicaSinkStartupValidatorShouldRejectMissingSinkRegistration()
    {
        ServiceCollection services = [];
        ReplicaSinkProjectionDescriptor binding = CreateMappedBinding(
            typeof(MappedReplicaProjection),
            "bootstrap-missing",
            "orders-missing");
        services.AddSingleton(binding);
        services.AddMapper<MappedReplicaProjection, MappedReplicaContract, MappedReplicaProjectionToContractMapper>();
        InvalidOperationException exception = await AssertValidationFailureAsync(services);
        ReplicaSinkStartupDiagnostic expected = ReplicaSinkStartupDiagnostics.CreateMissingSinkRegistration(binding);
        Assert.Contains(expected.Id, exception.Message, StringComparison.Ordinal);
        Assert.Contains(expected.Message, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Ensures overlapping physical targets surface the stable <c>RS0007</c> diagnostic.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReplicaSinkStartupValidatorShouldRejectOverlappingPhysicalTargets()
    {
        ServiceCollection services = [];
        services.AddSingleton(
            CreateMappedBinding(typeof(MappedReplicaProjection), "bootstrap-primary", "orders-shared"));
        services.AddSingleton(
            CreateDirectBinding(typeof(DirectReplicaProjection), "bootstrap-secondary", "orders-shared"));
        services.AddBootstrapReplicaSink(
            "bootstrap-primary",
            "bootstrap-client",
            options => options.ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing);
        services.AddBootstrapReplicaSink(
            "bootstrap-secondary",
            "bootstrap-client",
            options => options.ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing);
        services.AddMapper<MappedReplicaProjection, MappedReplicaContract, MappedReplicaProjectionToContractMapper>();
        InvalidOperationException exception = await AssertValidationFailureAsync(services);
        ReplicaSinkStartupDiagnostic expected = ReplicaSinkStartupDiagnostics.CreatePhysicalTargetOverlap(
            [
                new(
                    ReplicaSinkStartupDiagnostics.GetTypeDisplayName(typeof(DirectReplicaProjection)),
                    "bootstrap-secondary",
                    "orders-shared"),
                new(
                    ReplicaSinkStartupDiagnostics.GetTypeDisplayName(typeof(MappedReplicaProjection)),
                    "bootstrap-primary",
                    "orders-shared"),
            ],
            new(
                "bootstrap-primary",
                "bootstrap-client",
                BootstrapReplicaSinkProvider.FormatName,
                typeof(BootstrapReplicaSinkProvider),
                ReplicaProvisioningMode.CreateIfMissing),
            new(new("bootstrap-client", "orders-shared"), ReplicaProvisioningMode.CreateIfMissing));
        Assert.Contains(expected.Id, exception.Message, StringComparison.Ordinal);
        Assert.Contains(expected.Message, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Ensures repeated assembly scans merge without last-call-wins replacement and cache stable binding descriptors.
    /// </summary>
    [Fact]
    public void ScanReplicaSinkAssembliesShouldMergeRepeatedCallsAndCacheStableBindingDescriptors()
    {
        ServiceCollection services = [];
        services.AddReplicaSinks();
        services.ScanReplicaSinkAssemblies(typeof(MappedReplicaProjection).Assembly);
        services.ScanReplicaSinkAssemblies(typeof(MappedReplicaProjection).Assembly);
        services.AddBootstrapReplicaSink(
            "bootstrap-mapped",
            "bootstrap-client",
            options => options.ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing);
        services.AddBootstrapReplicaSink(
            "bootstrap-direct",
            "bootstrap-client",
            options => options.ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing);
        services.AddMapper<MappedReplicaProjection, MappedReplicaContract, MappedReplicaProjectionToContractMapper>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IReplicaSinkProjectionRegistry registry = provider.GetRequiredService<IReplicaSinkProjectionRegistry>();
        IReadOnlyList<ReplicaSinkBindingDescriptor> bindings = registry.GetBindingDescriptors();
        Assert.Same(bindings, registry.GetBindingDescriptors());
        Assert.Same(registry.GetDiagnostics(), registry.GetDiagnostics());
        Assert.Empty(registry.GetDiagnostics());
        Assert.Equal(2, bindings.Count);
        ReplicaSinkBindingDescriptor mappedBinding =
            bindings.Single(binding => binding.ProjectionType == typeof(MappedReplicaProjection));
        ReplicaSinkBindingDescriptor directBinding =
            bindings.Single(binding => binding.ProjectionType == typeof(DirectReplicaProjection));
        Assert.Equal(
            "MississippiTests.DomainModeling.ReplicaSinks.Runtime.L0Tests.Fixtures.MappedReplicaProjection|bootstrap-mapped|orders-read",
            mappedBinding.BindingIdentity.ToString());
        Assert.Equal("TestApp.Orders.MappedReplica.V1", mappedBinding.ContractIdentity);
        Assert.Equal(typeof(MappedReplicaContract), mappedBinding.ContractType);
        Assert.NotNull(mappedBinding.MapperDelegate);
        MappedReplicaContract mappedContract = Assert.IsType<MappedReplicaContract>(
            mappedBinding.MapperDelegate!(
                new MappedReplicaProjection
                {
                    Id = "order-42",
                }));
        Assert.Equal("order-42", mappedContract.Id);
        Assert.False(mappedBinding.UsesDirectMaterialization);
        Assert.Equal("bootstrap-client", mappedBinding.ValidatedTargetDescriptor.DestinationIdentity.ClientKey);
        Assert.Equal("orders-read", mappedBinding.ValidatedTargetDescriptor.DestinationIdentity.TargetName);
        Assert.Same(
            provider.GetRequiredKeyedService<IReplicaSinkProvider>("bootstrap-mapped"),
            mappedBinding.ProviderHandle);
        Assert.Equal(
            "MississippiTests.DomainModeling.ReplicaSinks.Runtime.L0Tests.Fixtures.DirectReplicaProjection",
            directBinding.ContractIdentity);
        Assert.Null(directBinding.ContractType);
        Assert.Null(directBinding.MapperDelegate);
        Assert.True(directBinding.UsesDirectMaterialization);
        Assert.Same(
            provider.GetRequiredKeyedService<IReplicaSinkProvider>("bootstrap-direct"),
            directBinding.ProviderHandle);
    }
}