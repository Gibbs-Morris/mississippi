using MississippiSamples.Spring.L3Tests.Pages;


namespace MississippiSamples.Spring.L3Tests.Smoke;

/// <summary>Provides the critical banking journey selected by the L3 smoke suite.</summary>
[Collection(SpringBrowserCollectionDefinition.Name)]
public sealed class BankAccountSmokeTests
{
    private const float ProjectionTimeout = 120_000;

    /// <summary>Initializes a new instance of the <see cref="BankAccountSmokeTests" /> class.</summary>
    /// <param name="fixture">The shared L3 browser fixture.</param>
    public BankAccountSmokeTests(
        SpringBrowserFixture fixture
    ) =>
        Fixture = fixture;

    private SpringBrowserFixture Fixture { get; }

    /// <summary>
    ///     Verifies the complete bank account flow via UI: open, deposit, withdraw,
    ///     and confirms the balance updates in real-time via SignalR projection.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    [Trait("Category", "Smoke")]
    public async Task CompleteBankAccountFlowShouldUpdateProjectionViaSignalR()
    {
        // Arrange
        Fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        IPage page = await Fixture.CreatePageAsync();
        try
        {
            await page.Context.Tracing.StartAsync(
                new()
                {
                    Screenshots = true,
                    Snapshots = true,
                    Sources = true,
                });

            // Demo accounts are pre-opened with £500 each
            OperationsPage operationsPage = await BankAccountScenario.PrepareAsync(Fixture, page, ProjectionTimeout);
            bool hasStyles = await page.Locator("link[rel='stylesheet']")
                .EvaluateAsync<bool>("link => link.sheet !== null && link.sheet.cssRules.length > 0");
            hasStyles.Should().BeTrue("the generated CSS isolation bundle must load successfully");

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
            await SpringBrowserFixture.SaveBrowserArtifactsAsync(page);
        }
    }
}