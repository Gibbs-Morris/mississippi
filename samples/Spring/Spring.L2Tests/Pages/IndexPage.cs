using System.Globalization;
using System.Text.RegularExpressions;


namespace Spring.L2Tests.Pages;

/// <summary>
///     Page Object Model for the Spring sample Index page (Bank Account Demo).
///     Encapsulates Playwright interactions with the bank account UI.
/// </summary>
public sealed partial class IndexPage
{
    private readonly IPage page;

    /// <summary>
    ///     Initializes a new instance of the <see cref="IndexPage" /> class.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    public IndexPage(
        IPage page
    ) =>
        this.page = page;

    /// <summary>
    ///     Checks if the loading indicator is visible.
    ///     Note: Loading indicator was removed in the unstyled version.
    /// </summary>
    /// <returns>Always returns false as loading indicator no longer exists.</returns>
    public static Task<bool> IsLoadingVisibleAsync() => Task.FromResult(false);

    /// <summary>
    ///     Regex pattern to match "Account: " prefix for account header.
    /// </summary>
    [GeneratedRegex("^Account:")]
    private static partial Regex AccountHeaderPattern();

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
    ///     Clicks the Deposit button.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task ClickDepositAsync() =>
        await page.GetByRole(
                AriaRole.Button,
                new()
                {
                    NameRegex = DepositButtonPattern(),
                })
            .ClickAsync();

    /// <summary>
    ///     Clicks the Open Account button.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task ClickOpenAccountAsync() =>
        await page.GetByRole(
                AriaRole.Button,
                new()
                {
                    Name = "Open Account",
                    Exact = true,
                })
            .ClickAsync();

    /// <summary>
    ///     Clicks the Set Account button (Continue).
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task ClickSetAccountAsync() =>
        await page.GetByRole(
                AriaRole.Button,
                new()
                {
                    Name = "Continue",
                    Exact = true,
                })
            .ClickAsync();

    /// <summary>
    ///     Clicks the Withdraw button.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task ClickWithdrawAsync() =>
        await page.GetByRole(
                AriaRole.Button,
                new()
                {
                    NameRegex = WithdrawButtonPattern(),
                })
            .ClickAsync();

    /// <summary>
    ///     Enters the account ID in the input field.
    /// </summary>
    /// <param name="accountId">The account ID to enter.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task EnterAccountIdAsync(
        string accountId
    ) =>
        await page.GetByPlaceholder("Enter account ID").FillAsync(accountId);

    /// <summary>
    ///     Enters the deposit amount.
    /// </summary>
    /// <param name="amount">The amount to deposit.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task EnterDepositAmountAsync(
        decimal amount
    ) =>
        await page.Locator("#deposit-amount-input").FillAsync(amount.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    ///     Enters the holder name for opening an account.
    /// </summary>
    /// <param name="holderName">The holder name to enter.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task EnterHolderNameAsync(
        string holderName
    ) =>
        await page.Locator("#holder-name-input").FillAsync(holderName);

    /// <summary>
    ///     Enters the initial deposit amount for opening an account.
    /// </summary>
    /// <param name="amount">The initial deposit amount.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task EnterInitialDepositAsync(
        decimal amount
    ) =>
        await page.Locator("#initial-deposit-input").FillAsync(amount.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    ///     Enters the withdraw amount.
    /// </summary>
    /// <param name="amount">The amount to withdraw.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task EnterWithdrawAmountAsync(
        decimal amount
    ) =>
        await page.Locator("#withdraw-amount-input").FillAsync(amount.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    ///     Gets the displayed account header (entity ID from the account switcher).
    /// </summary>
    /// <returns>The account ID text, or null if not present.</returns>
    public async Task<string?> GetAccountHeaderAsync()
    {
        // The account header shows "Account: {id}" in a span within the header
        ILocator accountHeader = page.Locator("main > div > span")
            .Filter(
                new()
                {
                    HasTextRegex = AccountHeaderPattern(),
                });
        if (await accountHeader.CountAsync() > 0)
        {
            string? text = await accountHeader.TextContentAsync();

            // Extract just the ID from "Account: {id}"
            return text?.Replace("Account:", string.Empty, StringComparison.Ordinal).Trim();
        }

        return null;
    }

    /// <summary>
    ///     Gets the displayed balance from the projection.
    /// </summary>
    /// <returns>The balance text (e.g., "£100.00" or "$100.00" depending on locale), or null if not present.</returns>
    public async Task<string?> GetBalanceTextAsync()
    {
        // Balance is shown as "Balance:" label followed by the value span
        ILocator balanceSection = page.Locator("section:has(h2:text-is('Account Status'))");
        ILocator balanceValue = balanceSection.Locator("div:has(span:text-is('Balance:')) > span").Last;
        if (await balanceValue.CountAsync() > 0)
        {
            return await balanceValue.TextContentAsync();
        }

        return null;
    }

    /// <summary>
    ///     Gets the error message if one is displayed.
    /// </summary>
    /// <returns>The error message text, or null if not present.</returns>
    public async Task<string?> GetErrorMessageAsync()
    {
        ILocator errorLocator = page.Locator("div[role='alert']");
        if (await errorLocator.CountAsync() > 0)
        {
            string? text = await errorLocator.TextContentAsync();

            // Remove the "Error:" prefix
            return text?.Replace("Error:", string.Empty, StringComparison.Ordinal).Trim();
        }

        return null;
    }

    /// <summary>
    ///     Gets the displayed holder name from the projection.
    /// </summary>
    /// <returns>The holder name text, or null if not present.</returns>
    public async Task<string?> GetHolderNameTextAsync()
    {
        // Holder is shown as "Holder:" label followed by the value span
        ILocator holderSection = page.Locator("section:has(h2:text-is('Account Status'))");
        ILocator holderValue = holderSection.Locator("div:has(span:text-is('Holder:')) > span").Last;
        if (await holderValue.CountAsync() > 0)
        {
            return await holderValue.TextContentAsync();
        }

        return null;
    }

    /// <summary>
    ///     Gets the displayed status from the projection.
    /// </summary>
    /// <returns>The status text (e.g., "Open"), or null if not present.</returns>
    public async Task<string?> GetStatusTextAsync()
    {
        // Status is shown as "Status:" label followed by the value span
        ILocator statusSection = page.Locator("section:has(h2:text-is('Account Status'))");
        ILocator statusValue = statusSection.Locator("div:has(span:text-is('Status:')) > span").Last;
        if (await statusValue.CountAsync() > 0)
        {
            return await statusValue.TextContentAsync();
        }

        return null;
    }

    /// <summary>
    ///     Gets the success message if one is displayed.
    /// </summary>
    /// <returns>The success message text, or null if not present.</returns>
    public async Task<string?> GetSuccessMessageAsync()
    {
        ILocator successLocator = page.Locator("div[role='status']");
        if (await successLocator.CountAsync() > 0)
        {
            return await successLocator.TextContentAsync();
        }

        return null;
    }

    /// <summary>
    ///     Gets the page title (h1 element).
    /// </summary>
    /// <returns>The page title.</returns>
    public async Task<string?> GetTitleAsync() => await page.Locator("h1").TextContentAsync();

    /// <summary>
    ///     Checks if the Deposit button is disabled.
    /// </summary>
    /// <returns>True if disabled.</returns>
    public async Task<bool> IsDepositButtonDisabledAsync() =>
        await page.GetByRole(
                AriaRole.Button,
                new()
                {
                    NameRegex = DepositButtonPattern(),
                })
            .IsDisabledAsync();

    /// <summary>
    ///     Checks if the Open Account button is disabled.
    /// </summary>
    /// <returns>True if disabled.</returns>
    public async Task<bool> IsOpenAccountButtonDisabledAsync() =>
        await page.GetByRole(
                AriaRole.Button,
                new()
                {
                    Name = "Open Account",
                    Exact = true,
                })
            .IsDisabledAsync();

    /// <summary>
    ///     Checks if the Withdraw button is disabled.
    /// </summary>
    /// <returns>True if disabled.</returns>
    public async Task<bool> IsWithdrawButtonDisabledAsync() =>
        await page.GetByRole(
                AriaRole.Button,
                new()
                {
                    NameRegex = WithdrawButtonPattern(),
                })
            .IsDisabledAsync();

    /// <summary>
    ///     Navigates to the index page.
    /// </summary>
    /// <param name="baseUri">The base URI of the application.</param>
    /// <returns>A task representing the navigation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="baseUri" /> is null.</exception>
    public async Task NavigateAsync(
        Uri baseUri
    )
    {
        ArgumentNullException.ThrowIfNull(baseUri);
        await page.GotoAsync(
            baseUri.ToString(),
            new()
            {
                WaitUntil = WaitUntilState.NetworkIdle,
            });
    }

    /// <summary>
    ///     Sets up the account ID by entering it and clicking Set Account.
    /// </summary>
    /// <param name="accountId">The account ID to set.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task SetAccountAsync(
        string accountId
    )
    {
        await EnterAccountIdAsync(accountId);
        await ClickSetAccountAsync();
    }

    /// <summary>
    ///     Waits for the balance projection to appear on the page.
    /// </summary>
    /// <param name="timeout">Optional timeout in milliseconds.</param>
    /// <returns>A task representing the wait operation.</returns>
    public async Task WaitForBalanceAsync(
        float? timeout = null
    ) =>
        await page.Locator("section:has(h2:text-is('Account Status')) div > div:has(> span:text-is('Balance:'))")
            .WaitForAsync(
                new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeout,
                });

    /// <summary>
    ///     Waits for the balance projection to show a specific value.
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
        await page.Locator("section:has(h2:text-is('Account Status')) div > div:has(> span:text-is('Balance:'))")
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
        await page.Locator("div[role='status']")
            .WaitForAsync(
                new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeout,
                });
}