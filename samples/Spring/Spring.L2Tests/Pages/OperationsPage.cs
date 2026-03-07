using System.Globalization;
using System.Text.RegularExpressions;


namespace Spring.L2Tests.Pages;

/// <summary>
///     Page Object Model for the Spring sample Operations page (Bank Account Operations).
///     Encapsulates Playwright interactions with the dual-account operations UI.
/// </summary>
public sealed partial class OperationsPage
{
    private readonly IPage page;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OperationsPage" /> class.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    public OperationsPage(
        IPage page
    ) =>
        this.page = page;

    /// <summary>
    ///     Gets the first account panel locator (Account A).
    /// </summary>
    private ILocator AccountAPanel => page.Locator("div.operations-panel").First;

    /// <summary>
    ///     Regex pattern to match the "Deposit £" button text exactly (not quick deposit buttons).
    /// </summary>
    [GeneratedRegex("^Deposit £$")]
    private static partial Regex DepositButtonPattern();

    /// <summary>
    ///     Regex pattern to match the "Withdraw" button text exactly (not quick withdraw buttons).
    /// </summary>
    [GeneratedRegex("^Withdraw$")]
    private static partial Regex WithdrawButtonPattern();

    /// <summary>
    ///     Clicks the Deposit button in the first account panel.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task ClickDepositAsync() =>
        await AccountAPanel.GetByRole(
                AriaRole.Button,
                new()
                {
                    NameRegex = DepositButtonPattern(),
                })
            .ClickAsync();

    /// <summary>
    ///     Clicks the Open Account button in the first account panel.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task ClickOpenAccountAsync() =>
        await AccountAPanel.GetByRole(
                AriaRole.Button,
                new()
                {
                    Name = "Open Account",
                    Exact = true,
                })
            .ClickAsync();

    /// <summary>
    ///     Clicks the Withdraw button in the first account panel.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task ClickWithdrawAsync() =>
        await AccountAPanel.GetByRole(
                AriaRole.Button,
                new()
                {
                    NameRegex = WithdrawButtonPattern(),
                })
            .ClickAsync();

    /// <summary>
    ///     Enters the deposit amount in the first account panel.
    /// </summary>
    /// <param name="amount">The amount to deposit.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task EnterDepositAmountAsync(
        decimal amount
    ) =>
        await AccountAPanel.Locator("#deposit-amount-input").FillAsync(amount.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    ///     Enters the holder name for opening an account in the first account panel.
    /// </summary>
    /// <param name="holderName">The holder name to enter.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task EnterHolderNameAsync(
        string holderName
    ) =>
        await AccountAPanel.Locator("#holder-name-input").FillAsync(holderName);

    /// <summary>
    ///     Enters the initial deposit amount for opening an account in the first account panel.
    /// </summary>
    /// <param name="amount">The initial deposit amount.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task EnterInitialDepositAsync(
        decimal amount
    ) =>
        await AccountAPanel.Locator("#initial-deposit-input").FillAsync(amount.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    ///     Enters the withdraw amount in the first account panel.
    /// </summary>
    /// <param name="amount">The amount to withdraw.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task EnterWithdrawAmountAsync(
        decimal amount
    ) =>
        await AccountAPanel.Locator("#withdraw-amount-input").FillAsync(amount.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    ///     Gets the displayed account header from the first account panel.
    /// </summary>
    /// <returns>The account ID text, or null if not present.</returns>
    public async Task<string?> GetAccountHeaderAsync()
    {
        // The account header shows "Account A: {id}" in a span within the panel
        ILocator accountHeader = AccountAPanel.Locator("> div > span").First;
        if (await accountHeader.CountAsync() > 0)
        {
            return await accountHeader.TextContentAsync();
        }

        return null;
    }

    /// <summary>
    ///     Gets the displayed balance from the first account panel's projection.
    /// </summary>
    /// <returns>The balance text (e.g., "£100.00"), or null if not present.</returns>
    public async Task<string?> GetBalanceTextAsync()
    {
        // Balance is shown as "Balance:" label followed by the value span
        ILocator balanceSection = AccountAPanel.Locator("section:has(h2:text-is('Account Status'))");
        ILocator balanceValue = balanceSection.Locator("div:has(span:text-is('Balance:')) > span").Last;
        if (await balanceValue.CountAsync() > 0)
        {
            return await balanceValue.TextContentAsync();
        }

        return null;
    }

    /// <summary>
    ///     Gets the displayed holder name from the first account panel's projection.
    /// </summary>
    /// <returns>The holder name text, or null if not present.</returns>
    public async Task<string?> GetHolderNameTextAsync()
    {
        // Holder is shown as "Holder:" label followed by the value span
        ILocator holderSection = AccountAPanel.Locator("section:has(h2:text-is('Account Status'))");
        ILocator holderValue = holderSection.Locator("div:has(span:text-is('Holder:')) > span").Last;
        if (await holderValue.CountAsync() > 0)
        {
            return await holderValue.TextContentAsync();
        }

        return null;
    }

    /// <summary>
    ///     Gets the displayed status from the first account panel's projection.
    /// </summary>
    /// <returns>The status text (e.g., "Open"), or null if not present.</returns>
    public async Task<string?> GetStatusTextAsync()
    {
        // Status is shown as "Status:" label followed by the value span
        ILocator statusSection = AccountAPanel.Locator("section:has(h2:text-is('Account Status'))");
        ILocator statusValue = statusSection.Locator("div:has(span:text-is('Status:')) > span").Last;
        if (await statusValue.CountAsync() > 0)
        {
            return await statusValue.TextContentAsync();
        }

        return null;
    }

    /// <summary>
    ///     Gets the page title (h1 element).
    /// </summary>
    /// <returns>The page title.</returns>
    public async Task<string?> GetTitleAsync() => await page.Locator("h1").TextContentAsync();

    /// <summary>
    ///     Waits for the balance projection to appear in the first account panel.
    /// </summary>
    /// <param name="timeout">Optional timeout in milliseconds.</param>
    /// <returns>A task representing the wait operation.</returns>
    public async Task WaitForBalanceAsync(
        float? timeout = null
    ) =>
        await AccountAPanel
            .Locator("section:has(h2:text-is('Account Status')) div > div:has(> span:text-is('Balance:'))")
            .WaitForAsync(
                new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeout,
                });

    /// <summary>
    ///     Waits for the balance projection to show a specific value in the first account panel.
    /// </summary>
    /// <param name="expectedBalance">
    ///     The expected balance value (e.g., "100.00"). Currency-agnostic to support different
    ///     locales.
    /// </param>
    /// <param name="timeout">Optional timeout in milliseconds.</param>
    /// <returns>A task representing the wait operation.</returns>
    public async Task WaitForBalanceValueAsync(
        string expectedBalance,
        float? timeout = null
    ) =>
        await AccountAPanel
            .Locator("section:has(h2:text-is('Account Status')) div > div:has(> span:text-is('Balance:'))")
            .Filter(
                new()
                {
                    HasText = expectedBalance,
                })
            .WaitForAsync(
                new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeout,
                });

    /// <summary>
    ///     Waits for the command success message to appear.
    /// </summary>
    /// <param name="timeout">Optional timeout in milliseconds.</param>
    /// <returns>A task representing the wait operation.</returns>
    public async Task WaitForCommandSuccessAsync(
        float? timeout = null
    ) =>
        await AccountAPanel.Locator("div[role='status']")
            .WaitForAsync(
                new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeout,
                });

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
}