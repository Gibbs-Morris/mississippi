using System.Collections.Generic;
using System.Globalization;
using System.Threading;

using MississippiSamples.Spring.L3Tests.Pages;

using static Microsoft.Playwright.Assertions;


namespace MississippiSamples.Spring.L3Tests;

/// <summary>Verifies failed account reads through real generated commands and projection fetches.</summary>
[Collection(SpringBrowserCollectionDefinition.Name)]
public sealed class AccountProjectionReadStatesBrowserTests
{
    private const float ProjectionTimeout = 120_000;

    /// <summary>Initializes a new instance of the <see cref="AccountProjectionReadStatesBrowserTests" /> class.</summary>
    /// <param name="fixture">The shared browser and real Spring deployment.</param>
    public AccountProjectionReadStatesBrowserTests(
        SpringBrowserFixture fixture
    ) =>
        Fixture = fixture;

    private SpringBrowserFixture Fixture { get; }

    private static async Task ActivateLinkAsync(
        IPage page,
        string label,
        bool phone
    )
    {
        ILocator link = page.GetByRole(
            AriaRole.Link,
            new()
            {
                Name = label,
                Exact = true,
            });
        if (phone)
        {
            await link.TapAsync();
        }
        else
        {
            await link.FocusAsync();
            await link.PressAsync("Enter");
        }
    }

    private static async Task RevealReadErrorAsync(
        IPage page,
        ILocator alert,
        bool phone
    )
    {
        ILocator details = alert.Locator("details");
        ILocator summary = details.Locator("summary");
        await Expect(summary).ToHaveTextAsync("Read error details");
        await Expect(details.Locator("p")).ToBeHiddenAsync();
        Assert.True(await summary.EvaluateAsync<double>("element => element.getBoundingClientRect().height") >= 44);
        if (phone)
        {
            await summary.TapAsync();
        }
        else
        {
            await summary.FocusAsync();
            await summary.PressAsync("Enter");
            Assert.True(await summary.EvaluateAsync<bool>("element => element === document.activeElement"));
            Assert.True(
                await summary.EvaluateAsync<double>(
                    "element => Number.parseFloat(getComputedStyle(element).outlineWidth)") >
                0);
        }

        await Expect(details.Locator("p")).ToBeVisibleAsync();
        await Expect(details.Locator("p")).ToContainTextAsync("503");
        Assert.True(
            await page.EvaluateAsync<bool>(
                "document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
    }

    private async Task RunJourneyAsync(
        int viewportWidth,
        string name,
        Func<IPage, Task> journey
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
            await journey(page);
            Assert.Empty(pageErrors);
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
                    "Account read journey and browser evidence capture both failed.",
                    failure,
                    captureFailure);
            }

            throw;
        }

        await SpringBrowserFixture.SaveBrowserArtifactsAsync(page, name);
    }

    /// <summary>A cold or cached failed account read cannot claim a healthy value, empty ledger or pending read.</summary>
    /// <param name="viewportWidth">The phone or desktop width.</param>
    /// <param name="loadCachedData">Whether to observe a real balance and transaction before the failed reads.</param>
    /// <returns>The asynchronous browser regression.</returns>
    [Theory]
    [InlineData(320, false)]
    [InlineData(390, false)]
    [InlineData(1440, false)]
    [InlineData(320, true)]
    [InlineData(390, true)]
    [InlineData(1440, true)]
    public async Task FailedAccountReadsHideMissingEmptyAndCachedOutcomesAsync(
        int viewportWidth,
        bool loadCachedData
    )
    {
        bool phone = viewportWidth < 768;
        string name = "account-reads-error-" +
                      (loadCachedData ? "cached-" : "cold-") +
                      viewportWidth.ToString(CultureInfo.InvariantCulture);
        await RunJourneyAsync(
            viewportWidth,
            name,
            async page =>
            {
                AccountsPage accounts = new(page);
                await accounts.NavigateAsync(Fixture.GatewayBaseUri);
                await accounts.WaitForConnectionStatusAsync("Connected", ProjectionTimeout);
                await accounts.ClickInitializeDemoAccountsAsync();
                await accounts.WaitForDemoAccountsInitializedAsync(ProjectionTimeout);
                await Expect(
                        page.GetByRole(
                            AriaRole.Region,
                            new()
                            {
                                Name = "Banking responses · this browser",
                                Exact = true,
                            }))
                    .ToContainTextAsync(
                        "2 accepted",
                        new()
                        {
                            Timeout = ProjectionTimeout,
                        });
                string accountId = await page.Locator("#demo-account-a-id").InnerTextAsync();
                string otherAccountId = await page.Locator("#demo-account-b-id").InnerTextAsync();
                string operationsUri = await page.GetByRole(
                                               AriaRole.Link,
                                               new()
                                               {
                                                   Name = "Go to Operations",
                                               })
                                           .GetAttributeAsync("href") ??
                                       throw new InvalidOperationException("The demo pair link is missing.");
                OperationsPage operations = new(page);
                double documentOrigin = await page.EvaluateAsync<double>("performance.timeOrigin");
                if (loadCachedData)
                {
                    await accounts.ClickGoToOperationsAsync();
                    await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout);
                    await operations.EnterDepositAmountAsync(25m);
                    await operations.ClickDepositAsync();
                    await operations.WaitForBalanceValueAsync("525.00", ProjectionTimeout);
                    await Expect(page.Locator("#account-a-operations-panel .spring-ledger tbody tr"))
                        .ToContainTextAsync("£25.00");
                    await SpringScreenshotEvidence.SaveAsync(page, name + "-observed");
                    await ActivateLinkAsync(page, "Start here", phone);
                    await Expect(page.Locator("#welcome-heading")).ToBeVisibleAsync();
                }

                int balanceFailures = 0;
                int ledgerFailures = 0;

                // Only the actual Account A HTTP reads fail; no projection data is fabricated.
                await page.RouteAsync(
                    "**/api/projections/bank-account-balance/" + accountId + "**",
                    async route =>
                    {
                        Interlocked.Increment(ref balanceFailures);
                        await route.FulfillAsync(
                            new()
                            {
                                Status = 503,
                                ContentType = "text/plain",
                                Body = "Controlled balance read failure.",
                            });
                    });
                await page.RouteAsync(
                    "**/api/projections/bank-account-ledger/" + accountId + "**",
                    async route =>
                    {
                        Interlocked.Increment(ref ledgerFailures);
                        await route.FulfillAsync(
                            new()
                            {
                                Status = 503,
                                ContentType = "text/plain",
                                Body = "Controlled ledger read failure.",
                            });
                    });
                if (loadCachedData)
                {
                    await ActivateLinkAsync(page, "Move money", phone);
                }
                else
                {
                    await accounts.ClickGoToOperationsAsync();
                }

                ILocator panel = page.Locator("#account-a-operations-panel");
                ILocator balance = panel.Locator(".spring-balance");
                ILocator ledger = panel.Locator(".spring-ledger");
                ILocator balanceAlert = balance.GetByRole(AriaRole.Alert);
                ILocator ledgerAlert = ledger.GetByRole(AriaRole.Alert);
                await Expect(balanceAlert).ToContainTextAsync("The account could not be read");
                await Expect(balanceAlert)
                    .ToContainTextAsync("does not establish the current balance or whether the account is open");
                await Expect(balanceAlert).ToContainTextAsync("503");
                await Expect(balance.Locator("output, .spring-holder, .spring-projection-meta, [role='status']"))
                    .ToHaveCountAsync(0);
                await Expect(
                        balance.GetByText(
                            "No account data received",
                            new()
                            {
                                Exact = true,
                            }))
                    .ToHaveCountAsync(0);
                await Expect(ledgerAlert).ToContainTextAsync("The ledger could not be read");
                await Expect(ledgerAlert).ToContainTextAsync("does not establish which transactions have committed");
                await Expect(ledgerAlert).ToContainTextAsync("503");
                await Expect(ledger.Locator("table, [role='status']")).ToHaveCountAsync(0);
                await Expect(
                        ledger.GetByText(
                            "No transactions yet",
                            new()
                            {
                                Exact = false,
                            }))
                    .ToHaveCountAsync(0);
                await Expect(
                        ledger.GetByText(
                            "No ledger data received",
                            new()
                            {
                                Exact = false,
                            }))
                    .ToHaveCountAsync(0);
                await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout, "B");
                await operations.WaitForCommandSuccessAsync(ProjectionTimeout);
                Assert.True(
                    (balanceFailures > 0) && (ledgerFailures > 0),
                    "both failures must reach real projection fetches");
                Assert.Equal(documentOrigin, await page.EvaluateAsync<double>("performance.timeOrigin"));
                await Expect(panel.Locator("h2 code")).ToHaveTextAsync(accountId);
                await Expect(page.Locator("#account-b-operations-panel h2 code")).ToHaveTextAsync(otherAccountId);
                await Expect(page.Locator(".spring-share-pair a"))
                    .ToHaveAttributeAsync("href", new Uri(Fixture.GatewayBaseUri, operationsUri).ToString());
                if (!loadCachedData)
                {
                    Assert.Equal(new Uri(Fixture.GatewayBaseUri, operationsUri).Query, new Uri(page.Url).Query);
                }

                Assert.True(
                    await page.EvaluateAsync<bool>(
                        "document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
                await Expect(balanceAlert.Locator("details p")).ToBeHiddenAsync();
                await Expect(ledgerAlert.Locator("details p")).ToBeHiddenAsync();
                await SpringScreenshotEvidence.SaveAsync(page, name);
                await RevealReadErrorAsync(page, balanceAlert, phone);
                await RevealReadErrorAsync(page, ledgerAlert, phone);
                await SpringScreenshotEvidence.SaveAsync(page, name + "-details");
            });
    }

    /// <summary>A failed saga read remains unknown even after an accepted start and real account updates.</summary>
    /// <param name="viewportWidth">The phone or desktop width.</param>
    /// <returns>The asynchronous browser regression.</returns>
    [Theory]
    [InlineData(320)]
    [InlineData(390)]
    [InlineData(1440)]
    public async Task FailedTransferReadDoesNotClaimPendingOrFinalSagaOutcomeAsync(
        int viewportWidth
    )
    {
        bool phone = viewportWidth < 768;
        string name = "transfer-read-error-" + viewportWidth.ToString(CultureInfo.InvariantCulture);
        await RunJourneyAsync(
            viewportWidth,
            name,
            async page =>
            {
                OperationsPage operations = await BankAccountScenario.PrepareAsync(Fixture, page, ProjectionTimeout);
                await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout);
                await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout, "B");
                int intercepted = 0;
                await page.RouteAsync(
                    "**/api/projections/money-transfer-status/**",
                    async route =>
                    {
                        Interlocked.Increment(ref intercepted);
                        await route.FulfillAsync(
                            new()
                            {
                                Status = 503,
                                ContentType = "text/plain",
                                Body = "Controlled transfer read failure.",
                            });
                    });
                await operations.EnterTransferAmountAsync(25m);
                await operations.ClickStartTransferAsync();
                ILocator status = operations.GetTransferStatus();
                ILocator alert = status.GetByRole(AriaRole.Alert);
                await Expect(alert).ToContainTextAsync("Transfer status could not be read");
                await Expect(alert).ToContainTextAsync("This failed read does not establish the saga outcome");
                await Expect(alert).ToContainTextAsync("503");
                await Expect(status).ToHaveAttributeAsync("data-state", "error");
                await Expect(status).ToContainTextAsync("Transfer saga ID:");
                await Expect(
                        status.GetByText(
                            "Waiting for transfer status",
                            new()
                            {
                                Exact = false,
                            }))
                    .ToHaveCountAsync(0);
                await Expect(
                        status.GetByText(
                            "Phase:",
                            new()
                            {
                                Exact = false,
                            }))
                    .ToHaveCountAsync(0);
                await Expect(
                        status.GetByText(
                            "Last completed step:",
                            new()
                            {
                                Exact = false,
                            }))
                    .ToHaveCountAsync(0);
                await Expect(
                        page.GetByRole(
                                AriaRole.Region,
                                new()
                                {
                                    Name = "Transfer-start responses · this browser",
                                    Exact = true,
                                })
                            .GetByRole(AriaRole.Status))
                    .ToContainTextAsync(
                        "Latest response: accepted.",
                        new()
                        {
                            Timeout = ProjectionTimeout,
                        });
                await operations.WaitForBalanceValueAsync("475.00", ProjectionTimeout);
                await operations.WaitForBalanceValueAsync("525.00", ProjectionTimeout, "B");
                await Expect(page.Locator("#account-a-operations-panel .spring-ledger tbody tr"))
                    .ToContainTextAsync("Withdrawal");
                await Expect(page.Locator("#account-b-operations-panel .spring-ledger tbody tr"))
                    .ToContainTextAsync("Deposit");
                Assert.True(intercepted > 0, "the failure must reach the actual transfer projection fetch");
                Assert.True(
                    await page.EvaluateAsync<bool>(
                        "document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
                await Expect(alert.Locator("details p")).ToBeHiddenAsync();
                await SpringScreenshotEvidence.SaveAsync(page, name);
                await RevealReadErrorAsync(page, alert, phone);
                await SpringScreenshotEvidence.SaveAsync(page, name + "-details");
            });
    }
}