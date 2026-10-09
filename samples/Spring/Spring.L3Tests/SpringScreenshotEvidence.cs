using System.IO;
using System.Text.Json;


namespace MississippiSamples.Spring.L3Tests;

/// <summary>Retains screenshots and browser metadata alongside the canonical L3 results.</summary>
internal static class SpringScreenshotEvidence
{
    /// <summary>Saves the current, real browser state when the test runner requests artifacts.</summary>
    /// <param name="page">The page whose journey assertions precede the capture.</param>
    /// <param name="name">The fixed artifact name supplied by the test.</param>
    /// <returns>The asynchronous screenshot and metadata write.</returns>
    internal static async Task SaveAsync(
        IPage page,
        string name
    )
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string? artifacts = Environment.GetEnvironmentVariable("SPRING_TEST_ARTIFACTS");
        if (string.IsNullOrWhiteSpace(artifacts))
        {
            return;
        }

        string directory = Path.Join(artifacts, "flagship");
        Directory.CreateDirectory(directory);
        string screenshot = Path.Join(directory, name + ".png");
        await page.ScreenshotAsync(
            new()
            {
                Path = screenshot,
                FullPage = true,
                Timeout = 10_000,
            });
        string metadata = JsonSerializer.Serialize(
            new
            {
                Screenshot = name + ".png",
                Route = new Uri(page.Url).PathAndQuery,
                Viewport = page.ViewportSize,
                Theme = await page.Locator("html").GetAttributeAsync("data-rf-theme"),
                Browser = page.Context.Browser?.Version,
                ScreenshotWrittenUtc = File.GetLastWriteTimeUtc(screenshot),
            });
        await File.WriteAllTextAsync(
            Path.Join(directory, name + ".json"),
            metadata,
            TestContext.Current.CancellationToken);
    }
}