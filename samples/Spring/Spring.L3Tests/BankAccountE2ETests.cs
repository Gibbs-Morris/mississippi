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
            Assert.Contains("500.00", balanceText, StringComparison.Ordinal);
            string? holderText = await operationsPage.GetHolderNameTextAsync();
            Assert.False(string.IsNullOrEmpty(holderText), "holder name should be displayed");
            string? statusText = await operationsPage.GetStatusTextAsync();
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
            Assert.Contains("450.00", balanceText, StringComparison.Ordinal);
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}