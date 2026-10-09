using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;

using MississippiSamples.Spring.L3Tests.Pages;


namespace MississippiSamples.Spring.L3Tests;

/// <summary>Verifies command identifier preservation through the real browser and gateway.</summary>
[Collection(SpringBrowserCollectionDefinition.Name)]
public sealed class CommandEntityUrlTests
{
    /// <summary>Initializes a new instance of the <see cref="CommandEntityUrlTests" /> class.</summary>
    /// <param name="fixture">The shared browser deployment.</param>
    public CommandEntityUrlTests(
        SpringBrowserFixture fixture
    ) =>
        Fixture = fixture;

    private SpringBrowserFixture Fixture { get; }

    private static async Task SaveScreenshotsAsync(
        IPage page,
        string state,
        string entityId
    )
    {
        string? directory = Environment.GetEnvironmentVariable("SPRING_TEST_ARTIFACTS");
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);
        foreach ((string name, int width, int height) in new[] { ("desktop", 1440, 900), ("mobile", 390, 844) })
        {
            await page.SetViewportSizeAsync(width, height);
            await page.ScreenshotAsync(
                new()
                {
                    Path = Path.Join(directory, $"command-entity-{state}-{name}.png"),
                    FullPage = true,
                });
        }

        await File.WriteAllTextAsync(
            Path.Join(directory, $"command-entity-{state}.json"),
            JsonSerializer.Serialize(
                new
                {
                    Route = page.Url,
                    State = state,
                    EntityId = entityId,
                    Browser = page.Context.Browser?.Version,
                    Viewports = new[]
                    {
                        new
                        {
                            Width = 1440,
                            Height = 900,
                        },
                        new
                        {
                            Width = 390,
                            Height = 844,
                        },
                    },
                }),
            TestContext.Current.CancellationToken);
    }

    /// <summary>Preserves supported identifiers through WebAssembly, HTTP, storage and projection updates.</summary>
    /// <param name="suffix">Reserved characters in the identifier.</param>
    /// <param name="state">The screenshot state name.</param>
    /// <returns>The asynchronous browser test.</returns>
    [Theory]
    [InlineData("#?region=42+客户", "reserved-success")]
    [InlineData("%2F42", "percent-success")]
    public async Task SupportedEntityIdTargetsOriginalAccount(
        string suffix,
        string state
    )
    {
        Assert.True(Fixture.IsInitialized);
        string entityId = $"command-{Guid.NewGuid():N}{suffix}";
        IPage page = await Fixture.CreatePageAsync();
        try
        {
            await page.GotoAsync(
                new Uri(
                    Fixture.GatewayBaseUri,
                    $"operations?a={Uri.EscapeDataString(entityId)}&b=control-{Guid.NewGuid():N}").ToString());
            OperationsPage operations = new(page);
            await operations.WaitForConnectionStatusAsync("Connected");
            await operations.EnterHolderNameAsync("URL identity proof");
            await operations.EnterInitialDepositAsync(10m);
            string expectedOpenPath = $"/api/aggregates/bank-account/{Uri.EscapeDataString(entityId)}/open";
            Task<IResponse> openResponse = page.WaitForResponseAsync(response =>
                (response.Request.Method == "POST") && (new Uri(response.Url).AbsolutePath == expectedOpenPath));
            await operations.ClickOpenAccountAsync();
            Assert.Equal(200, (await openResponse).Status);
            await operations.WaitForBalanceValueAsync("10.00");
            Assert.Equal("URL identity proof", await operations.GetHolderNameTextAsync());
            await operations.EnterDepositAmountAsync(12.34m);
            string expectedDepositPath = $"/api/aggregates/bank-account/{Uri.EscapeDataString(entityId)}/deposit";
            Task<IResponse> depositResponse = page.WaitForResponseAsync(response =>
                (response.Request.Method == "POST") && (new Uri(response.Url).AbsolutePath == expectedDepositPath));
            await operations.ClickDepositAsync();
            Assert.Equal(200, (await depositResponse).Status);
            await operations.WaitForCommandSuccessAsync();
            await operations.WaitForBalanceValueAsync("22.34");
            await page.ReloadAsync();
            await operations.WaitForConnectionStatusAsync("Connected");
            await operations.WaitForBalanceValueAsync("22.34");
            Assert.Equal("URL identity proof", await operations.GetHolderNameTextAsync());
            await SaveScreenshotsAsync(page, state, entityId);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    /// <summary>Rejects ambiguous identifiers without a command request or a stuck executing state.</summary>
    /// <param name="entityId">The unsupported identifier.</param>
    /// <param name="state">The screenshot state name.</param>
    /// <returns>The asynchronous browser test.</returns>
    [Theory]
    [InlineData("customer/42", "slash-rejected")]
    [InlineData(".", "dot-rejected")]
    [InlineData("..", "parent-dot-rejected")]
    public async Task UnsupportedEntityIdFailsBeforeHttpDispatch(
        string entityId,
        string state
    )
    {
        Assert.True(Fixture.IsInitialized);
        IPage page = await Fixture.CreatePageAsync();
        ConcurrentQueue<string> commandRequests = new();
        page.Request += (_, request) =>
        {
            if ((request.Method == "POST") &&
                new Uri(request.Url).AbsolutePath.StartsWith("/api/aggregates/", StringComparison.Ordinal))
            {
                commandRequests.Enqueue(request.Url);
            }
        };
        try
        {
            await page.GotoAsync(
                new Uri(
                    Fixture.GatewayBaseUri,
                    $"operations?a={Uri.EscapeDataString(entityId)}&b=control-{Guid.NewGuid():N}").ToString());
            OperationsPage operations = new(page);
            await operations.WaitForConnectionStatusAsync("Connected");
            await operations.EnterHolderNameAsync("Rejected identity");
            await operations.EnterInitialDepositAsync(10m);
            await operations.ClickOpenAccountAsync();
            ILocator panel = page.Locator("#account-a-operations-panel");
            await page.GetByRole(
                    AriaRole.Region,
                    new()
                    {
                        Name = "Banking responses · this browser",
                        Exact = true,
                    })
                .GetByRole(AriaRole.Alert)
                .Filter(
                    new()
                    {
                        HasText = "The entity ID cannot contain '/' or be '.' or '..'",
                    })
                .WaitForAsync();
            Assert.True(
                await panel.GetByRole(
                        AriaRole.Button,
                        new()
                        {
                            Name = "Open Account",
                            Exact = true,
                        })
                    .IsEnabledAsync());
            Assert.Empty(commandRequests);
            await SaveScreenshotsAsync(page, state, entityId);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}