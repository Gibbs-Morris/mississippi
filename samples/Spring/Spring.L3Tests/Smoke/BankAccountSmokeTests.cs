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
        Assert.True(Fixture.IsInitialized, "fixture must be initialized");
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
            Assert.True(hasStyles, "the generated CSS isolation bundle must load successfully");

            // Wait for projection to show the balance via SignalR
            await operationsPage.WaitForBalanceAsync(ProjectionTimeout);

            // Assert - Verify initial state (demo accounts start with £500)
            string? balanceText = await operationsPage.GetBalanceTextAsync();
            Assert.Contains("500.00", balanceText, StringComparison.Ordinal);
            string? holderText = await operationsPage.GetHolderNameTextAsync();
            Assert.False(string.IsNullOrEmpty(holderText), "holder name should be displayed");
            string? statusText = await operationsPage.GetStatusTextAsync();
            Assert.Contains("Open", statusText, StringComparison.Ordinal);

            // Act - Deposit funds
            const decimal depositAmount = 50.00m;
            await operationsPage.EnterDepositAmountAsync(depositAmount);
            await operationsPage.ClickDepositAsync();
            await operationsPage.WaitForCommandSuccessAsync(ProjectionTimeout);
            await operationsPage.WaitForBalanceValueAsync("550.00", ProjectionTimeout);
            balanceText = await operationsPage.GetBalanceTextAsync();
            Assert.Contains("550.00", balanceText, StringComparison.Ordinal);

            // Act - Withdraw funds
            const decimal withdrawAmount = 25.00m;
            await operationsPage.EnterWithdrawAmountAsync(withdrawAmount);
            await operationsPage.ClickWithdrawAsync();
            await operationsPage.WaitForCommandSuccessAsync(ProjectionTimeout);
            await operationsPage.WaitForBalanceValueAsync("525.00", ProjectionTimeout);

            // Assert - Final balance
            balanceText = await operationsPage.GetBalanceTextAsync();
            Assert.Contains("525.00", balanceText, StringComparison.Ordinal);
        }
        catch (Exception testException)
        {
            try
            {
                await SpringBrowserFixture.SaveBrowserArtifactsAsync(page);
            }
            catch (Exception artifactException)
            {
                throw new AggregateException(
                    "Banking journey and artifact capture both failed.",
                    testException,
                    artifactException);
            }

            throw;
        }

        await SpringBrowserFixture.SaveBrowserArtifactsAsync(page);
    }
}