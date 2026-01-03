using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

using Projects;


namespace Cascade.L2Tests;

/// <summary>
///     xUnit fixture that starts the Aspire AppHost and Playwright browser for E2E tests.
/// </summary>
#pragma warning disable CA1515 // Types can be made internal - xUnit fixture must be public
#pragma warning disable IDISP002 // Dispose member - disposed in DisposeAsync
#pragma warning disable IDISP003 // Dispose previous before re-assigning - fields are null initially
public sealed class PlaywrightFixture
    : IAsyncLifetime,
      IDisposable
#pragma warning restore CA1515
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(180);

    private DistributedApplication? app;

    private bool disposed;

    private IPlaywright? playwright;

    /// <summary>
    ///     Gets the base URL of the Cascade.Server resource.
    /// </summary>
#pragma warning disable CA1056 // URI properties should not be strings - simpler for test usage
    public string BaseUrl { get; private set; } = string.Empty;
#pragma warning restore CA1056

    /// <summary>
    ///     Gets the Playwright browser instance.
    /// </summary>
    public IBrowser Browser { get; private set; } = null!;

    /// <summary>
    ///     Creates a new Playwright page in a fresh browser context.
    /// </summary>
    /// <returns>A new page instance.</returns>
    public async Task<IPage> CreatePageAsync()
    {
        IBrowserContext context = await Browser.NewContextAsync(
            new()
            {
                IgnoreHTTPSErrors = true,
            });
        return await context.NewPageAsync();
    }

    /// <summary>
    ///     Disposes the fixture synchronously.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        playwright?.Dispose();
        disposed = true;
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.DisposeAsync();
        }

        playwright?.Dispose();
        if (app is not null)
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    /// <inheritdoc />
#pragma warning disable IDISP001 // Dispose created - appHost not disposed directly, BuildAsync returns different app instance that is disposed
    public async Task InitializeAsync()
    {
        // Start Aspire AppHost
        IDistributedApplicationTestingBuilder appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Cascade_AppHost>();
#pragma warning restore IDISP001
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddFilter("Aspire.Hosting.Dcp", LogLevel.Warning);
            logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
        });
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        DistributedApplication builtApp = await appHost.BuildAsync().WaitAsync(DefaultTimeout);
        app = builtApp;
        await app.StartAsync().WaitAsync(DefaultTimeout);

        // Wait for container resources to be healthy first (cosmos emulator takes ~30+ seconds)
        using CancellationTokenSource cts = new(DefaultTimeout);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("cosmos", cts.Token);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("storage", cts.Token);

        // Wait for the cascade-server resource to be healthy
        await app.ResourceNotifications.WaitForResourceHealthyAsync("cascade-server", cts.Token);

        // Get the Cascade.Server URL (try https first, fall back to http)
        Uri? resourceUrl;
        try
        {
            resourceUrl = app.GetEndpoint("cascade-server", "https");
        }
        catch (InvalidOperationException)
        {
            resourceUrl = app.GetEndpoint("cascade-server", "http");
        }

        BaseUrl = resourceUrl.ToString().TrimEnd('/');

        // Initialize Playwright
        IPlaywright createdPlaywright = await Playwright.CreateAsync();
        playwright = createdPlaywright;
        Browser = await playwright.Chromium.LaunchAsync(
            new()
            {
                Headless = true,
            });
    }
}