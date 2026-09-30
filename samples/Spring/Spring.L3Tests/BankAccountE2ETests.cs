using System.IO;

using MississippiSamples.Spring.L3Tests.Pages;


namespace MississippiSamples.Spring.L3Tests;

/// <summary>
///     End-to-end tests for the Bank Account Demo using Playwright.
///     Tests the full flow from Blazor UI through SignalR/Inlet projections and Orleans grains.
/// </summary>
[Collection(SpringBrowserCollectionDefinition.Name)]
public sealed class BankAccountE2ETests
{
    /// <summary>
    ///     Timeout for waiting on SignalR projection updates (120 seconds).
    ///     Extended from 60s to accommodate CI environments with slower container/cluster startup.
    /// </summary>
    private const float ProjectionTimeout = 120_000;

    private readonly SpringBrowserFixture fixture;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BankAccountE2ETests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Spring fixture.</param>
    public BankAccountE2ETests(
        SpringBrowserFixture fixture
    ) =>
        this.fixture = fixture;

    private static async Task SaveAccountSetupScreenshotsAsync(
        IPage page
    )
    {
        string? artifactsDirectory = Environment.GetEnvironmentVariable("SPRING_TEST_ARTIFACTS");
        if (string.IsNullOrWhiteSpace(artifactsDirectory))
        {
            return;
        }

        Directory.CreateDirectory(artifactsDirectory);
        await page.SetViewportSizeAsync(1440, 900);
        await page.ScreenshotAsync(
            new()
            {
                Path = Path.Join(artifactsDirectory, "account-setup-dark-desktop.png"),
                FullPage = true,
            });
        await page.SetViewportSizeAsync(390, 844);
        await page.ScreenshotAsync(
            new()
            {
                Path = Path.Join(artifactsDirectory, "account-setup-dark-mobile.png"),
                FullPage = true,
            });
        await page.SetViewportSizeAsync(1440, 900);
    }

    /// <summary>
    ///     Verifies setup links to distinct, accessible account panels and amount drafts stay isolated.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task AccountSetupShouldShareIndependentAccessibleOperationsPanels()
    {
        Assert.True(fixture.IsInitialized, "fixture must be initialized");
        IPage page = await fixture.CreatePageAsync();
        try
        {
            AccountsPage accountsPage = new(page);
            await accountsPage.NavigateAsync(fixture.GatewayBaseUri);
            await accountsPage.WaitForConnectionStatusAsync("Connected", ProjectionTimeout);
            await accountsPage.ClickInitializeDemoAccountsAsync();
            await accountsPage.WaitForDemoAccountsInitializedAsync(ProjectionTimeout);
            string accountAId = (await page.Locator("#demo-account-a-id").TextContentAsync())?.Trim() ?? string.Empty;
            string accountBId = (await page.Locator("#demo-account-b-id").TextContentAsync())?.Trim() ?? string.Empty;
            string operationsHref = await page.GetByRole(
                                            AriaRole.Link,
                                            new()
                                            {
                                                Name = "Go to Operations",
                                                Exact = true,
                                            })
                                        .GetAttributeAsync("href") ??
                                    string.Empty;
            Assert.False(string.IsNullOrWhiteSpace(accountAId));
            Assert.False(string.IsNullOrWhiteSpace(accountBId));
            Assert.NotEqual(accountAId, accountBId);
            Assert.Contains($"a={Uri.EscapeDataString(accountAId)}", operationsHref, StringComparison.Ordinal);
            Assert.Contains($"b={Uri.EscapeDataString(accountBId)}", operationsHref, StringComparison.Ordinal);
            await SaveAccountSetupScreenshotsAsync(page);
            string encodedAccountAId = Uri.EscapeDataString(accountAId);
            string encodedAccountBId = Uri.EscapeDataString(accountBId);
            string paddedOperationsHref = operationsHref
                .Replace($"a={encodedAccountAId}", $"a=%20{encodedAccountAId}%20", StringComparison.Ordinal)
                .Replace($"b={encodedAccountBId}", $"b=%20{encodedAccountBId}%20", StringComparison.Ordinal);
            Assert.Contains($"a=%20{encodedAccountAId}%20", paddedOperationsHref, StringComparison.Ordinal);
            Assert.Contains($"b=%20{encodedAccountBId}%20", paddedOperationsHref, StringComparison.Ordinal);
            await page.GotoAsync(new Uri(fixture.GatewayBaseUri, paddedOperationsHref).ToString());
            OperationsPage operationsPage = new(page);
            await operationsPage.WaitForBalanceAsync(ProjectionTimeout);
            await operationsPage.WaitForBalanceAsync(ProjectionTimeout, "B");
            Assert.Contains(accountAId, await operationsPage.GetAccountHeaderAsync(), StringComparison.Ordinal);
            string? accountBHeader = await page.Locator("#account-b-panel-heading").TextContentAsync();
            Assert.Contains(accountBId, accountBHeader, StringComparison.Ordinal);
            ILocator accountADeposit = page.GetByLabel(
                "Account A deposit amount (£)",
                new()
                {
                    Exact = true,
                });
            ILocator accountBDeposit = page.GetByLabel(
                "Account B deposit amount (£)",
                new()
                {
                    Exact = true,
                });
            Assert.Equal("account-a-deposit-amount-input", await accountADeposit.GetAttributeAsync("id"));
            Assert.Equal("account-b-deposit-amount-input", await accountBDeposit.GetAttributeAsync("id"));
            await operationsPage.EnterDepositAmountAsync(12.34m);
            await operationsPage.ClickDepositAsync();
            await operationsPage.WaitForBalanceValueAsync("512.34", ProjectionTimeout);
            await operationsPage.WaitForBalanceValueAsync("500.00", ProjectionTimeout, "B");
            await accountBDeposit.FillAsync("12.");
            Assert.Equal("true", await accountBDeposit.GetAttributeAsync("aria-invalid"));
            ILocator accountBDepositButton = page.Locator("#account-b-operations-panel")
                .GetByRole(
                    AriaRole.Button,
                    new()
                    {
                        Name = "Deposit £",
                        Exact = true,
                    });
            Assert.False(await accountBDepositButton.IsEnabledAsync());
            Assert.Equal("12.34", await accountADeposit.InputValueAsync());
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    ///     Verifies the accounts page loads and displays the correct title.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task AccountsPageShouldDisplayTitle()
    {
        // Arrange
        Assert.True(fixture.IsInitialized, "fixture must be initialized");
        IPage page = await fixture.CreatePageAsync();
        try
        {
            AccountsPage accountsPage = new(page);

            // Act
            await accountsPage.NavigateAsync(fixture.GatewayBaseUri);
            await accountsPage.WaitForConnectionStatusAsync("Connected", ProjectionTimeout);
            string? title = await page.Locator("h1").TextContentAsync();

            // Assert
            Assert.Equal("Bank Account Operations", title);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    ///     Verifies the deposit button works and updates the balance.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task DepositButtonShouldUpdateBalance()
    {
        // Arrange
        Assert.True(fixture.IsInitialized, "fixture must be initialized");
        IPage page = await fixture.CreatePageAsync();
        try
        {
            // Demo accounts are pre-opened with £500 each
            OperationsPage operationsPage = await BankAccountScenario.PrepareAsync(fixture, page, ProjectionTimeout);
            await operationsPage.WaitForBalanceAsync(ProjectionTimeout);

            // Act - Deposit
            const decimal additionalDeposit = 75.00m;
            await operationsPage.EnterDepositAmountAsync(additionalDeposit);
            await operationsPage.ClickDepositAsync();
            await operationsPage.WaitForBalanceValueAsync("575.00", ProjectionTimeout);

            // Assert
            string? balanceText = await operationsPage.GetBalanceTextAsync();
            Assert.NotNull(balanceText);
            Assert.Contains("575.00", balanceText, StringComparison.Ordinal);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    ///     Verifies the fixture initializes successfully.
    /// </summary>
    [Fact]
    public void FixtureShouldBeInitialized()
    {
        // Assert
        Assert.True(fixture.IsInitialized, "the Spring AppHost should start successfully");
        Assert.Null(fixture.InitializationError);
        Assert.NotEqual(new("about:blank"), fixture.GatewayBaseUri);
    }

    /// <summary>
    ///     Verifies that demo account initialization displays the balance projection.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task InitializeDemoAccountsShouldDisplayBalanceProjection()
    {
        // Arrange
        Assert.True(fixture.IsInitialized, "fixture must be initialized");
        IPage page = await fixture.CreatePageAsync();
        try
        {
            // Demo accounts are pre-opened with £500 each
            OperationsPage operationsPage = await BankAccountScenario.PrepareAsync(fixture, page, ProjectionTimeout);

            // Wait for projection update via SignalR
            await operationsPage.WaitForBalanceAsync(ProjectionTimeout);

            // Assert
            string? balanceText = await operationsPage.GetBalanceTextAsync();
            Assert.NotNull(balanceText);
            Assert.Contains("500.00", balanceText, StringComparison.Ordinal);
            string? holderText = await operationsPage.GetHolderNameTextAsync();
            Assert.False(string.IsNullOrEmpty(holderText), "holder name should be displayed");
            string? statusText = await operationsPage.GetStatusTextAsync();
            Assert.NotNull(statusText);
            Assert.Contains("Open", statusText, StringComparison.Ordinal);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    ///     Verifies that the operations page shows account headers after setup.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task OperationsPageShouldDisplayAccountHeader()
    {
        // Arrange
        Assert.True(fixture.IsInitialized, "fixture must be initialized");
        IPage page = await fixture.CreatePageAsync();
        try
        {
            // Demo accounts are pre-opened with £500 each
            OperationsPage operationsPage = await BankAccountScenario.PrepareAsync(fixture, page, ProjectionTimeout);

            // Assert
            string? accountHeader = await operationsPage.GetAccountHeaderAsync();
            Assert.False(string.IsNullOrEmpty(accountHeader), "account header should be displayed");
            Assert.Contains("Account A", accountHeader, StringComparison.Ordinal);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    ///     Verifies the withdraw button works and updates the balance.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task WithdrawButtonShouldUpdateBalance()
    {
        // Arrange
        Assert.True(fixture.IsInitialized, "fixture must be initialized");
        IPage page = await fixture.CreatePageAsync();
        try
        {
            // Demo accounts are pre-opened with £500 each
            OperationsPage operationsPage = await BankAccountScenario.PrepareAsync(fixture, page, ProjectionTimeout);
            await operationsPage.WaitForBalanceAsync(ProjectionTimeout);

            // Act - Withdraw
            const decimal withdrawAmount = 50.00m;
            await operationsPage.EnterWithdrawAmountAsync(withdrawAmount);
            await operationsPage.ClickWithdrawAsync();
            await operationsPage.WaitForCommandSuccessAsync(ProjectionTimeout);
            await operationsPage.WaitForBalanceValueAsync("450.00", ProjectionTimeout);

            // Assert
            string? balanceText = await operationsPage.GetBalanceTextAsync();
            Assert.NotNull(balanceText);
            Assert.Contains("450.00", balanceText, StringComparison.Ordinal);
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}