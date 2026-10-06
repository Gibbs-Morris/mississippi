using System.Globalization;
using System.Threading;

using MississippiSamples.Spring.L3Tests.Pages;

using static Microsoft.Playwright.Assertions;


namespace MississippiSamples.Spring.L3Tests;

/// <summary>Verifies task completion and observed outcomes through the flagship demo.</summary>
[Collection(SpringBrowserCollectionDefinition.Name)]
public sealed class FlagshipJourneysTests
{
    private const float ProjectionTimeout = 120_000;

    /// <summary>Initializes a new instance of the <see cref="FlagshipJourneysTests" /> class.</summary>
    /// <param name="fixture">The shared Spring browser fixture.</param>
    public FlagshipJourneysTests(
        SpringBrowserFixture fixture
    ) =>
        Fixture = fixture;

    private SpringBrowserFixture Fixture { get; }

    private static ILocator AccountA(
        IPage page
    ) =>
        page.Locator("#account-a-operations-panel");

    /// <summary>Switching one account retains the other ID and keeps the demo shortcut bound to its displayed pair.</summary>
    /// <returns>The asynchronous account-selection journey.</returns>
    [Fact]
    public async Task AccountSwitchRetainsOtherAccountAndDemoShortcutAsync()
    {
        Assert.True(Fixture.IsInitialized, "fixture must be initialized");
        IPage page = await Fixture.CreatePageAsync();
        try
        {
            OperationsPage operations = await BankAccountScenario.PrepareAsync(Fixture, page, ProjectionTimeout);
            await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout);
            await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout, "B");
            string demoA = await AccountA(page).Locator("h2 code").InnerTextAsync();
            string demoB = await page.Locator("#account-b-operations-panel h2 code").InnerTextAsync();
            await AccountA(page)
                .GetByRole(
                    AriaRole.Button,
                    new()
                    {
                        Name = "Switch Account A account",
                        Exact = true,
                    })
                .ClickAsync();
            await Expect(
                    page.GetByLabel(
                        "Account A ID",
                        new()
                        {
                            Exact = true,
                        }))
                .ToHaveValueAsync(string.Empty);
            await Expect(
                    page.GetByLabel(
                        "Account B ID",
                        new()
                        {
                            Exact = true,
                        }))
                .ToHaveValueAsync(demoB);
            string replacement = $"replacement-{Guid.NewGuid():N}";
            await page.GetByLabel(
                    "Account A ID",
                    new()
                    {
                        Exact = true,
                    })
                .FillAsync(replacement);
            await page.GetByRole(
                    AriaRole.Button,
                    new()
                    {
                        Name = "Use these accounts",
                        Exact = true,
                    })
                .PressAsync("Enter");
            await Expect(AccountA(page).Locator("h2 code")).ToHaveTextAsync(replacement);
            await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout, "B");
            await Expect(
                    AccountA(page)
                        .GetByRole(
                            AriaRole.Button,
                            new()
                            {
                                Name = "Open Account",
                                Exact = true,
                            }))
                .ToBeVisibleAsync();
            await page.GetByRole(
                    AriaRole.Link,
                    new()
                    {
                        Name = "Prepare accounts",
                        Exact = true,
                    })
                .ClickAsync();
            await Expect(
                    page.GetByLabel(
                        "Account A ID",
                        new()
                        {
                            Exact = true,
                        }))
                .ToHaveValueAsync(replacement);
            await Expect(
                    page.GetByLabel(
                        "Account B ID",
                        new()
                        {
                            Exact = true,
                        }))
                .ToHaveValueAsync(demoB);
            ILocator demoShortcut = page.GetByRole(
                AriaRole.Link,
                new()
                {
                    Name = "Go to Operations",
                    Exact = true,
                });
            await Expect(demoShortcut)
                .ToHaveAttributeAsync(
                    "href",
                    $"/operations?a={Uri.EscapeDataString(demoA)}&b={Uri.EscapeDataString(demoB)}");
            await demoShortcut.ClickAsync();
            await Expect(AccountA(page).Locator("h2 code")).ToHaveTextAsync(demoA);
            await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout);
            await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout, "B");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>Every preserved quick and burst control sends its original number and amount of commands.</summary>
    /// <returns>The asynchronous burst journey.</returns>
    [Fact]
    public async Task AllQuickAndBurstActionsPreserveTotalsAndRetainedLedgerAsync()
    {
        Assert.True(Fixture.IsInitialized, "fixture must be initialized");
        IPage page = await Fixture.CreatePageAsync();
        int acceptedBankingRequests = 0;
        page.Response += (_, response) =>
        {
            if ((response.Request.Method == "POST") &&
                response.Url.Contains("/api/aggregates/bank-account/", StringComparison.Ordinal) &&
                (response.Status == 200))
            {
                Interlocked.Increment(ref acceptedBankingRequests);
            }
        };
        try
        {
            OperationsPage operations = await BankAccountScenario.PrepareAsync(Fixture, page, ProjectionTimeout);
            await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout);
            await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout, "B");
            await AccountA(page)
                .GetByText(
                    "Quick actions & command bursts",
                    new()
                    {
                        Exact = true,
                    })
                .ClickAsync();
            ILocator deposits = AccountA(page)
                .GetByRole(
                    AriaRole.Group,
                    new()
                    {
                        Name = "Deposit into Account A",
                        Exact = true,
                    });
            ILocator withdrawals = AccountA(page)
                .GetByRole(
                    AriaRole.Group,
                    new()
                    {
                        Name = "Withdraw from Account A",
                        Exact = true,
                    });
            await deposits.GetByRole(
                    AriaRole.Button,
                    new()
                    {
                        Name = "+£100",
                        Exact = true,
                    })
                .ClickAsync();
            await operations.WaitForBalanceValueAsync("600.00", ProjectionTimeout);
            await deposits.GetByRole(
                    AriaRole.Button,
                    new()
                    {
                        Name = "20×£5",
                        Exact = true,
                    })
                .ClickAsync();
            await operations.WaitForBalanceValueAsync("700.00", ProjectionTimeout);
            await deposits.GetByRole(
                    AriaRole.Button,
                    new()
                    {
                        Name = "200×£10",
                        Exact = true,
                    })
                .ClickAsync();
            await operations.WaitForBalanceValueAsync("2,700.00", ProjectionTimeout);
            await withdrawals.GetByRole(
                    AriaRole.Button,
                    new()
                    {
                        Name = "200×£10",
                        Exact = true,
                    })
                .ClickAsync();
            await operations.WaitForBalanceValueAsync("700.00", ProjectionTimeout);
            await withdrawals.GetByRole(
                    AriaRole.Button,
                    new()
                    {
                        Name = "20×£5",
                        Exact = true,
                    })
                .ClickAsync();
            await operations.WaitForBalanceValueAsync("600.00", ProjectionTimeout);
            await withdrawals.GetByRole(
                    AriaRole.Button,
                    new()
                    {
                        Name = "-£100",
                        Exact = true,
                    })
                .ClickAsync();
            await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout);
            await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout, "B");
            await Expect(AccountA(page).Locator("tbody tr"))
                .ToHaveCountAsync(
                    20,
                    new()
                    {
                        Timeout = ProjectionTimeout,
                    });
            ILocator responses = page.GetByRole(
                AriaRole.Region,
                new()
                {
                    Name = "Banking responses · this browser",
                    Exact = true,
                });
            await Expect(
                    responses.GetByText(
                        "Command history · 200 request(s)",
                        new()
                        {
                            Exact = true,
                        }))
                .ToBeVisibleAsync();
            await Expect(responses.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0);
            await responses.Locator("summary").ClickAsync();
            await Expect(
                    responses.GetByText(
                        "200 accepted",
                        new()
                        {
                            Exact = false,
                        }))
                .ToBeVisibleAsync(
                    new()
                    {
                        Timeout = ProjectionTimeout,
                    });
            await Expect(
                    responses.GetByText(
                        "0 rejected.",
                        new()
                        {
                            Exact = false,
                        }))
                .ToBeVisibleAsync();
            Assert.Equal(444, acceptedBankingRequests);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>A phone user can start with the recommended task and verify a change in two independent browsers.</summary>
    /// <returns>The asynchronous browser journey.</returns>
    [Fact]
    public async Task FirstRunOnPhoneVerifiesLiveBalancesLedgerAndSharedViewAsync()
    {
        Assert.True(Fixture.IsInitialized, "fixture must be initialized");
        IPage page = await Fixture.CreatePageAsync();
        IPage observer = await Fixture.CreatePageAsync();
        try
        {
            await page.SetViewportSizeAsync(390, 844);
            await page.GotoAsync(Fixture.GatewayBaseUri.ToString());
            await Expect(page.Locator("h1")).ToBeFocusedAsync();
            await page.GetByRole(
                    AriaRole.Link,
                    new()
                    {
                        Name = "Start with two accounts",
                        Exact = true,
                    })
                .PressAsync("Enter");
            AccountsPage accounts = new(page);
            await accounts.WaitForConnectionStatusAsync("Connected", ProjectionTimeout);
            await page.GetByRole(
                    AriaRole.Button,
                    new()
                    {
                        Name = "Initialize demo accounts",
                        Exact = true,
                    })
                .PressAsync("Enter");
            await accounts.WaitForDemoAccountsInitializedAsync(ProjectionTimeout);
            await Expect(
                    page.GetByText(
                        "Next: check the live accounts",
                        new()
                        {
                            Exact = true,
                        }))
                .ToBeVisibleAsync();
            await page.GetByRole(
                    AriaRole.Link,
                    new()
                    {
                        Name = "Go to Operations",
                        Exact = true,
                    })
                .PressAsync("Enter");
            OperationsPage operations = new(page);
            await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout);
            await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout, "B");
            await observer.GotoAsync(page.Url);
            OperationsPage observedOperations = new(observer);
            await observedOperations.WaitForBalanceValueAsync("500.00", ProjectionTimeout);
            await operations.EnterDepositAmountAsync(25m);
            await operations.ClickDepositAsync();
            await operations.WaitForBalanceValueAsync("525.00", ProjectionTimeout);
            await observedOperations.WaitForBalanceValueAsync("525.00", ProjectionTimeout);
            await Expect(
                    AccountA(page)
                        .GetByRole(
                            AriaRole.Cell,
                            new()
                            {
                                Name = "£25.00",
                                Exact = true,
                            }))
                .ToBeVisibleAsync(
                    new()
                    {
                        Timeout = ProjectionTimeout,
                    });
            await Expect(
                    AccountA(observer)
                        .GetByRole(
                            AriaRole.Cell,
                            new()
                            {
                                Name = "Deposit",
                                Exact = true,
                            }))
                .ToBeVisibleAsync(
                    new()
                    {
                        Timeout = ProjectionTimeout,
                    });
            await operations.EnterTransferAmountAsync(12m);
            await page.GetByLabel(
                    "Account A transfer amount (£)",
                    new()
                    {
                        Exact = true,
                    })
                .FillAsync("12.");
            await Expect(
                    AccountA(page)
                        .GetByRole(
                            AriaRole.Button,
                            new()
                            {
                                Name = "Start Transfer",
                                Exact = true,
                            }))
                .ToBeDisabledAsync();
            await operations.EnterWithdrawAmountAsync(2_000m);
            await operations.ClickWithdrawAsync();
            ILocator responses = page.GetByRole(
                AriaRole.Region,
                new()
                {
                    Name = "Banking responses · this browser",
                    Exact = true,
                });
            await Expect(responses.GetByRole(AriaRole.Status)).ToContainTextAsync("Latest response: rejected.");
            await Expect(responses.GetByRole(AriaRole.Alert))
                .ToContainTextAsync(
                    "Insufficient",
                    new()
                    {
                        IgnoreCase = true,
                    });
            await operations.WaitForBalanceValueAsync("525.00", ProjectionTimeout);
            await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout, "B");
            await Expect(AccountA(page).Locator("tbody tr")).ToHaveCountAsync(1);
            await operations.EnterTransferAmountAsync(2_000m);
            await operations.ClickStartTransferAsync();
            await operations.WaitForTransferPhaseAsync("Compensated", ProjectionTimeout);
            await Expect(operations.GetTransferStatus())
                .ToContainTextAsync("No step completed. No compensating account action was required.");
            await operations.WaitForBalanceValueAsync("525.00", ProjectionTimeout);
            await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout, "B");
            await Expect(AccountA(page).Locator("tbody tr")).ToHaveCountAsync(1);
            Assert.True(
                await page.EvaluateAsync<bool>(
                    "document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
        }
        finally
        {
            await observer.CloseAsync();
            await page.CloseAsync();
        }
    }

    /// <summary>A committed high-value deposit produces a real flag in the separate live queue.</summary>
    /// <returns>The asynchronous investigation journey.</returns>
    [Fact]
    public async Task HighValueDepositAppearsInBalanceLedgerAndInvestigationQueueAsync()
    {
        Assert.True(Fixture.IsInitialized, "fixture must be initialized");
        IPage page = await Fixture.CreatePageAsync();
        try
        {
            OperationsPage operations = await BankAccountScenario.PrepareAsync(Fixture, page, ProjectionTimeout);
            await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout);
            string? header = await operations.GetAccountHeaderAsync();
            Assert.NotNull(header);
            string accountId = await AccountA(page).Locator("h2 code").InnerTextAsync();
            await operations.EnterDepositAmountAsync(10_001m);
            await operations.ClickDepositAsync();
            await operations.WaitForBalanceValueAsync("10,501.00", ProjectionTimeout);
            await Expect(
                    AccountA(page)
                        .GetByRole(
                            AriaRole.Cell,
                            new()
                            {
                                Name = "£10,001.00",
                                Exact = true,
                            }))
                .ToBeVisibleAsync();
            await page.GetByRole(
                    AriaRole.Button,
                    new()
                    {
                        Name = "View Investigations",
                        Exact = true,
                    })
                .ClickAsync();
            await Expect(
                    page.GetByRole(
                        AriaRole.Heading,
                        new()
                        {
                            Name = "Transaction Investigations",
                            Exact = true,
                        }))
                .ToBeVisibleAsync();
            ILocator flaggedRow = page.GetByRole(
                    AriaRole.Region,
                    new()
                    {
                        Name = "Flagged deposit entries",
                        Exact = true,
                    })
                .GetByRole(AriaRole.Row)
                .Filter(
                    new()
                    {
                        HasText = accountId,
                    });
            await Expect(flaggedRow)
                .ToHaveCountAsync(
                    1,
                    new()
                    {
                        Timeout = ProjectionTimeout,
                    });
            await Expect(flaggedRow).ToContainTextAsync("£10,001.00");
            await page.SetViewportSizeAsync(390, 844);
            Assert.True(
                await page.EvaluateAsync<bool>(
                    "document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>All five personas produce their actual HTTP authorization outcomes through the UI.</summary>
    /// <returns>The asynchronous authorization journey.</returns>
    [Fact]
    public async Task PersonasExposeActualCommandSagaAndProjectionAuthorizationAsync()
    {
        Assert.True(Fixture.IsInitialized, "fixture must be initialized");
        IPage page = await Fixture.CreatePageAsync();
        try
        {
            await page.GotoAsync(new Uri(Fixture.GatewayBaseUri, "/auth-proof").ToString());
            string entityId = $"flagship-auth-{Guid.NewGuid():N}";
            await page.GetByLabel(
                    "Auth Proof entity ID",
                    new()
                    {
                        Exact = true,
                    })
                .FillAsync(entityId);
            await Expect(
                    page.GetByText(
                        $"Current entity: {entityId}",
                        new()
                        {
                            Exact = false,
                        }))
                .ToBeVisibleAsync();
            (string Persona, int Authenticated, int Policy, int Role, int Read)[] personas =
            [
                ("Unauthenticated", 401, 401, 401, 401),
                ("Operator Roles", 200, 403, 403, 403),
                ("AuthProof Role", 200, 403, 200, 403),
                ("AuthProof Claim", 200, 200, 403, 200),
                ("Full Access", 200, 200, 200, 200),
            ];
            ILocator commandResponses = page.GetByRole(
                AriaRole.Region,
                new()
                {
                    Name = "Auth Proof command responses",
                    Exact = true,
                });
            foreach ((string persona, int authenticated, int policy, int role, int read) in personas)
            {
                Task<IResponse> projectionResponse = page.WaitForResponseAsync(response =>
                    (response.Request.Method == "GET") &&
                    response.Url.EndsWith($"/api/projections/auth-proof/{entityId}", StringComparison.Ordinal));
                await page.GetByRole(
                        AriaRole.Button,
                        new()
                        {
                            Name = persona,
                            Exact = true,
                        })
                    .ClickAsync();
                Assert.Equal(read, (await projectionResponse).Status);
                await Expect(
                        page.GetByRole(
                            AriaRole.Button,
                            new()
                            {
                                Name = persona,
                                Exact = true,
                            }))
                    .ToHaveAttributeAsync("aria-pressed", "true");
                (string Route, string Button, int Status)[] commands =
                [
                    ("authenticated", "Record Authenticated Access", authenticated),
                    ("policy", "Record Policy Access", policy),
                    ("role", "Record Role Access", role),
                ];
                foreach ((string route, string button, int status) in commands)
                {
                    Task<IResponse> responseTask = page.WaitForResponseAsync(response =>
                        (response.Request.Method == "POST") &&
                        response.Url.EndsWith($"/{entityId}/{route}", StringComparison.Ordinal));
                    await page.GetByRole(
                            AriaRole.Button,
                            new()
                            {
                                Name = button,
                                Exact = true,
                            })
                        .ClickAsync();
                    Assert.Equal(status, (await responseTask).Status);
                    await Expect(commandResponses.GetByRole(AriaRole.Status))
                        .ToContainTextAsync(
                            status == 200 ? "Latest response: accepted." : "Latest response: rejected.");
                    if (status != 200)
                    {
                        await Expect(commandResponses.GetByRole(AriaRole.Alert))
                            .ToContainTextAsync(status.ToString(CultureInfo.InvariantCulture));
                    }
                }

                Task<IResponse> sagaResponse = page.WaitForResponseAsync(response =>
                    (response.Request.Method == "POST") &&
                    response.Url.Contains("/api/sagas/auth-proof/", StringComparison.Ordinal));
                await page.GetByRole(
                        AriaRole.Button,
                        new()
                        {
                            Name = "Start AuthProof Saga",
                            Exact = true,
                        })
                    .ClickAsync();
                Assert.Equal(role, (await sagaResponse).Status);
            }

            await page.GetByRole(
                    AriaRole.Button,
                    new()
                    {
                        Name = "Refresh protected read",
                        Exact = true,
                    })
                .ClickAsync();
            await Expect(
                    page.GetByLabel(
                        "Authenticated access events observed",
                        new()
                        {
                            Exact = true,
                        }))
                .ToHaveTextAsync(
                    "4",
                    new()
                    {
                        Timeout = ProjectionTimeout,
                    });
            await page.SetViewportSizeAsync(390, 844);
            Assert.True(
                await page.EvaluateAsync<bool>(
                    "document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>A failed destination deposit reports compensation and reverses the completed source withdrawal.</summary>
    /// <returns>The asynchronous compensation journey.</returns>
    [Fact]
    public async Task TransferToUnopenedAccountReportsCompensationAndRestoresSourceAsync()
    {
        Assert.True(Fixture.IsInitialized, "fixture must be initialized");
        IPage page = await Fixture.CreatePageAsync();
        try
        {
            OperationsPage operations = await BankAccountScenario.PrepareAsync(Fixture, page, ProjectionTimeout);
            await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout);
            string source = await AccountA(page).Locator("h2 code").InnerTextAsync();
            string destination = $"unopened-{Guid.NewGuid():N}";
            await page.GotoAsync(
                new Uri(Fixture.GatewayBaseUri, $"/operations?a={Uri.EscapeDataString(source)}&b={destination}")
                    .ToString());
            await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout);
            await Expect(
                    page.Locator("#account-b-operations-panel")
                        .GetByRole(
                            AriaRole.Button,
                            new()
                            {
                                Name = "Open Account",
                                Exact = true,
                            }))
                .ToBeVisibleAsync();
            await operations.EnterTransferAmountAsync(25m);
            await operations.ClickStartTransferAsync();
            await operations.WaitForTransferPhaseAsync("Compensated", ProjectionTimeout);
            await Expect(operations.GetTransferStatus())
                .ToContainTextAsync("Account must be open before depositing funds.");
            await Expect(operations.GetTransferStatus()).ToContainTextAsync("Last completed step: 0");
            await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout);
            await Expect(AccountA(page).Locator("tbody tr"))
                .ToHaveCountAsync(
                    2,
                    new()
                    {
                        Timeout = ProjectionTimeout,
                    });
            await Expect(
                    AccountA(page)
                        .GetByRole(
                            AriaRole.Cell,
                            new()
                            {
                                Name = "Withdrawal",
                                Exact = true,
                            }))
                .ToBeVisibleAsync();
            await Expect(
                    AccountA(page)
                        .GetByRole(
                            AriaRole.Cell,
                            new()
                            {
                                Name = "Deposit",
                                Exact = true,
                            }))
                .ToBeVisibleAsync();
            await Expect(
                    AccountA(page)
                        .GetByRole(
                            AriaRole.Cell,
                            new()
                            {
                                Name = "£25.00",
                                Exact = true,
                            }))
                .ToHaveCountAsync(2);
            await Expect(
                    page.GetByRole(
                            AriaRole.Region,
                            new()
                            {
                                Name = "Transfer-start responses · this browser",
                                Exact = true,
                            })
                        .GetByRole(AriaRole.Status))
                .ToContainTextAsync("Latest response: accepted.");
            await Expect(
                    page.GetByLabel(
                        "Account B live balance",
                        new()
                        {
                            Exact = true,
                        }))
                .ToHaveCountAsync(0);
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}