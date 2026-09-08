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
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        IPage page = await fixture.CreatePageAsync();
        try
        {
            AccountsPage accountsPage = new(page);

            // Act
            await accountsPage.NavigateAsync(fixture.GatewayBaseUri);
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
            OperationsPage operationsPage = await BankAccountScenario.PrepareAsync(fixture, page, ProjectionTimeout);
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
        fixture.GatewayBaseUri.Should().NotBe(new Uri("about:blank"), "the gateway should have a valid URL");
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
            OperationsPage operationsPage = await BankAccountScenario.PrepareAsync(fixture, page, ProjectionTimeout);

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
            OperationsPage operationsPage = await BankAccountScenario.PrepareAsync(fixture, page, ProjectionTimeout);

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
            balanceText.Should().Contain("450.00");
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}