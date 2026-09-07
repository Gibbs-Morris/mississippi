using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;

using Projects;


namespace MississippiSamples.Spring.L2Tests;

/// <summary>
///     xUnit fixture that starts the Spring AppHost with Azurite emulator and Playwright browser.
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
public sealed class SpringFixture
    : IAsyncLifetime,
      IDisposable
{
    /// <summary>
    ///     Default timeout for Playwright operations (in milliseconds).
    ///     Set to 1 minute which is generous for page operations while still failing fast.
    /// </summary>
    private const float PlaywrightTimeoutMs = 60_000;

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(3);

    private DistributedApplication? app;

    private IDistributedApplicationTestingBuilder? appBuilder;

    private IBrowser? browser;

    private bool disposed;

    private HttpClient? gatewayHttpClient;

    private IPlaywright? playwright;

    /// <summary>
    ///     Gets the base URI for the Spring gateway application.
    /// </summary>
    public Uri GatewayBaseUri { get; private set; } = new("about:blank");

    /// <summary>
    ///     Gets the initialization error if the fixture failed to start.
    /// </summary>
    public Exception? InitializationError { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether the fixture initialized successfully.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    ///     Saves the banking smoke trace and screenshot when an artifact directory is configured.
    /// </summary>
    /// <param name="page">The page with an active Playwright trace.</param>
    /// <returns>A task representing artifact capture and browser context cleanup.</returns>
    public static async Task SaveBrowserArtifactsAsync(
        IPage page
    )
    {
        ArgumentNullException.ThrowIfNull(page);
        string? directory = Environment.GetEnvironmentVariable("SPRING_TEST_ARTIFACTS");
        try
        {
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
                try
                {
                    await page.ScreenshotAsync(
                        new()
                        {
                            Path = Path.Combine(directory, "banking.png"),
                            FullPage = true,
                            Timeout = 10_000,
                        });
                }
                finally
                {
                    await page.Context.Tracing.StopAsync(
                        new()
                        {
                            Path = Path.Combine(directory, "banking.zip"),
                        });
                }
            }
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    /// <summary>
    ///     Returns the fixture-owned <see cref="HttpClient" /> configured to communicate with the Spring gateway.
    ///     The client targets the plain HTTP endpoint obtained via <c>app.GetEndpoint("spring-gateway", "http")</c>.
    ///     No certificate validation bypass is required because the Spring gateway does not configure
    ///     HTTPS redirection middleware, so HTTP requests are served directly without TLS.
    ///     Callers must not dispose the returned client — its lifetime is managed by the fixture.
    /// </summary>
    /// <returns>The shared <see cref="HttpClient" /> configured for the Spring gateway.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the app is not initialized.</exception>
    public HttpClient CreateHttpClient()
    {
        if (gatewayHttpClient is null)
        {
            throw new InvalidOperationException("Application not initialized.");
        }

        return gatewayHttpClient;
    }

    /// <summary>
    ///     Creates a new browser page for testing.
    /// </summary>
    /// <returns>A new browser page.</returns>
    public async Task<IPage> CreatePageAsync()
    {
        if (browser is null)
        {
            throw new InvalidOperationException("Browser not initialized.");
        }

        IPage page = await browser.NewPageAsync();
        page.SetDefaultTimeout(PlaywrightTimeoutMs);
        return page;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
#pragma warning disable VSTHRD002 // Synchronous waiting is acceptable in Dispose for resource cleanup
        browser?.DisposeAsync().AsTask().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        gatewayHttpClient?.Dispose();
        playwright?.Dispose();
        appBuilder?.Dispose();
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
            try
            {
                if (browser is not null)
                {
                    await browser.DisposeAsync();
                }
            }
            finally
            {
                gatewayHttpClient?.Dispose();
                playwright?.Dispose();
                if (appBuilder is not null)
                {
                    await appBuilder.DisposeAsync();
                }
            }
        }
    }

    /// <inheritdoc />
#pragma warning disable IDISP001 // Dispose created - appHost implements builder pattern
    public async Task InitializeAsync()
    {
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

            // Initialize Playwright
            playwright = await Playwright.CreateAsync();
            browser = await playwright.Chromium.LaunchAsync(
                new()
                {
                    Headless = true,
                });
            IsInitialized = true;
        }
        catch (Exception ex)
        {
            InitializationError = ex;
            IsInitialized = false;
            await DisposeAsync();

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
                    Path.Combine(directory, $"{resource.Name}.log"),
                    batch.Select(line => line.Content));
            }
        }
    }
}
#pragma warning restore IDISP002
#pragma warning restore IDISP003
