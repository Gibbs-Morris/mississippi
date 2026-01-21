using System.Globalization;


namespace Spring.L2Tests.Pages;

/// <summary>
///     Page Object Model for the Spring sample Index page (Bank Account Demo).
///     Encapsulates Playwright interactions with the bank account UI.
/// </summary>
public sealed class IndexPage
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
    ///     Clicks the Deposit button.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task ClickDepositAsync() =>
        await page.GetByRole(
                AriaRole.Button,
                new()
                {
                    Name = "Deposit",
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
                })
            .ClickAsync();

    /// <summary>
    ///     Clicks the Set Account button.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task ClickSetAccountAsync() =>
        await page.GetByRole(
                AriaRole.Button,
                new()
                {
                    Name = "Set Account",
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
                    Name = "Withdraw",
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
        await page.Locator("div:has(h3:text('Deposit Funds')) input[type='number']")
            .FillAsync(amount.ToString(CultureInfo.InvariantCulture));

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
        await page.Locator("div:has(h3:text('Withdraw Funds')) input[type='number']")
            .FillAsync(amount.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    ///     Gets the displayed account header (e.g., "Account: test-123").
    /// </summary>
    /// <returns>The account header text, or null if not present.</returns>
    public async Task<string?> GetAccountHeaderAsync()
    {
        ILocator accountHeader = page.Locator("#operations-panel-title");
        if (await accountHeader.CountAsync() > 0)
        {
            return await accountHeader.TextContentAsync();
        }

        return null;
    }

    /// <summary>
    ///     Gets the displayed balance from the projection.
    /// </summary>
    /// <returns>The balance text (e.g., "$100.00"), or null if not present.</returns>
    public async Task<string?> GetBalanceTextAsync()
    {
        ILocator balanceLocator = page.Locator("p:has(strong:text('Balance:'))");
        if (await balanceLocator.CountAsync() > 0)
        {
            return await balanceLocator.TextContentAsync();
        }

        return null;
    }

    /// <summary>
    ///     Gets the error message if one is displayed.
    /// </summary>
    /// <returns>The error message text, or null if not present.</returns>
    public async Task<string?> GetErrorMessageAsync()
    {
        ILocator errorLocator = page.Locator("p[style*='color: red']");
        if (await errorLocator.CountAsync() > 0)
        {
            return await errorLocator.TextContentAsync();
        }

        return null;
    }

    /// <summary>
    ///     Gets the displayed holder name from the projection.
    /// </summary>
    /// <returns>The holder name text, or null if not present.</returns>
    public async Task<string?> GetHolderNameTextAsync()
    {
        ILocator holderLocator = page.Locator("p:has(strong:text('Holder:'))");
        if (await holderLocator.CountAsync() > 0)
        {
            return await holderLocator.TextContentAsync();
        }

        return null;
    }

    /// <summary>
    ///     Gets the displayed status from the projection.
    /// </summary>
    /// <returns>The status text (e.g., "Open"), or null if not present.</returns>
    public async Task<string?> GetStatusTextAsync()
    {
        ILocator statusLocator = page.Locator("p:has(strong:text('Status:'))");
        if (await statusLocator.CountAsync() > 0)
        {
            return await statusLocator.TextContentAsync();
        }

        return null;
    }

    /// <summary>
    ///     Gets the success message if one is displayed.
    /// </summary>
    /// <returns>The success message text, or null if not present.</returns>
    public async Task<string?> GetSuccessMessageAsync()
    {
        ILocator successLocator = page.Locator("p[style*='color: green']");
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
                    Name = "Deposit",
                })
            .IsDisabledAsync();

    /// <summary>
    ///     Checks if the loading indicator is visible.
    /// </summary>
    /// <returns>True if loading is visible.</returns>
    public async Task<bool> IsLoadingVisibleAsync()
    {
        ILocator loadingLocator = page.Locator("p")
            .Filter(
                new()
                {
                    HasText = "Loading...",
                });
        return await loadingLocator.CountAsync() > 0;
    }

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
                    Name = "Withdraw",
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
        await page.Locator("p:has(strong:text('Balance:'))")
            .WaitForAsync(
                new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeout,
                });

    /// <summary>
    ///     Waits for the balance projection to show a specific value.
    /// </summary>
    /// <param name="expectedBalance">The expected balance formatted string (e.g., "$100.00").</param>
    /// <param name="timeout">Optional timeout in milliseconds.</param>
    /// <returns>A task representing the wait operation.</returns>
    public async Task WaitForBalanceValueAsync(
        string expectedBalance,
        float? timeout = null
    ) =>
        await page.Locator("p:has(strong:text('Balance:'))")
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
        await page.Locator("p[style*='color: green']")
            .WaitForAsync(
                new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeout,
                });
}