using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;

using Projects;


namespace MississippiSamples.Spring.TestHarness;

/// <summary>
///     Shared xUnit fixture that starts the Spring AppHost and its emulators without a browser.
///     This fixture is shared across all tests in the collection to minimize startup overhead.
/// </summary>
/// <remarks>
///     <para>
///         This class implements both <see cref="IAsyncLifetime" /> and <see cref="IDisposable" />.
///         <see cref="IAsyncLifetime.DisposeAsync" /> is the primary cleanup path used by xUnit.
///         The synchronous <see cref="IDisposable.Dispose" /> method exists as a fallback for edge cases
///         where async disposal isn't invoked (e.g., finalizer scenarios or alternative test runners).
///         Both methods share the same cleanup logic and are guarded against double-disposal.
///     </para>
/// </remarks>
#pragma warning disable IDISP002 // Dispose member - disposed in DisposeAsync
#pragma warning disable IDISP003 // Dispose previous before re-assigning - fields are null initially
public sealed class SpringApplicationFixture
    : IAsyncLifetime,
      IDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(3);

    private DistributedApplication? app;

    private IDistributedApplicationTestingBuilder? appBuilder;

    private bool disposed;

    private HttpClient? gatewayHttpClient;

    /// <summary>
    ///     Gets the base URI for the Spring gateway application.
    /// </summary>
    public Uri GatewayBaseUri { get; private set; } = new("about:blank");

    /// <summary>
    ///     Gets the fixture-owned <see cref="HttpClient" /> configured to communicate with the Spring gateway.
    ///     The client targets the plain HTTP endpoint obtained via <c>app.GetEndpoint("spring-gateway", "http")</c>.
    ///     No certificate validation bypass is required because the Spring gateway does not configure
    ///     HTTPS redirection middleware, so HTTP requests are served directly without TLS.
    ///     Callers must not dispose the returned client — its lifetime is managed by the fixture.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the app is not initialized.</exception>
    public HttpClient GatewayClient =>
        gatewayHttpClient ?? throw new InvalidOperationException("Application not initialized.");

    /// <summary>
    ///     Gets the initialization error if the fixture failed to start.
    /// </summary>
    public Exception? InitializationError { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether the fixture initialized successfully.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        gatewayHttpClient?.Dispose();
        if (app is not null)
        {
            app.Dispose();
        }
        else
        {
            appBuilder?.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        try
        {
            await SaveResourceLogsAsync();
        }
        finally
        {
            gatewayHttpClient?.Dispose();
            if (app is not null)
            {
                await app.DisposeAsync();
            }
            else if (appBuilder is not null)
            {
                await appBuilder.DisposeAsync();
            }
        }
    }

    /// <inheritdoc />
#pragma warning disable IDISP001 // Dispose created - appHost implements builder pattern
    public async Task InitializeAsync()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (appBuilder is not null)
        {
            throw new InvalidOperationException("Application already initialized.");
        }

        try
        {
            // One cancellation budget covers creation, startup, and resource readiness.
            using CancellationTokenSource cts = new(DefaultTimeout);
            IDistributedApplicationTestingBuilder builder =
                await DistributedApplicationTestingBuilder.CreateAsync<Spring_AppHost>(
                    ["Spring:AuthProofMode=true"],
                    cts.Token);

            // The builder owns the app; keep it alive until collection teardown.
            appBuilder = builder;
            builder.Services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddFilter("Orleans", LogLevel.Warning);
                logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
            });

            // Build and start the application (following official docs pattern)
            DistributedApplication builtApp = await builder.BuildAsync(cts.Token);
            app = builtApp;
            await app.StartAsync(cts.Token);

            // Wait for Azure Storage emulator (Azurite)
            await app.ResourceNotifications.WaitForResourceHealthyAsync("storage", cts.Token)
                .WaitAsync(DefaultTimeout, cts.Token);

            // Wait for the gateway to be running
            await app.ResourceNotifications.WaitForResourceHealthyAsync("spring-gateway", cts.Token)
                .WaitAsync(DefaultTimeout, cts.Token);

            // Get the gateway HTTP endpoint (returns Uri directly)
            GatewayBaseUri = app.GetEndpoint("spring-gateway", "http");
            gatewayHttpClient = new()
            {
                BaseAddress = GatewayBaseUri,
            };
            IsInitialized = true;
        }
        catch (Exception ex)
        {
            InitializationError = ex;
            IsInitialized = false;
            try
            {
                await DisposeAsync();
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException("Spring startup and cleanup both failed.", ex, cleanupException);
            }

            // Re-throw to fail the test fixture, but keep the error captured for diagnostics
            throw;
        }
    }
#pragma warning restore IDISP001

    private async Task SaveResourceLogsAsync()
    {
        string? directory = Environment.GetEnvironmentVariable("SPRING_TEST_ARTIFACTS");
        if (app is null || appBuilder is null || string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);
        ResourceLoggerService logs = app.Services.GetRequiredService<ResourceLoggerService>();
        foreach (IResource resource in appBuilder.Resources.Where(resource =>
                     resource.Name is "storage" or "cosmos" or "spring-runtime" or "spring-gateway"))
        {
            await foreach (IReadOnlyList<LogLine> batch in logs.GetAllAsync(resource))
            {
                await File.AppendAllLinesAsync(
                    Path.Join(directory, $"{resource.Name}.log"),
                    batch.Select(line => line.Content));
            }
        }
    }
}
#pragma warning restore IDISP002
#pragma warning restore IDISP003