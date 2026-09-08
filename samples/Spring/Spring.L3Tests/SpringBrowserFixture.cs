using System.IO;

using MississippiSamples.Spring.TestHarness;


namespace MississippiSamples.Spring.L3Tests;

/// <summary>
///     Provides xUnit with an Aspire deployment and Chromium for Spring L3 browser journeys.
/// </summary>
public sealed class SpringBrowserFixture
    : IAsyncLifetime,
      IAsyncDisposable
{
    private IBrowser? browser;

    private bool disposed;

    private bool isInitializationStarted;

    private IPlaywright? playwright;

    /// <summary>Gets the gateway endpoint discovered by Aspire.</summary>
    public Uri GatewayBaseUri => Application.GatewayBaseUri;

    /// <summary>Gets the startup error, if initialization failed.</summary>
    public Exception? InitializationError { get; private set; }

    /// <summary>Gets a value indicating whether the application and browser are initialized.</summary>
    public bool IsInitialized => Application.IsInitialized && browser is not null;

    private SpringApplicationFixture Application { get; } = new();

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
                            Path = Path.Join(directory, "banking.png"),
                            FullPage = true,
                            Timeout = 10_000,
                        });
                }
                finally
                {
                    await page.Context.Tracing.StopAsync(
                        new()
                        {
                            Path = Path.Join(directory, "banking.zip"),
                        });
                }
            }
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    /// <summary>Creates an isolated browser page for a test.</summary>
    /// <returns>The new page, whose context is owned by the caller.</returns>
    public async Task<IPage> CreatePageAsync()
    {
        if (browser is null)
        {
            throw new InvalidOperationException("Browser not initialized.");
        }

        IPage page = await browser.NewPageAsync();
        page.SetDefaultTimeout(60_000);
        return page;
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
            if (browser is not null)
            {
                await browser.DisposeAsync();
            }
        }
        finally
        {
            try
            {
                playwright?.Dispose();
            }
            finally
            {
                await Application.DisposeAsync();
            }
        }
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (isInitializationStarted)
        {
            throw new InvalidOperationException("Browser initialization has already started.");
        }

        isInitializationStarted = true;
        try
        {
            await Application.InitializeAsync();
            playwright ??= await Playwright.CreateAsync();
            browser ??= await playwright.Chromium.LaunchAsync(
                new()
                {
                    Headless = true,
                });
        }
        catch (Exception ex)
        {
            InitializationError = ex;
            try
            {
                await DisposeAsync();
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException("Spring browser startup and cleanup both failed.", ex, cleanupException);
            }

            throw;
        }
    }

    /// <inheritdoc />
    ValueTask IAsyncDisposable.DisposeAsync() => new(DisposeAsync());
}