using Spring.L2Tests.Pages;


namespace Spring.L2Tests;

/// <summary>
///     End-to-end tests for the Bank Account Demo using Playwright.
///     Tests the full flow from Blazor UI through SignalR/Inlet projections and Orleans grains.
/// </summary>
[Collection(SpringTestCollection.Name)]
public sealed class BankAccountE2ETests
{
    /// <summary>
    ///     Timeout for waiting on SignalR projection updates (60 seconds).
    ///     Extended from 30s to accommodate CI environments with slower container/cluster startup.
    /// </summary>
    private const float ProjectionTimeout = 60_000;

    private readonly SpringFixture fixture;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BankAccountE2ETests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Spring fixture.</param>
    public BankAccountE2ETests(
        SpringFixture fixture
    ) =>
        this.fixture = fixture;

    /// <summary>
    ///     Sets up demo accounts and navigates to the operations page.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    /// <returns>The operations page object ready for interactions.</returns>
    private async Task<OperationsPage> SetupDemoAccountsAndNavigateToOperationsAsync(
        IPage page
    )
    {
        // Navigate to accounts page and initialize demo accounts
        AccountsPage accountsPage = new(page);
        await accountsPage.NavigateAsync(fixture.ServerBaseUri);
        await accountsPage.WaitForConnectionStatusAsync("Connected", ProjectionTimeout);
        await accountsPage.ClickInitializeDemoAccountsAsync();
        await accountsPage.WaitForDemoAccountsInitializedAsync(ProjectionTimeout);
        await accountsPage.ClickGoToOperationsAsync();

        // Return operations page for further interactions
        OperationsPage operationsPage = new(page);
        await operationsPage.WaitForConnectionStatusAsync("Connected", ProjectionTimeout);
        return operationsPage;
    }

    /// <summary>
    ///     Verifies the accounts page loads and displays the correct title.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task AccountsPageShouldDisplayTitle()
    {
        // Arrange
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        IPage page = await fixture.CreatePageAsync();
        try
        {
            AccountsPage accountsPage = new(page);

            // Act
            await accountsPage.NavigateAsync(fixture.ServerBaseUri);
            await accountsPage.WaitForConnectionStatusAsync("Connected", ProjectionTimeout);
            string? title = await page.Locator("h1").TextContentAsync();

            // Assert
            title.Should().Be("Bank Account Operations");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    ///     Verifies the complete bank account flow via UI: open, deposit, withdraw,
    ///     and confirms the balance updates in real-time via SignalR projection.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CompleteBankAccountFlowShouldUpdateProjectionViaSignalR()
    {
        // Arrange
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        IPage page = await fixture.CreatePageAsync();
        try
        {
            // Demo accounts are pre-opened with £500 each
            OperationsPage operationsPage = await SetupDemoAccountsAndNavigateToOperationsAsync(page);

            // Wait for projection to show the balance via SignalR
            await operationsPage.WaitForBalanceAsync(ProjectionTimeout);

            // Assert - Verify initial state (demo accounts start with £500)
            string? balanceText = await operationsPage.GetBalanceTextAsync();
            balanceText.Should().Contain("500.00", "demo account should start with £500");
            string? holderText = await operationsPage.GetHolderNameTextAsync();
            holderText.Should().NotBeNullOrEmpty("holder name should be displayed");
            string? statusText = await operationsPage.GetStatusTextAsync();
            statusText.Should().Contain("Open", "account status should be Open");

            // Act - Deposit funds
            const decimal depositAmount = 50.00m;
            await operationsPage.EnterDepositAmountAsync(depositAmount);
            await operationsPage.ClickDepositAsync();
            await operationsPage.WaitForCommandSuccessAsync(ProjectionTimeout);
            await operationsPage.WaitForBalanceValueAsync("550.00", ProjectionTimeout);
            balanceText = await operationsPage.GetBalanceTextAsync();
            balanceText.Should().Contain("550.00", "balance should be £550 after deposit");

            // Act - Withdraw funds
            const decimal withdrawAmount = 25.00m;
            await operationsPage.EnterWithdrawAmountAsync(withdrawAmount);
            await operationsPage.ClickWithdrawAsync();
            await operationsPage.WaitForCommandSuccessAsync(ProjectionTimeout);
            await operationsPage.WaitForBalanceValueAsync("525.00", ProjectionTimeout);

            // Assert - Final balance
            balanceText = await operationsPage.GetBalanceTextAsync();
            balanceText.Should().Contain("525.00", "final balance should reflect all transactions");
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
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        IPage page = await fixture.CreatePageAsync();
        try
        {
            // Demo accounts are pre-opened with £500 each
            OperationsPage operationsPage = await SetupDemoAccountsAndNavigateToOperationsAsync(page);
            await operationsPage.WaitForBalanceAsync(ProjectionTimeout);

            // Act - Deposit
            const decimal additionalDeposit = 75.00m;
            await operationsPage.EnterDepositAmountAsync(additionalDeposit);
            await operationsPage.ClickDepositAsync();
            await operationsPage.WaitForBalanceValueAsync("575.00", ProjectionTimeout);

            // Assert
            string? balanceText = await operationsPage.GetBalanceTextAsync();
            balanceText.Should().Contain("575.00");
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
        fixture.IsInitialized.Should().BeTrue("the Spring AppHost should start successfully");
        fixture.InitializationError.Should().BeNull("there should be no initialization errors");
        fixture.ServerBaseUri.Should().NotBe(new Uri("about:blank"), "the server should have a valid URL");
    }

    /// <summary>
    ///     Verifies that demo account initialization displays the balance projection.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task InitializeDemoAccountsShouldDisplayBalanceProjection()
    {
        // Arrange
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        IPage page = await fixture.CreatePageAsync();
        try
        {
            // Demo accounts are pre-opened with £500 each
            OperationsPage operationsPage = await SetupDemoAccountsAndNavigateToOperationsAsync(page);

            // Wait for projection update via SignalR
            await operationsPage.WaitForBalanceAsync(ProjectionTimeout);

            // Assert
            string? balanceText = await operationsPage.GetBalanceTextAsync();
            balanceText.Should().Contain("500.00", "demo account should have £500 initial balance");
            string? holderText = await operationsPage.GetHolderNameTextAsync();
            holderText.Should().NotBeNullOrEmpty("holder name should be displayed");
            string? statusText = await operationsPage.GetStatusTextAsync();
            statusText.Should().Contain("Open", "account status should be Open");
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
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        IPage page = await fixture.CreatePageAsync();
        try
        {
            // Demo accounts are pre-opened with £500 each
            OperationsPage operationsPage = await SetupDemoAccountsAndNavigateToOperationsAsync(page);

            // Assert
            string? accountHeader = await operationsPage.GetAccountHeaderAsync();
            accountHeader.Should().NotBeNullOrEmpty("account header should be displayed");
            accountHeader.Should().Contain("Account A", "should show Account A panel label");
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
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        IPage page = await fixture.CreatePageAsync();
        try
        {
            // Demo accounts are pre-opened with £500 each
            OperationsPage operationsPage = await SetupDemoAccountsAndNavigateToOperationsAsync(page);
            await operationsPage.WaitForBalanceAsync(ProjectionTimeout);

            // Act - Withdraw
            const decimal withdrawAmount = 50.00m;
            await operationsPage.EnterWithdrawAmountAsync(withdrawAmount);
            await operationsPage.ClickWithdrawAsync();
            await operationsPage.WaitForCommandSuccessAsync(ProjectionTimeout);
            await operationsPage.WaitForBalanceValueAsync("450.00", ProjectionTimeout);

            // Assert
            string? balanceText = await operationsPage.GetBalanceTextAsync();
            balanceText.Should().Contain("450.00");
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}