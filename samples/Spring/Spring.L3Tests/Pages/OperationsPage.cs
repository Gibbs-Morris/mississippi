using System.Globalization;
using System.Text.RegularExpressions;


namespace MississippiSamples.Spring.L3Tests.Pages;

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
    ///     Gets an account panel locator from its accessible region name.
    /// </summary>
    private ILocator AccountAPanel => page.Locator("#account-a-operations-panel");

    private ILocator AccountBPanel => page.Locator("#account-b-operations-panel");

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
    ///     Clicks the Start Transfer button in the selected account panel.
    /// </summary>
    /// <param name="account">The account slot, A or B.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task ClickStartTransferAsync(
        string account = "A"
    ) =>
        await GetAccountPanel(account)
            .GetByRole(
                AriaRole.Button,
                new()
                {
                    Name = "Start Transfer",
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
        await AccountAPanel.GetByLabel(
                "Account A deposit amount (£)",
                new()
                {
                    Exact = true,
                })
            .FillAsync(amount.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    ///     Enters the holder name for opening an account in the first account panel.
    /// </summary>
    /// <param name="holderName">The holder name to enter.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task EnterHolderNameAsync(
        string holderName
    ) =>
        await AccountAPanel.GetByLabel(
                "Account A holder name",
                new()
                {
                    Exact = true,
                })
            .FillAsync(holderName);

    /// <summary>
    ///     Enters the initial deposit amount for opening an account in the first account panel.
    /// </summary>
    /// <param name="amount">The initial deposit amount.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task EnterInitialDepositAsync(
        decimal amount
    ) =>
        await AccountAPanel.GetByLabel(
                "Account A initial deposit (£)",
                new()
                {
                    Exact = true,
                })
            .FillAsync(amount.ToString(CultureInfo.InvariantCulture));

    /// <summary>Enter the transfer amount in the selected account panel.</summary>
    /// <param name="amount">The transfer amount.</param>
    /// <param name="account">The account slot, A or B.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task EnterTransferAmountAsync(
        decimal amount,
        string account = "A"
    )
    {
        await OpenTaskAsync(account, "transfer");
        await GetAccountPanel(account)
            .GetByLabel(
                $"Account {account} transfer amount (£)",
                new()
                {
                    Exact = true,
                })
            .FillAsync(amount.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>Enter the withdraw amount in the first account panel.</summary>
    /// <param name="amount">The amount to withdraw.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task EnterWithdrawAmountAsync(
        decimal amount
    )
    {
        await OpenTaskAsync("A", "withdraw");
        await AccountAPanel.GetByLabel(
                "Account A withdrawal amount (£)",
                new()
                {
                    Exact = true,
                })
            .FillAsync(amount.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    ///     Gets the displayed account header from the selected account panel.
    /// </summary>
    /// <param name="account">The account slot, A or B.</param>
    /// <returns>The account ID text, or null if not present.</returns>
    public async Task<string?> GetAccountHeaderAsync(
        string account = "A"
    )
    {
        string headingId = account switch
        {
            "A" => "#account-a-panel-heading",
            "B" => "#account-b-panel-heading",
            var _ => throw new ArgumentOutOfRangeException(nameof(account), account, "Account slot must be A or B."),
        };
        ILocator accountHeader = GetAccountPanel(account).Locator(headingId);
        return await accountHeader.TextContentAsync();
    }

    /// <summary>Get the displayed balance from the first account panel's projection.</summary>
    /// <returns>The balance text (e.g., "£100.00"), or null if not present.</returns>
    public async Task<string?> GetBalanceTextAsync()
    {
        // The balance is an accessible live output, separate from command responses.
        ILocator balanceSection = AccountAPanel;
        ILocator balanceValue = balanceSection.GetByLabel(
            "Account A live balance",
            new()
            {
                Exact = true,
            });
        if (await balanceValue.CountAsync() > 0)
        {
            return await balanceValue.TextContentAsync();
        }

        return null;
    }

    /// <summary>Get the displayed holder name from the first account panel's projection.</summary>
    /// <returns>The holder name text, or null if not present.</returns>
    public async Task<string?> GetHolderNameTextAsync()
    {
        // The holder comes from the same live balance projection.
        ILocator holderSection = AccountAPanel;
        ILocator holderValue = holderSection.Locator("[data-spring-holder]");
        if (await holderValue.CountAsync() > 0)
        {
            return await holderValue.TextContentAsync();
        }

        return null;
    }

    /// <summary>Get the displayed status from the first account panel's projection.</summary>
    /// <returns>The status text (e.g., "Open"), or null if not present.</returns>
    public async Task<string?> GetStatusTextAsync()
    {
        // Account status is projected domain data.
        ILocator statusSection = AccountAPanel;
        ILocator statusValue = statusSection.Locator("[data-spring-account-status]");
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
    ///     Gets the value rendered in the selected account panel's read-only transfer destination.
    /// </summary>
    /// <param name="account">The account slot, A or B.</param>
    /// <returns>The destination account ID shown in the input.</returns>
    public async Task<string> GetTransferDestinationValueAsync(
        string account = "A"
    ) =>
        await GetAccountPanel(account)
            .GetByLabel(
                $"Account {account} transfer destination account",
                new()
                {
                    Exact = true,
                })
            .InputValueAsync();

    /// <summary>
    ///     Gets the accessible transfer status strip for the selected account.
    /// </summary>
    /// <param name="account">The account slot, A or B.</param>
    /// <returns>The status strip locator.</returns>
    public ILocator GetTransferStatus(
        string account = "A"
    ) =>
        GetAccountPanel(account)
            .Locator(
                account switch
                {
                    "A" => "#account-a-transfer-status",
                    "B" => "#account-b-transfer-status",
                    var _ => throw new ArgumentOutOfRangeException(
                        nameof(account),
                        account,
                        "Account slot must be A or B."),
                });

    /// <summary>Select a theme using the shell's accessible theme controls and wait for the theme to apply.</summary>
    /// <param name="label">The accessible theme button label.</param>
    /// <param name="themeAttribute">The expected document theme attribute value.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task SetThemeAsync(
        string label,
        string themeAttribute
    )
    {
        ILocator appearance = page.Locator("details.spring-appearance");
        if (await appearance.GetAttributeAsync("open") is null)
        {
            await appearance.Locator("summary").ClickAsync();
        }

        await page.GetByRole(
                AriaRole.Group,
                new()
                {
                    Name = "Color theme",
                })
            .GetByRole(
                AriaRole.Button,
                new()
                {
                    Name = label,
                    Exact = true,
                })
            .ClickAsync();
        await page.Locator($"html[data-rf-theme='{themeAttribute}']")
            .WaitForAsync(
                new()
                {
                    State = WaitForSelectorState.Visible,
                });
    }

    /// <summary>Wait for the balance projection to appear in the selected account panel.</summary>
    /// <param name="timeout">Optional timeout in milliseconds.</param>
    /// <param name="account">The account slot, A or B.</param>
    /// <returns>A task representing the wait operation.</returns>
    public async Task WaitForBalanceAsync(
        float? timeout = null,
        string account = "A"
    ) =>
        await GetAccountPanel(account)
            .GetByLabel(
                $"Account {account} live balance",
                new()
                {
                    Exact = true,
                })
            .WaitForAsync(
                new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeout,
                });

    /// <summary>Wait for the balance projection to show a specific value in the selected account panel.</summary>
    /// <param name="expectedBalance">
    ///     The expected balance value (e.g., "100.00"). The Spring UI displays the GBP currency used by the domain.
    /// </param>
    /// <param name="timeout">Optional timeout in milliseconds.</param>
    /// <param name="account">The account slot, A or B.</param>
    /// <returns>A task representing the wait operation.</returns>
    public async Task WaitForBalanceValueAsync(
        string expectedBalance,
        float? timeout = null,
        string account = "A"
    ) =>
        await GetAccountPanel(account)
            .GetByLabel(
                $"Account {account} live balance",
                new()
                {
                    Exact = true,
                })
            .Filter(
                new()
                {
                    HasTextRegex = new($"^£{Regex.Escape(expectedBalance)}$"),
                })
            .WaitForAsync(
                new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeout,
                });

    /// <summary>Wait for the command acceptance response to appear.</summary>
    /// <param name="timeout">Optional timeout in milliseconds.</param>
    /// <returns>A task representing the wait operation.</returns>
    public async Task WaitForCommandSuccessAsync(
        float? timeout = null
    ) =>
        await page.GetByRole(
                AriaRole.Region,
                new()
                {
                    Name = "Banking responses · this browser",
                    Exact = true,
                })
            .GetByText(
                "Latest response: accepted.",
                new()
                {
                    Exact = true,
                })
            .WaitForAsync(
                new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeout,
                });

    /// <summary>Wait for the SignalR connection status to reach the expected value.</summary>
    /// <param name="expectedStatus">The expected connection status text (e.g., "Connected").</param>
    /// <param name="timeout">Optional timeout in milliseconds.</param>
    /// <returns>A task representing the wait operation.</returns>
    public async Task WaitForConnectionStatusAsync(
        string expectedStatus,
        float? timeout = null
    ) =>
        await page.GetByRole(
                AriaRole.Button,
                new()
                {
                    Name = $"Connection status: {expectedStatus}",
                    Exact = true,
                })
            .Filter(
                new()
                {
                    HasTextRegex = new($"^\\s*{Regex.Escape(expectedStatus)}\\s*$"),
                })
            .WaitForAsync(
                new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeout,
                });

    /// <summary>
    ///     Waits for the selected account's live transfer projection to report a phase.
    /// </summary>
    /// <param name="phase">The projected saga phase.</param>
    /// <param name="timeout">Optional timeout in milliseconds.</param>
    /// <param name="account">The account slot, A or B.</param>
    /// <returns>A task representing the wait operation.</returns>
    public async Task WaitForTransferPhaseAsync(
        string phase,
        float? timeout = null,
        string account = "A"
    ) =>
        await GetTransferStatus(account)
            .GetByText(
                $"Phase: {phase}",
                new()
                {
                    Exact = true,
                })
            .WaitForAsync(
                new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeout,
                });

    private ILocator GetAccountPanel(
        string account
    ) =>
        account switch
        {
            "A" => AccountAPanel,
            "B" => AccountBPanel,
            var _ => throw new ArgumentOutOfRangeException(nameof(account), account, "Account slot must be A or B."),
        };

    private async Task OpenTaskAsync(
        string account,
        string task
    )
    {
        string prefix = account switch
        {
            "A" => "account-a",
            "B" => "account-b",
            var _ => throw new ArgumentOutOfRangeException(nameof(account), account, "Account slot must be A or B."),
        };
        ILocator disclosure = GetAccountPanel(account).Locator($"#{prefix}-{task}-task");
        if (await disclosure.GetAttributeAsync("open") is null)
        {
            await disclosure.Locator("summary").ClickAsync();
        }
    }
}