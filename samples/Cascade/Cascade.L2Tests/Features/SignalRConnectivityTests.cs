using Microsoft.Playwright;


namespace Cascade.L2Tests.Features;

/// <summary>
///     Tests for SignalR messaging from the Blazor WASM client.
/// </summary>
#pragma warning disable CA1515 // Types can be made internal - xUnit test class must be public
public sealed class SignalRConnectivityTests : TestBase
#pragma warning restore CA1515
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SignalRConnectivityTests" /> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public SignalRConnectivityTests(
        PlaywrightFixture fixture
    )
        : base(fixture)
    {
    }

    /// <summary>
    ///     Verifies SignalR connection can be established.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task SignalRConnectionIsEstablished()
    {
        // Arrange
        IPage page = await CreatePageAsync();

        // Act
        await page.GotoAsync(
            Fixture.BaseUrl,
            new()
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 60000,
            });

        // Wait for SignalR to connect
        await page.WaitForSelectorAsync(
            "text=Connected",
            new()
            {
                Timeout = 30000,
            });

        // Assert
        ILocator status = page.Locator("text=Connected");
        await Assertions.Expect(status).ToBeVisibleAsync();
    }

    /// <summary>
    ///     Verifies messages can be sent via SignalR and appear in the message list.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task SignalRSendMessageAppearsInList()
    {
        // Arrange
        IPage page = await CreatePageAsync();
        await page.GotoAsync(
            Fixture.BaseUrl,
            new()
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 60000,
            });

        // Wait for SignalR to connect
        await page.WaitForSelectorAsync(
            "text=Connected",
            new()
            {
                Timeout = 30000,
            });

        // Act
        string testMessage = $"Test message {Guid.NewGuid():N}";
        await page.FillAsync("input[placeholder='Enter message']", testMessage);
        await page.ClickAsync("button:has-text('Send')");

        // Wait for message to appear
        await page.WaitForSelectorAsync(
            $"text={testMessage}",
            new()
            {
                Timeout = 10000,
            });

        // Assert
        ILocator message = page.Locator($"li:has-text('{testMessage}')");
        await Assertions.Expect(message).ToBeVisibleAsync();
    }
}