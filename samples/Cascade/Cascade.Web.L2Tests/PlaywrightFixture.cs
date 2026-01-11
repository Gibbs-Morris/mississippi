using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

using Projects;


namespace Cascade.Web.L2Tests;

/// <summary>
///     xUnit fixture that starts the Aspire AppHost and Playwright browser for E2E tests.
/// </summary>
public sealed class PlaywrightFixture
    : IAsyncLifetime,
      IDisposable
{
    // 5 minutes is sufficient buffer for emulator initialization
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    private DistributedApplication? app;

    private bool disposed;

    private IPlaywright? playwright;

    /// <summary>
    ///     Gets the base URL of the Cascade.Web.Server resource.
    /// </summary>
    public string BaseUrl { get; private set; } = string.Empty;

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
    public async Task InitializeAsync()
    {
        // Start Aspire AppHost
        IDistributedApplicationTestingBuilder appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Cascade_Web_AppHost>();
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
        app = await appHost.BuildAsync().WaitAsync(DefaultTimeout);
        await app.StartAsync().WaitAsync(DefaultTimeout);

        // Wait for container resources to be healthy first (cosmos emulator takes ~30+ seconds)
        using CancellationTokenSource cts = new(DefaultTimeout);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("cosmos", cts.Token);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("storage", cts.Token);

        // Wait for the cascade-web-server resource to be healthy
        await app.ResourceNotifications.WaitForResourceHealthyAsync("cascade-web-server", cts.Token);

        // Get the Cascade.Web.Server URL (try https first, fall back to http)
        Uri? resourceUrl;
        try
        {
            resourceUrl = app.GetEndpoint("cascade-web-server", "https");
        }
        catch (InvalidOperationException)
        {
            resourceUrl = app.GetEndpoint("cascade-web-server", "http");
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
