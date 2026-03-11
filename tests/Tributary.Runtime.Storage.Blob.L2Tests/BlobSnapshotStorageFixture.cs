using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Aspire.Hosting.Testing;

using Azure.Storage.Blobs;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Mississippi.Tributary.Runtime.Storage.Abstractions;

using Projects;
using Xunit.Sdk;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L2Tests;

/// <summary>
///     xUnit fixture that starts the Azurite-backed AppHost used for Blob snapshot storage integration testing.
/// </summary>
#pragma warning disable CA1515
#pragma warning disable IDISP002
#pragma warning disable IDISP003
public sealed class BlobSnapshotStorageFixture : IAsyncLifetime, IDisposable
#pragma warning restore IDISP003
#pragma warning restore IDISP002
#pragma warning restore CA1515
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

#pragma warning disable IDISP002
    private DistributedApplication? app;
#pragma warning restore IDISP002

    private bool disposed;

    /// <summary>
    ///     Gets the initialization error if the Azurite-backed AppHost could not be started.
    /// </summary>
    public Exception? InitializationError { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether the Azurite-backed AppHost initialized successfully.
    /// </summary>
    public bool IsAvailable { get; private set; }

    /// <summary>
    ///     Gets the Blob connection string exposed by the Azurite AppHost resource.
    /// </summary>
    public string BlobConnectionString { get; private set; } = string.Empty;

    /// <summary>
    ///     Creates a Blob service client for direct verification against the emulator.
    /// </summary>
    /// <returns>A Blob service client connected to Azurite.</returns>
    public BlobServiceClient CreateBlobServiceClient() => new(BlobConnectionString);

    /// <summary>
    ///     Skips the current test when the Azurite-backed AppHost is unavailable in the current environment.
    /// </summary>
    public void EnsureAzuriteAvailable()
    {
        if (IsAvailable)
        {
            return;
        }

        throw SkipException.ForSkip(
            InitializationError is null
                ? "Azurite-backed Blob L2 tests require an Aspire DCP-capable environment."
                : $"Azurite-backed Blob L2 tests are unavailable in this environment: {InitializationError.Message}");
    }

    /// <summary>
    ///     Creates and starts a host configured with the Blob snapshot storage provider.
    /// </summary>
    /// <param name="containerName">The Blob container to target.</param>
    /// <param name="compressionEnabled">Whether gzip compression should be enabled.</param>
    /// <returns>A started host that exposes <see cref="ISnapshotStorageProvider" />.</returns>
    public async Task<IHost> CreateProviderHostAsync(
        string containerName,
        bool compressionEnabled
    )
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Services.AddLogging();
        builder.Services.AddKeyedSingleton(
            SnapshotBlobDefaults.BlobServiceClientServiceKey,
            (
                _,
                _
            ) => new BlobServiceClient(BlobConnectionString));
        builder.Services.AddBlobSnapshotStorageProvider(options =>
        {
            options.BlobServiceClientServiceKey = SnapshotBlobDefaults.BlobServiceClientServiceKey;
            options.ContainerName = containerName;
            options.CompressionEnabled = compressionEnabled;
        });
        IHost host = builder.Build();
        await host.StartAsync();
        return host;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (app is not null)
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    /// <inheritdoc />
#pragma warning disable IDISP001
    public async Task InitializeAsync()
    {
        if (app is not null)
        {
            return;
        }

        try
        {
            IDistributedApplicationTestingBuilder appHost =
                await DistributedApplicationTestingBuilder.CreateAsync<Tributary_Runtime_Storage_Blob_L2Tests_AppHost>();
#pragma warning restore IDISP001
            DistributedApplication builtApp = await appHost.BuildAsync().WaitAsync(DefaultTimeout);
#pragma warning disable IDISP003
            app = builtApp;
#pragma warning restore IDISP003
            await app.StartAsync().WaitAsync(DefaultTimeout);
            using CancellationTokenSource cancellationTokenSource = new(DefaultTimeout);
            await app.ResourceNotifications.WaitForResourceHealthyAsync("storage", cancellationTokenSource.Token)
                .WaitAsync(DefaultTimeout, cancellationTokenSource.Token);
            BlobConnectionString = await app.GetConnectionStringAsync("blobs", cancellationTokenSource.Token) ??
                                   throw new InvalidOperationException("Failed to get Blob storage connection string.");
            BlobConnectionString.Should().NotBeNullOrWhiteSpace();
            IsAvailable = true;
        }
        catch (HttpRequestException ex)
        {
            InitializationError = ex;
            IsAvailable = false;
        }
        catch (SocketException ex)
        {
            InitializationError = ex;
            IsAvailable = false;
        }
        catch (TimeoutException ex)
        {
            InitializationError = ex;
            IsAvailable = false;
        }
        catch (InvalidOperationException ex)
        {
            InitializationError = ex;
            IsAvailable = false;
        }
        catch (TaskCanceledException ex)
        {
            InitializationError = ex;
            IsAvailable = false;
        }
    }
}
