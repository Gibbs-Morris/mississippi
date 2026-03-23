using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Blobs;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Serialization.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blob.Startup;
using Mississippi.Tributary.Runtime.Storage.Blob.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L0Tests;

/// <summary>
///     Tests for Blob snapshot storage provider DI registrations and startup validation.
/// </summary>
public sealed class SnapshotBlobStorageProviderRegistrationsTests
{
    /// <summary>
    ///     Ensures the canonical registration path wires the provider and keyed container client.
    /// </summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderShouldRegisterServices()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeyedSingleton<BlobServiceClient>(
            SnapshotBlobDefaults.BlobServiceClientServiceKey,
            new BlobServiceClient("UseDevelopmentStorage=true"));
        services.AddSingleton<ISerializationProvider>(new TestSerializationProvider(SnapshotBlobDefaults.PayloadSerializerFormat));
        services.Configure<SnapshotBlobStorageOptions>(options => options.ContainerName = "snapshots-test");

        services.AddBlobSnapshotStorageProvider();

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<ISnapshotStorageProvider>());
        Assert.NotNull(provider.GetRequiredService<ISnapshotStorageReader>());
        Assert.NotNull(provider.GetRequiredService<ISnapshotStorageWriter>());
        Assert.NotNull(provider.GetRequiredService<IBlobEnvelopeCodec>());
        Assert.NotNull(provider.GetRequiredService<SnapshotPayloadSerializerResolver>());
        Assert.NotNull(provider.GetRequiredKeyedService<BlobContainerClient>(SnapshotBlobDefaults.BlobContainerServiceKey));
        Assert.Single(provider.GetServices<IHostedService>());
    }

    /// <summary>
    ///     Ensures the connection string overload creates the keyed Blob client and applies options.
    /// </summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderWithConnectionStringShouldRegisterKeyedClientAndConfigureOptions()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<ISerializationProvider>(new TestSerializationProvider(SnapshotBlobDefaults.PayloadSerializerFormat));

        services.AddBlobSnapshotStorageProvider(
            "UseDevelopmentStorage=true",
            options => options.ContainerName = "connection-string-container");

        using ServiceProvider provider = services.BuildServiceProvider();

        SnapshotBlobStorageOptions options = provider.GetRequiredService<IOptions<SnapshotBlobStorageOptions>>().Value;
        BlobServiceClient blobServiceClient = provider.GetRequiredKeyedService<BlobServiceClient>(
            SnapshotBlobDefaults.BlobServiceClientServiceKey);

        Assert.Equal("connection-string-container", options.ContainerName);
        Assert.NotNull(blobServiceClient);
    }

    /// <summary>
    ///     Ensures the connection string overload honors a non-default Blob service client key override.
    /// </summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderWithConnectionStringShouldHonorBlobServiceClientServiceKeyOverride()
    {
        const string CustomBlobServiceClientServiceKey = "custom-blob-client";

        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<ISerializationProvider>(new TestSerializationProvider(SnapshotBlobDefaults.PayloadSerializerFormat));

        services.AddBlobSnapshotStorageProvider(
            "UseDevelopmentStorage=true",
            options =>
            {
                options.ContainerName = "connection-string-custom-key-container";
                options.BlobServiceClientServiceKey = CustomBlobServiceClientServiceKey;
            });

        using ServiceProvider provider = services.BuildServiceProvider();

        SnapshotBlobStorageOptions options = provider.GetRequiredService<IOptions<SnapshotBlobStorageOptions>>().Value;
        BlobServiceClient blobServiceClient = provider.GetRequiredKeyedService<BlobServiceClient>(CustomBlobServiceClientServiceKey);
        BlobContainerClient containerClient = provider.GetRequiredKeyedService<BlobContainerClient>(SnapshotBlobDefaults.BlobContainerServiceKey);

        Assert.Equal(CustomBlobServiceClientServiceKey, options.BlobServiceClientServiceKey);
        Assert.Equal("connection-string-custom-key-container", options.ContainerName);
        Assert.NotNull(blobServiceClient);
        Assert.Equal("connection-string-custom-key-container", containerClient.Name);
    }

    /// <summary>
    ///     Ensures the options delegate overload configures options before registration completes.
    /// </summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderWithOptionsShouldConfigureOptions()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeyedSingleton<BlobServiceClient>(
            SnapshotBlobDefaults.BlobServiceClientServiceKey,
            new BlobServiceClient("UseDevelopmentStorage=true"));
        services.AddSingleton<ISerializationProvider>(new TestSerializationProvider("custom-json"));

        services.AddBlobSnapshotStorageProvider(options =>
        {
            options.ContainerName = "options-container";
            options.PayloadSerializerFormat = "custom-json";
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        SnapshotBlobStorageOptions options = provider.GetRequiredService<IOptions<SnapshotBlobStorageOptions>>().Value;
        Assert.Equal("options-container", options.ContainerName);
        Assert.Equal("custom-json", options.PayloadSerializerFormat);
    }

    /// <summary>
    ///     Ensures the configuration overload binds options from configuration.
    /// </summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderWithConfigurationShouldBindOptions()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    { nameof(SnapshotBlobStorageOptions.ContainerName), "config-container" },
                    { nameof(SnapshotBlobStorageOptions.PayloadSerializerFormat), "custom-json" },
                })
            .Build();

        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeyedSingleton<BlobServiceClient>(
            SnapshotBlobDefaults.BlobServiceClientServiceKey,
            new BlobServiceClient("UseDevelopmentStorage=true"));
        services.AddSingleton<ISerializationProvider>(new TestSerializationProvider("custom-json"));

        services.AddBlobSnapshotStorageProvider(configuration);
        services.AddSingleton<IBlobContainerInitializerOperations>(new StubBlobContainerInitializerOperations());

        using ServiceProvider provider = services.BuildServiceProvider();

        SnapshotBlobStorageOptions options = provider.GetRequiredService<IOptions<SnapshotBlobStorageOptions>>().Value;
        Assert.Equal("config-container", options.ContainerName);
        Assert.Equal("custom-json", options.PayloadSerializerFormat);
    }

    /// <summary>
    ///     Ensures startup fails when the configured Blob client registration is missing.
    /// </summary>
    [Fact]
    public void AddBlobSnapshotStorageProviderShouldRequireConfiguredBlobClient()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<ISerializationProvider>(new TestSerializationProvider(SnapshotBlobDefaults.PayloadSerializerFormat));
        services.AddBlobSnapshotStorageProvider();

        using ServiceProvider provider = services.BuildServiceProvider();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredService<IHostedService>());

        Assert.Contains(nameof(BlobServiceClient), exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Ensures startup fails when no serializer matches the configured format.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task BlobContainerInitializerShouldFailWhenNoSerializerMatchesConfiguredFormat()
    {
        TestLogger<BlobContainerInitializer> logger = new();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeyedSingleton<BlobServiceClient>(
            SnapshotBlobDefaults.BlobServiceClientServiceKey,
            new BlobServiceClient("UseDevelopmentStorage=true"));
        services.AddSingleton<ISerializationProvider>(new TestSerializationProvider("other-format"));
        services.AddBlobSnapshotStorageProvider(options => options.PayloadSerializerFormat = "missing-format");
        services.AddSingleton<IBlobContainerInitializerOperations>(new StubBlobContainerInitializerOperations());
        services.AddSingleton<ILogger<BlobContainerInitializer>>(logger);

        await using ServiceProvider provider = services.BuildServiceProvider();

        IHostedService hostedService = provider.GetRequiredService<IHostedService>();
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => hostedService.StartAsync(CancellationToken.None));
        TestLogEntry validationLog = Assert.Single(logger.Entries, entry => entry.EventId.Id == 2413);

        Assert.Contains("startup validation failed", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("snapshots", exception.Message, StringComparison.Ordinal);
        Assert.Contains("missing-format", exception.Message, StringComparison.Ordinal);
        Assert.Equal(LogLevel.Error, validationLog.Level);
        Assert.Equal("snapshots", Assert.IsType<string>(validationLog.State["containerName"]));
        Assert.Equal("missing-format", Assert.IsType<string>(validationLog.State["payloadSerializerFormat"]));
        Assert.IsType<InvalidOperationException>(validationLog.Exception);
    }

    /// <summary>
    ///     Ensures startup fails when multiple serializers match the configured format.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task BlobContainerInitializerShouldFailWhenMultipleSerializersMatchConfiguredFormat()
    {
        TestLogger<BlobContainerInitializer> logger = new();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeyedSingleton<BlobServiceClient>(
            SnapshotBlobDefaults.BlobServiceClientServiceKey,
            new BlobServiceClient("UseDevelopmentStorage=true"));
        services.AddSingleton<ISerializationProvider>(new TestSerializationProvider("duplicate-format"));
        services.AddSingleton<ISerializationProvider>(new TestSerializationProvider("duplicate-format"));
        services.AddBlobSnapshotStorageProvider(options => options.PayloadSerializerFormat = "duplicate-format");
        services.AddSingleton<IBlobContainerInitializerOperations>(new StubBlobContainerInitializerOperations());
        services.AddSingleton<ILogger<BlobContainerInitializer>>(logger);

        await using ServiceProvider provider = services.BuildServiceProvider();

        IHostedService hostedService = provider.GetRequiredService<IHostedService>();
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => hostedService.StartAsync(CancellationToken.None));
        TestLogEntry validationLog = Assert.Single(logger.Entries, entry => entry.EventId.Id == 2413);

        Assert.Contains("startup validation failed", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("duplicate-format", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Multiple", exception.Message, StringComparison.Ordinal);
        Assert.Equal(LogLevel.Error, validationLog.Level);
        Assert.Equal("snapshots", Assert.IsType<string>(validationLog.State["containerName"]));
        Assert.Equal("duplicate-format", Assert.IsType<string>(validationLog.State["payloadSerializerFormat"]));
        Assert.IsType<InvalidOperationException>(validationLog.Exception);
    }

    /// <summary>
    ///     Ensures startup creates the container when CreateIfMissing is configured.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task BlobContainerInitializerShouldCreateContainerWhenConfigured()
    {
        StubBlobContainerInitializerOperations operations = new();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeyedSingleton<BlobServiceClient>(
            SnapshotBlobDefaults.BlobServiceClientServiceKey,
            new BlobServiceClient("UseDevelopmentStorage=true"));
        services.AddSingleton<ISerializationProvider>(new TestSerializationProvider(SnapshotBlobDefaults.PayloadSerializerFormat));
        services.AddBlobSnapshotStorageProvider(options =>
        {
            options.ContainerName = "create-container";
            options.ContainerInitializationMode = SnapshotBlobContainerInitializationMode.CreateIfMissing;
        });
        services.AddSingleton<IBlobContainerInitializerOperations>(operations);

        await using ServiceProvider provider = services.BuildServiceProvider();

        IHostedService hostedService = provider.GetRequiredService<IHostedService>();
        await hostedService.StartAsync(CancellationToken.None);

        Assert.Equal(1, operations.CreateIfNotExistsCallCount);
        Assert.Equal(0, operations.ExistsCallCount);
    }

    /// <summary>
    ///     Ensures startup fails when ValidateExists is configured and the container is missing.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task BlobContainerInitializerShouldFailWhenValidateExistsContainerIsMissing()
    {
        StubBlobContainerInitializerOperations operations = new()
        {
            ExistsResult = false,
        };
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeyedSingleton<BlobServiceClient>(
            SnapshotBlobDefaults.BlobServiceClientServiceKey,
            new BlobServiceClient("UseDevelopmentStorage=true"));
        services.AddSingleton<ISerializationProvider>(new TestSerializationProvider(SnapshotBlobDefaults.PayloadSerializerFormat));
        services.AddBlobSnapshotStorageProvider(options =>
        {
            options.ContainerName = "missing-container";
            options.ContainerInitializationMode = SnapshotBlobContainerInitializationMode.ValidateExists;
        });
        services.AddSingleton<IBlobContainerInitializerOperations>(operations);

        await using ServiceProvider provider = services.BuildServiceProvider();

        IHostedService hostedService = provider.GetRequiredService<IHostedService>();
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => hostedService.StartAsync(CancellationToken.None));

        Assert.Contains("missing-container", exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(SnapshotBlobContainerInitializationMode.ValidateExists), exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, operations.CreateIfNotExistsCallCount);
        Assert.Equal(1, operations.ExistsCallCount);
    }

    /// <summary>
    ///     Ensures startup succeeds when ValidateExists is configured and the container exists.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task BlobContainerInitializerShouldSucceedWhenValidateExistsContainerExists()
    {
        StubBlobContainerInitializerOperations operations = new()
        {
            ExistsResult = true,
        };
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeyedSingleton<BlobServiceClient>(
            SnapshotBlobDefaults.BlobServiceClientServiceKey,
            new BlobServiceClient("UseDevelopmentStorage=true"));
        services.AddSingleton<ISerializationProvider>(new TestSerializationProvider(SnapshotBlobDefaults.PayloadSerializerFormat));
        services.AddBlobSnapshotStorageProvider(options =>
        {
            options.ContainerInitializationMode = SnapshotBlobContainerInitializationMode.ValidateExists;
        });
        services.AddSingleton<IBlobContainerInitializerOperations>(operations);

        await using ServiceProvider provider = services.BuildServiceProvider();

        IHostedService hostedService = provider.GetRequiredService<IHostedService>();
        await hostedService.StartAsync(CancellationToken.None);

        Assert.Equal(0, operations.CreateIfNotExistsCallCount);
        Assert.Equal(1, operations.ExistsCallCount);
    }

    /// <summary>
    ///     Ensures startup surfaces actionable diagnostics when container creation throws.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task BlobContainerInitializerShouldWrapCreateIfMissingOperationalFailures()
    {
        InvalidOperationException underlyingException = new("simulated create failure");
        StubBlobContainerInitializerOperations operations = new()
        {
            CreateIfNotExistsException = underlyingException,
        };
        TestLogger<BlobContainerInitializer> logger = new();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeyedSingleton<BlobServiceClient>(
            SnapshotBlobDefaults.BlobServiceClientServiceKey,
            new BlobServiceClient("UseDevelopmentStorage=true"));
        services.AddSingleton<ISerializationProvider>(new TestSerializationProvider(SnapshotBlobDefaults.PayloadSerializerFormat));
        services.AddBlobSnapshotStorageProvider(options =>
        {
            options.ContainerName = "create-failure-container";
            options.ContainerInitializationMode = SnapshotBlobContainerInitializationMode.CreateIfMissing;
        });
        services.AddSingleton<IBlobContainerInitializerOperations>(operations);
        services.AddSingleton<ILogger<BlobContainerInitializer>>(logger);

        await using ServiceProvider provider = services.BuildServiceProvider();

        IHostedService hostedService = provider.GetRequiredService<IHostedService>();
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => hostedService.StartAsync(CancellationToken.None));
        TestLogEntry initializationLog = Assert.Single(logger.Entries, entry => entry.EventId.Id == 2414);

        Assert.Contains("create-failure-container", exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(SnapshotBlobContainerInitializationMode.CreateIfMissing), exception.Message, StringComparison.Ordinal);
        Assert.Contains("BlobServiceClient registration", exception.Message, StringComparison.Ordinal);
        Assert.Same(underlyingException, exception.InnerException);
        Assert.Equal(LogLevel.Error, initializationLog.Level);
        Assert.Equal("create-failure-container", Assert.IsType<string>(initializationLog.State["containerName"]));
        Assert.Equal(
            SnapshotBlobContainerInitializationMode.CreateIfMissing,
            Assert.IsType<SnapshotBlobContainerInitializationMode>(initializationLog.State["initializationMode"]));
        Assert.Same(underlyingException, initializationLog.Exception);
    }

    /// <summary>
    ///     Ensures startup surfaces actionable diagnostics when container existence validation throws.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task BlobContainerInitializerShouldWrapValidateExistsOperationalFailures()
    {
        IOException underlyingException = new("simulated exists failure");
        StubBlobContainerInitializerOperations operations = new()
        {
            ExistsException = underlyingException,
        };
        TestLogger<BlobContainerInitializer> logger = new();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddKeyedSingleton<BlobServiceClient>(
            SnapshotBlobDefaults.BlobServiceClientServiceKey,
            new BlobServiceClient("UseDevelopmentStorage=true"));
        services.AddSingleton<ISerializationProvider>(new TestSerializationProvider(SnapshotBlobDefaults.PayloadSerializerFormat));
        services.AddBlobSnapshotStorageProvider(options =>
        {
            options.ContainerName = "validate-failure-container";
            options.ContainerInitializationMode = SnapshotBlobContainerInitializationMode.ValidateExists;
        });
        services.AddSingleton<IBlobContainerInitializerOperations>(operations);
        services.AddSingleton<ILogger<BlobContainerInitializer>>(logger);

        await using ServiceProvider provider = services.BuildServiceProvider();

        IHostedService hostedService = provider.GetRequiredService<IHostedService>();
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => hostedService.StartAsync(CancellationToken.None));
        TestLogEntry initializationLog = Assert.Single(logger.Entries, entry => entry.EventId.Id == 2414);

        Assert.Contains("validate-failure-container", exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(SnapshotBlobContainerInitializationMode.ValidateExists), exception.Message, StringComparison.Ordinal);
        Assert.Contains("configured container name is correct", exception.Message, StringComparison.Ordinal);
        Assert.Same(underlyingException, exception.InnerException);
        Assert.Equal(LogLevel.Error, initializationLog.Level);
        Assert.Equal("validate-failure-container", Assert.IsType<string>(initializationLog.State["containerName"]));
        Assert.Equal(
            SnapshotBlobContainerInitializationMode.ValidateExists,
            Assert.IsType<SnapshotBlobContainerInitializationMode>(initializationLog.State["initializationMode"]));
        Assert.Same(underlyingException, initializationLog.Exception);
    }
}
