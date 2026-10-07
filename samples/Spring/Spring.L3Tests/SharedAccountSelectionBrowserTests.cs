using System.Collections.Generic;
using System.Globalization;

using MississippiSamples.Spring.L3Tests.Pages;

using static Microsoft.Playwright.Assertions;


namespace MississippiSamples.Spring.L3Tests;

/// <summary>Verifies real live views when both account panels select the same account.</summary>
[Collection(SpringBrowserCollectionDefinition.Name)]
public sealed class SharedAccountSelectionBrowserTests
{
    private const float ProjectionTimeout = 120_000;

    /// <summary>Initializes a new instance of the <see cref="SharedAccountSelectionBrowserTests" /> class.</summary>
    /// <param name="fixture">The shared Spring browser fixture.</param>
    public SharedAccountSelectionBrowserTests(
        SpringBrowserFixture fixture
    ) =>
        Fixture = fixture;

    private SpringBrowserFixture Fixture { get; }

    /// <summary>Verify that an equal-ID shared link keeps both real balances and ledgers live after switching a panel.</summary>
    /// <param name="viewportWidth">The phone or desktop width.</param>
    /// <returns>The asynchronous shared-account browser regression.</returns>
    [Theory]
    [InlineData(390)]
    [InlineData(1440)]
    public async Task SharedAccountPairKeepsBothViewsLiveAsync(
        int viewportWidth
    )
    {
        Assert.True(Fixture.IsInitialized, "fixture must be initialized");
        bool phone = viewportWidth < 768;
        IPage page = await Fixture.CreatePageAsync(
            new()
            {
                ViewportSize = new()
                {
                    Width = viewportWidth,
                    Height = phone ? 844 : 900,
                },
                IsMobile = phone,
                HasTouch = phone,
            });
        string name = "shared-account-pair-" + viewportWidth.ToString(CultureInfo.InvariantCulture);
        List<string> pageErrors = [];
        page.PageError += (_, error) => pageErrors.Add(error);
        try
        {
            await page.Context.Tracing.StartAsync(
                new()
                {
                    Screenshots = true,
                    Snapshots = true,
                    Sources = true,
                });
            OperationsPage operations = await BankAccountScenario.PrepareAsync(Fixture, page, ProjectionTimeout);
            await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout);
            string accountId = await page.Locator("#account-a-operations-panel h2 code").InnerTextAsync();
            string encodedId = Uri.EscapeDataString(accountId);
            await page.GotoAsync(
                new Uri(Fixture.GatewayBaseUri, "/operations?a=" + encodedId + "&b=" + encodedId).ToString());
            await Expect(page.Locator("#account-a-operations-panel h2 code")).ToHaveTextAsync(accountId);
            await Expect(page.Locator("#account-b-operations-panel h2 code")).ToHaveTextAsync(accountId);
            await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout);
            await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout, "B");
            await operations.EnterDepositAmountAsync(25m);
            ILocator deposit = page.Locator("#account-a-operations-panel")
                .GetByRole(
                    AriaRole.Button,
                    new()
                    {
                        Name = "Deposit £",
                        Exact = true,
                    });
            if (phone)
            {
                await deposit.TapAsync();
            }
            else
            {
                await deposit.FocusAsync();
                await deposit.PressAsync("Enter");
            }

            await operations.WaitForBalanceValueAsync("525.00", ProjectionTimeout);
            await operations.WaitForBalanceValueAsync("525.00", ProjectionTimeout, "B");
            await Expect(page.Locator("#account-a-operations-panel tbody tr")).ToHaveCountAsync(1);
            await Expect(page.Locator("#account-b-operations-panel tbody tr")).ToHaveCountAsync(1);
            ILocator switchAccount = page.GetByRole(
                AriaRole.Button,
                new()
                {
                    Name = "Switch Account A account",
                    Exact = true,
                });
            if (phone)
            {
                await switchAccount.TapAsync();
            }
            else
            {
                await switchAccount.FocusAsync();
                await switchAccount.PressAsync("Enter");
            }

            await Expect(page.Locator("#custom-account-a")).ToHaveValueAsync(string.Empty);
            await Expect(page.Locator("#custom-account-b")).ToHaveValueAsync(accountId);
            await page.GetByLabel(
                    "Account A ID",
                    new()
                    {
                        Exact = true,
                    })
                .FillAsync(accountId);
            ILocator useAccounts = page.GetByRole(
                AriaRole.Button,
                new()
                {
                    Name = "Use these accounts",
                    Exact = true,
                });
            if (phone)
            {
                await useAccounts.TapAsync();
            }
            else
            {
                await useAccounts.FocusAsync();
                await useAccounts.PressAsync("Enter");
            }

            await operations.WaitForBalanceValueAsync("525.00", ProjectionTimeout);
            await operations.WaitForBalanceValueAsync("525.00", ProjectionTimeout, "B");
            await operations.EnterDepositAmountAsync(25m);
            await operations.ClickDepositAsync();
            await operations.WaitForBalanceValueAsync("550.00", ProjectionTimeout);
            await operations.WaitForBalanceValueAsync("550.00", ProjectionTimeout, "B");
            await Expect(page.Locator("#account-a-operations-panel tbody tr")).ToHaveCountAsync(2);
            await Expect(page.Locator("#account-b-operations-panel tbody tr")).ToHaveCountAsync(2);
            Assert.Empty(pageErrors);
            Assert.True(
                await page.EvaluateAsync<bool>(
                    "document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
            await SpringScreenshotEvidence.SaveAsync(page, name);
        }
        catch (Exception failure)
        {
            try
            {
                await SpringBrowserFixture.SaveBrowserArtifactsAsync(page, name);
            }
            catch (Exception captureFailure)
            {
                throw new AggregateException(
                    "Shared-account journey failed and browser evidence capture also failed.",
                    failure,
                    captureFailure);
            }

            throw;
        }

        await SpringBrowserFixture.SaveBrowserArtifactsAsync(page, name);
    }
}