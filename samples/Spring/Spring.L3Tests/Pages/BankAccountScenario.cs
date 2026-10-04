namespace MississippiSamples.Spring.L3Tests.Pages;

/// <summary>Prepares isolated demo accounts for a banking browser scenario.</summary>
internal static class BankAccountScenario
{
    /// <summary>Initializes demo accounts and opens their operations page.</summary>
    /// <param name="fixture">The shared browser fixture.</param>
    /// <param name="page">The isolated test page.</param>
    /// <param name="timeout">The timeout for application-state assertions, in milliseconds.</param>
    /// <returns>The operations page after its SignalR connection is established.</returns>
    public static async Task<OperationsPage> PrepareAsync(
        SpringBrowserFixture fixture,
        IPage page,
        float timeout
    )
    {
        AccountsPage accountsPage = new(page);
        await accountsPage.NavigateAsync(fixture.GatewayBaseUri);
        await accountsPage.WaitForConnectionStatusAsync("Connected", timeout);
        await accountsPage.ClickInitializeDemoAccountsAsync();
        await accountsPage.WaitForDemoAccountsInitializedAsync(timeout);
        await accountsPage.ClickGoToOperationsAsync();
        OperationsPage operationsPage = new(page);
        await operationsPage.WaitForConnectionStatusAsync("Connected", timeout);
        return operationsPage;
    }
}