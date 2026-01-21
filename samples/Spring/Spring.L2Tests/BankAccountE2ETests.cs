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
    ///     Timeout for waiting on SignalR projection updates (10 seconds).
    /// </summary>
    private const float ProjectionTimeout = 10_000;

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
            IndexPage indexPage = new(page);
            await indexPage.NavigateAsync(fixture.ServerBaseUri);
            string accountId = $"e2e-{Guid.NewGuid():N}";
            const string holderName = "E2E Test User";
            const decimal initialDeposit = 100.00m;
            const decimal depositAmount = 50.00m;
            const decimal withdrawAmount = 25.00m;
            decimal expectedFinalBalance = (initialDeposit + depositAmount) - withdrawAmount;

            // Act - Set account ID
            await indexPage.SetAccountAsync(accountId);
            string? accountHeader = await indexPage.GetAccountHeaderAsync();
            accountHeader.Should().Contain(accountId, "account header should show the set account ID");

            // Act - Open account with initial deposit
            await indexPage.EnterHolderNameAsync(holderName);
            await indexPage.EnterInitialDepositAsync(initialDeposit);
            await indexPage.ClickOpenAccountAsync();

            // Wait for projection to show the balance via SignalR
            await indexPage.WaitForBalanceAsync(ProjectionTimeout);

            // Assert - Verify initial state
            string? balanceText = await indexPage.GetBalanceTextAsync();
            balanceText.Should().Contain("$100.00", "initial balance should be $100.00");
            string? holderText = await indexPage.GetHolderNameTextAsync();
            holderText.Should().Contain(holderName, "holder name should be displayed");
            string? statusText = await indexPage.GetStatusTextAsync();
            statusText.Should().Contain("Open", "account status should be Open");

            // Act - Deposit funds
            await indexPage.EnterDepositAmountAsync(depositAmount);
            await indexPage.ClickDepositAsync();
            await indexPage.WaitForBalanceValueAsync("$150.00", ProjectionTimeout);
            balanceText = await indexPage.GetBalanceTextAsync();
            balanceText.Should().Contain("$150.00", "balance should be $150.00 after deposit");

            // Act - Withdraw funds
            await indexPage.EnterWithdrawAmountAsync(withdrawAmount);
            await indexPage.ClickWithdrawAsync();
            await indexPage.WaitForBalanceValueAsync("$125.00", ProjectionTimeout);

            // Assert - Final balance
            balanceText = await indexPage.GetBalanceTextAsync();
            balanceText.Should()
                .Contain($"${expectedFinalBalance:F2}", "final balance should reflect all transactions");
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
            IndexPage indexPage = new(page);
            await indexPage.NavigateAsync(fixture.ServerBaseUri);
            string accountId = $"deposit-{Guid.NewGuid():N}";
            const string holderName = "Deposit Test";
            const decimal initialDeposit = 50.00m;
            const decimal additionalDeposit = 75.00m;

            // Act - Set up account
            await indexPage.SetAccountAsync(accountId);
            await indexPage.EnterHolderNameAsync(holderName);
            await indexPage.EnterInitialDepositAsync(initialDeposit);
            await indexPage.ClickOpenAccountAsync();
            await indexPage.WaitForBalanceAsync(ProjectionTimeout);

            // Act - Deposit
            await indexPage.EnterDepositAmountAsync(additionalDeposit);
            await indexPage.ClickDepositAsync();
            await indexPage.WaitForBalanceValueAsync("$125.00", ProjectionTimeout);

            // Assert
            string? balanceText = await indexPage.GetBalanceTextAsync();
            balanceText.Should().Contain("$125.00");
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
    ///     Verifies the index page loads and displays the correct title.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task IndexPageShouldDisplayTitle()
    {
        // Arrange
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        IPage page = await fixture.CreatePageAsync();
        try
        {
            IndexPage indexPage = new(page);

            // Act
            await indexPage.NavigateAsync(fixture.ServerBaseUri);
            string? title = await indexPage.GetTitleAsync();

            // Assert
            title.Should().Be("Bank Account Operations");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    ///     Verifies that opening an account displays the balance projection.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task OpenAccountShouldDisplayBalanceProjection()
    {
        // Arrange
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        IPage page = await fixture.CreatePageAsync();
        try
        {
            IndexPage indexPage = new(page);
            await indexPage.NavigateAsync(fixture.ServerBaseUri);
            string accountId = $"open-{Guid.NewGuid():N}";
            const string holderName = "Jane Doe";
            const decimal initialDeposit = 250.00m;

            // Act - Set account and open it
            await indexPage.SetAccountAsync(accountId);
            await indexPage.EnterHolderNameAsync(holderName);
            await indexPage.EnterInitialDepositAsync(initialDeposit);
            await indexPage.ClickOpenAccountAsync();

            // Wait for projection update via SignalR
            await indexPage.WaitForBalanceAsync(ProjectionTimeout);

            // Assert
            string? balanceText = await indexPage.GetBalanceTextAsync();
            balanceText.Should().Contain("$250.00");
            string? holderText = await indexPage.GetHolderNameTextAsync();
            holderText.Should().Contain(holderName);
            string? statusText = await indexPage.GetStatusTextAsync();
            statusText.Should().Contain("Open");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    ///     Verifies that setting an account ID displays the account header.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task SetAccountShouldDisplayAccountHeader()
    {
        // Arrange
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        IPage page = await fixture.CreatePageAsync();
        try
        {
            IndexPage indexPage = new(page);
            await indexPage.NavigateAsync(fixture.ServerBaseUri);
            const string accountId = "test-account-123";

            // Act
            await indexPage.SetAccountAsync(accountId);

            // Assert
            string? accountHeader = await indexPage.GetAccountHeaderAsync();
            accountHeader.Should().Contain(accountId);
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
            IndexPage indexPage = new(page);
            await indexPage.NavigateAsync(fixture.ServerBaseUri);
            string accountId = $"withdraw-{Guid.NewGuid():N}";
            const string holderName = "Withdraw Test";
            const decimal initialDeposit = 200.00m;
            const decimal withdrawAmount = 50.00m;

            // Act - Set up account
            await indexPage.SetAccountAsync(accountId);
            await indexPage.EnterHolderNameAsync(holderName);
            await indexPage.EnterInitialDepositAsync(initialDeposit);
            await indexPage.ClickOpenAccountAsync();
            await indexPage.WaitForBalanceAsync(ProjectionTimeout);

            // Act - Withdraw
            await indexPage.EnterWithdrawAmountAsync(withdrawAmount);
            await indexPage.ClickWithdrawAsync();
            await indexPage.WaitForBalanceValueAsync("$150.00", ProjectionTimeout);

            // Assert
            string? balanceText = await indexPage.GetBalanceTextAsync();
            balanceText.Should().Contain("$150.00");
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}