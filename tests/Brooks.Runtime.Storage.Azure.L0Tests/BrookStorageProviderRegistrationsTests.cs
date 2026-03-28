using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Blobs;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Runtime.Storage.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Azure;


namespace Mississippi.Brooks.Runtime.Storage.Azure.L0Tests;

/// <summary>
///     Surface-level registration and startup validation tests for the Brooks Azure provider scaffold.
/// </summary>
public sealed class BrookStorageProviderRegistrationsTests
{
    /// <summary>
    ///     Verifies that the no-argument registration wires the provider surface and hosted initializer without requiring an
    ///     immediate blob client registration.
    /// </summary>
    [Fact]
    public void AddAzureBrookStorageProviderRegistersCoreServicesWithoutBlobClient()
    {
        ServiceCollection services = new();

        services.AddAzureBrookStorageProvider();

        List<ServiceDescriptor> descriptors = services.ToList();
        List<Type> serviceTypes = descriptors.Select(static descriptor => descriptor.ServiceType).ToList();

        Assert.Contains(typeof(IBrookStorageProvider), serviceTypes);
        Assert.Contains(typeof(IBrookStorageReader), serviceTypes);
        Assert.Contains(typeof(IBrookStorageWriter), serviceTypes);
        Assert.Contains(typeof(IHostedService), serviceTypes);
        Assert.DoesNotContain(descriptors, static descriptor => descriptor.ServiceType == typeof(BlobServiceClient));
    }

    /// <summary>
    ///     Verifies that configuration binding populates the Brooks Azure options shape.
    /// </summary>
    [Fact]
    public void AddAzureBrookStorageProviderWithConfigurationBindsOptions()
    {
        ServiceCollection services = new();
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [nameof(BrookStorageOptions.BlobServiceClientServiceKey)] = "brooks-shared",
                    [nameof(BrookStorageOptions.ContainerName)] = "brooks-east",
                    [nameof(BrookStorageOptions.LockContainerName)] = "locks-east",
                    [nameof(BrookStorageOptions.InitializationMode)] = nameof(BrookStorageInitializationMode.ValidateOnly),
                    [nameof(BrookStorageOptions.LeaseDurationSeconds)] = "75",
                    [nameof(BrookStorageOptions.LeaseRenewalThresholdSeconds)] = "25",
                    [nameof(BrookStorageOptions.MaxEventsPerBatch)] = "55",
                    [nameof(BrookStorageOptions.ReadPrefetchCount)] = "12",
                })
            .Build();

        services.AddAzureBrookStorageProvider(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        BrookStorageOptions options = provider.GetRequiredService<IOptions<BrookStorageOptions>>().Value;
        Assert.Equal("brooks-shared", options.BlobServiceClientServiceKey);
        Assert.Equal("brooks-east", options.ContainerName);
        Assert.Equal("locks-east", options.LockContainerName);
        Assert.Equal(BrookStorageInitializationMode.ValidateOnly, options.InitializationMode);
        Assert.Equal(75, options.LeaseDurationSeconds);
        Assert.Equal(25, options.LeaseRenewalThresholdSeconds);
        Assert.Equal(55, options.MaxEventsPerBatch);
        Assert.Equal(12, options.ReadPrefetchCount);
    }

    /// <summary>
    ///     Verifies the configure-options overload populates the Brooks Azure options instance.
    /// </summary>
    [Fact]
    public void AddAzureBrookStorageProviderWithConfigureOptionsAppliesOptions()
    {
        ServiceCollection services = new();

        services.AddAzureBrookStorageProvider(options =>
        {
            options.BlobServiceClientServiceKey = "brooks-shared";
            options.ContainerName = "brooks-prod";
            options.LockContainerName = "locks-prod";
            options.InitializationMode = BrookStorageInitializationMode.ValidateOnly;
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        BrookStorageOptions options = provider.GetRequiredService<IOptions<BrookStorageOptions>>().Value;

        Assert.Equal("brooks-shared", options.BlobServiceClientServiceKey);
        Assert.Equal("brooks-prod", options.ContainerName);
        Assert.Equal("locks-prod", options.LockContainerName);
        Assert.Equal(BrookStorageInitializationMode.ValidateOnly, options.InitializationMode);
    }

    /// <summary>
    ///     Verifies that the connection-string convenience overload registers a keyed blob client using the configured
    ///     service key.
    /// </summary>
    [Fact]
    public void AddAzureBrookStorageProviderWithConnectionStringRegistersConfiguredKeyedClient()
    {
        ServiceCollection services = new();

        services.AddAzureBrookStorageProvider(
            "UseDevelopmentStorage=true",
            options =>
            {
                options.BlobServiceClientServiceKey = "shared-azure";
                options.ContainerName = "brooks-prod";
                options.LockContainerName = "locks-prod";
            });

        using ServiceProvider provider = services.BuildServiceProvider();
        BlobServiceClient client = provider.GetRequiredKeyedService<BlobServiceClient>("shared-azure");
        BrookStorageOptions options = provider.GetRequiredService<IOptions<BrookStorageOptions>>().Value;

        Assert.NotNull(client);
        Assert.Equal("shared-azure", options.BlobServiceClientServiceKey);
        Assert.Equal("brooks-prod", options.ContainerName);
    }

    /// <summary>
    ///     Verifies that startup fails with an actionable message when the configured keyed blob client is missing.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task AzureBrookStorageInitializerFailsFastWhenKeyedBlobClientIsMissing()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddAzureBrookStorageProvider(options =>
        {
            options.InitializationMode = BrookStorageInitializationMode.ValidateOnly;
            options.ContainerName = "brooks-prod";
            options.LockContainerName = "locks-prod";
            options.BlobServiceClientServiceKey = "missing-client";
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        AzureBrookStorageInitializer initializer = provider.GetServices<IHostedService>()
            .OfType<AzureBrookStorageInitializer>()
            .Single();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => initializer.StartAsync(CancellationToken.None));

        Assert.Contains("missing-client", exception.Message, StringComparison.Ordinal);
        Assert.Contains("AddAzureBrookStorageProvider(connectionString", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies that when later option configuration changes the service key after the connection-string overload has
    ///     registered its keyed client, startup fails against the final configured key instead of silently selecting a
    ///     different account.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task AzureBrookStorageInitializerFailsAgainstTheFinalConfiguredServiceKeyWhenConfigurationOverridesIt()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddAzureBrookStorageProvider(
            "UseDevelopmentStorage=true",
            options =>
            {
                options.BlobServiceClientServiceKey = "registered-client";
                options.InitializationMode = BrookStorageInitializationMode.ValidateOnly;
                options.ContainerName = "brooks-prod";
                options.LockContainerName = "locks-prod";
            });
        services.Configure<BrookStorageOptions>(options => { options.BlobServiceClientServiceKey = "overridden-client"; });

        using ServiceProvider provider = services.BuildServiceProvider();
        AzureBrookStorageInitializer initializer = provider.GetServices<IHostedService>()
            .OfType<AzureBrookStorageInitializer>()
            .Single();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => initializer.StartAsync(CancellationToken.None));

        Assert.Contains("overridden-client", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("registered-client", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("UseDevelopmentStorage=true", exception.Message, StringComparison.Ordinal);
    }
}