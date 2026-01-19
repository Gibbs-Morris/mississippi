using System.Threading;

using Projects;


namespace Spring.L2Tests;

/// <summary>
///     xUnit fixture that starts the Spring AppHost with Azurite emulator and Playwright browser.
///     This fixture is shared across all tests in the collection to minimize startup overhead.
/// </summary>
#pragma warning disable CA1515 // Types can be made internal - xUnit fixture must be public
#pragma warning disable IDISP002 // Dispose member - disposed in DisposeAsync
#pragma warning disable IDISP003 // Dispose previous before re-assigning - fields are null initially
public sealed class SpringFixture
    : IAsyncLifetime,
      IDisposable
#pragma warning restore CA1515
{
    /// <summary>
    ///     Timeout to accommodate Azurite emulator startup.
    /// </summary>
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    private DistributedApplication? app;

    private IBrowser? browser;

    private bool disposed;

    private IPlaywright? playwright;

    /// <summary>
    ///     Gets the initialization error if the fixture failed to start.
    /// </summary>
    public Exception? InitializationError { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether the fixture initialized successfully.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    ///     Gets the base URI for the Spring.Server application.
    /// </summary>
    public Uri ServerBaseUri { get; private set; } = new("about:blank");

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
        playwright?.Dispose();
        app?.Dispose();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (browser is not null)
        {
            await browser.DisposeAsync();
        }

        playwright?.Dispose();
        if (app is not null)
        {
            await app.DisposeAsync();
        }
    }

    /// <inheritdoc />
#pragma warning disable IDISP001 // Dispose created - appHost implements builder pattern
    public async Task InitializeAsync()
    {
        try
        {
            // Start the Spring AppHost with Azurite emulator
            IDistributedApplicationTestingBuilder builder =
                await DistributedApplicationTestingBuilder.CreateAsync<Spring_AppHost>();
            builder.Services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddFilter("Orleans", LogLevel.Warning);
                logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
            });

            // Build and start the application (following official docs pattern)
            DistributedApplication builtApp = await builder.BuildAsync().WaitAsync(DefaultTimeout);
            app = builtApp;
            await app.StartAsync().WaitAsync(DefaultTimeout);

            // Wait for container resources to be healthy (following official docs pattern)
            using CancellationTokenSource cts = new(DefaultTimeout);

            // Wait for Azure Storage emulator (Azurite)
            await app.ResourceNotifications.WaitForResourceHealthyAsync("storage", cts.Token)
                .WaitAsync(DefaultTimeout, cts.Token);

            // Wait for the server to be running
            await app.ResourceNotifications.WaitForResourceHealthyAsync("spring-server", cts.Token)
                .WaitAsync(DefaultTimeout, cts.Token);

            // Get the server HTTP endpoint (returns Uri directly)
            ServerBaseUri = app.GetEndpoint("spring-server", "http");

            // Initialize Playwright
            playwright = await Playwright.CreateAsync();
            browser = await playwright.Chromium.LaunchAsync(
                new()
                {
                    Headless = true,
                });
            IsInitialized = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            InitializationError = ex;
            IsInitialized = false;

            // Re-throw to fail the test fixture, but keep the error captured for diagnostics
            throw;
        }
    }
#pragma warning restore IDISP001
}
#pragma warning restore IDISP002
#pragma warning restore IDISP003