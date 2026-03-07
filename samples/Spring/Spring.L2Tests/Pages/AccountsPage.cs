using System.Text.RegularExpressions;


namespace Spring.L2Tests.Pages;

/// <summary>
///     Page Object Model for the Spring sample Accounts page (Demo Account Setup).
///     Encapsulates Playwright interactions with the demo accounts initialization UI.
/// </summary>
public sealed class AccountsPage
{
    private readonly IPage page;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AccountsPage" /> class.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    public AccountsPage(
        IPage page
    ) =>
        this.page = page;

    /// <summary>
    ///     Navigates to the operations page via the link.
    /// </summary>
    /// <returns>A task representing the navigation.</returns>
    public async Task ClickGoToOperationsAsync() =>
        await page.GetByRole(
                AriaRole.Link,
                new()
                {
                    Name = "Go to Operations",
                })
            .ClickAsync();

    /// <summary>
    ///     Clicks the Initialize Demo Accounts button.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task ClickInitializeDemoAccountsAsync() =>
        await page.GetByRole(
                AriaRole.Button,
                new()
                {
                    Name = "Initialize demo accounts",
                })
            .ClickAsync();

    /// <summary>
    ///     Navigates to the accounts page.
    /// </summary>
    /// <param name="baseUri">The base URI of the application.</param>
    /// <returns>A task representing the navigation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="baseUri" /> is null.</exception>
    public async Task NavigateAsync(
        Uri baseUri
    )
    {
        ArgumentNullException.ThrowIfNull(baseUri);
        Uri accountsUri = new(baseUri, "accounts");
        await page.GotoAsync(
            accountsUri.ToString(),
            new()
            {
                WaitUntil = WaitUntilState.NetworkIdle,
            });
    }

    /// <summary>
    ///     Waits for the SignalR connection status to reach the expected value.
    /// </summary>
    /// <param name="expectedStatus">The expected connection status text (e.g., "Connected").</param>
    /// <param name="timeout">Optional timeout in milliseconds.</param>
    /// <returns>A task representing the wait operation.</returns>
    public async Task WaitForConnectionStatusAsync(
        string expectedStatus,
        float? timeout = null
    ) =>
        await page.Locator("button[aria-label='Connection status']")
            .Filter(
                new()
                {
                    HasTextRegex = new($"^{Regex.Escape(expectedStatus)}$"),
                })
            .WaitForAsync(
                new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeout,
                });

    /// <summary>
    ///     Waits for the demo accounts to be initialized (shows the account details).
    /// </summary>
    /// <param name="timeout">Optional timeout in milliseconds.</param>
    /// <returns>A task representing the wait operation.</returns>
    public async Task WaitForDemoAccountsInitializedAsync(
        float? timeout = null
    ) =>
        await page.GetByRole(
                AriaRole.Link,
                new()
                {
                    Name = "Go to Operations",
                })
            .WaitForAsync(
                new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeout,
                });
}