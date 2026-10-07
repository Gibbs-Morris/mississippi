using System.Globalization;
using System.Text.Json;
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

    private static async Task ExpectAuthProofReadAsync(
        IPage page,
        IResponse response,
        int status
    )
    {
        Assert.Equal(status, response.Status);
        ILocator outcome = page.Locator(".spring-auth-outcome");
        ILocator count = page.GetByLabel(
            "Authenticated access events observed",
            new()
            {
                Exact = true,
            });
        if (status == 200)
        {
            JsonElement body = Assert.IsType<JsonElement>(
                await response.JsonAsync()
                    .WaitAsync(TimeSpan.FromMilliseconds(ProjectionTimeout), TestContext.Current.CancellationToken));
            await Expect(outcome.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0);
            await Expect(count)
                .ToHaveTextAsync(
                    body.GetProperty("authenticatedAccessCount").GetInt32().ToString(CultureInfo.InvariantCulture));
        }
        else if (status == 404)
        {
            await Expect(outcome.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0);
            await Expect(count).ToHaveCountAsync(0);
            await Expect(outcome).ToContainTextAsync("No projection data received yet.");
        }
        else
        {
            // Denied ResponseHeadersRead results are disposed before reading the body.
            // Verify the actual HTTP denial and completed client outcome, not network-finished.
            await Expect(outcome.GetByRole(AriaRole.Alert))
                .ToContainTextAsync($"HTTP {status.ToString(CultureInfo.InvariantCulture)}");
            await Expect(count).ToHaveCountAsync(0);
        }

        await Expect(outcome.Locator("p[role='status']")).ToHaveCountAsync(0);
        await Expect(
                outcome.GetByRole(
                    AriaRole.Button,
                    new()
                    {
                        Name = "Refresh protected read",
                        Exact = true,
                    }))
            .ToBeEnabledAsync();
    }

    private static bool HasHttpPersona(
        IRequest request,
        string persona
    )
    {
        bool anonymous = request.Headers.TryGetValue("x-spring-anonymous", out string? anonymousValue) &&
                         string.Equals(anonymousValue, "true", StringComparison.Ordinal);
        if (persona == "Unauthenticated")
        {
            return anonymous;
        }

        request.Headers.TryGetValue("x-spring-roles", out string? roles);
        request.Headers.TryGetValue("x-spring-claims", out string? claims);
        string expectedRoles = persona is "AuthProof Role" or "Full Access"
            ? "banking-operator,transfer-operator,auth-proof-operator"
            : "banking-operator,transfer-operator";
        string? expectedClaims = persona is "AuthProof Claim" or "Full Access" ? "spring.permission=auth-proof" : null;
        return !anonymous &&
               string.Equals(roles, expectedRoles, StringComparison.Ordinal) &&
               string.Equals(claims, expectedClaims, StringComparison.Ordinal);
    }

    private static async Task WaitForAuthProofCountAsync(
        IPage page,
        string endpoint,
        string persona,
        int expectedCount
    )
    {
        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromMilliseconds(ProjectionTimeout));
        while (true)
        {
            deadline.Token.ThrowIfCancellationRequested();
            ILocator refresh = page.GetByRole(
                AriaRole.Button,
                new()
                {
                    Name = "Refresh protected read",
                    Exact = true,
                });
            await Expect(refresh).ToBeEnabledAsync().WaitAsync(deadline.Token);
            Task<IRequest> requestTask = page.WaitForRequestAsync(request =>
                (request.Method == "GET") &&
                request.Url.EndsWith(endpoint, StringComparison.Ordinal) &&
                HasHttpPersona(request, persona));
            await refresh.ClickAsync().WaitAsync(deadline.Token);
            IRequest request = await requestTask.WaitAsync(deadline.Token);
            IResponse response = Assert.IsType<IResponse>(
                await request.ResponseAsync().WaitAsync(deadline.Token),
                false);
            Assert.True(
                response.Status is 200 or 404,
                $"Unexpected protected read: HTTP {response.Status.ToString(CultureInfo.InvariantCulture)}.");
            await ExpectAuthProofReadAsync(page, response, response.Status).WaitAsync(deadline.Token);
            if (response.Status == 200)
            {
                JsonElement body = Assert.IsType<JsonElement>(await response.JsonAsync().WaitAsync(deadline.Token));
                int observedCount = body.GetProperty("authenticatedAccessCount").GetInt32();
                Assert.InRange(observedCount, 0, expectedCount);
                if (observedCount == expectedCount)
                {
                    return;
                }
            }
        }
    }

    /// <summary>Task navigation keeps both account anchors and the shared link bound to the current pair.</summary>
    /// <returns>The asynchronous keyboard, narrow-layout and independent-browser navigation regression.</returns>
    [Fact]
    public async Task AccountAnchorsAndSharedLinkRetainPairAfterTaskNavigationAsync()
    {
        Assert.True(Fixture.IsInitialized, "fixture must be initialized");
        IPage page = await Fixture.CreatePageAsync();
        IPage observer = await Fixture.CreatePageAsync();
        try
        {
            OperationsPage operations = await BankAccountScenario.PrepareAsync(Fixture, page, ProjectionTimeout);
            await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout);
            await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout, "B");
            string accountA = await AccountA(page).Locator("h2 code").InnerTextAsync();
            string accountB = await page.Locator("#account-b-operations-panel h2 code").InnerTextAsync();
            await page.GetByRole(
                    AriaRole.Button,
                    new()
                    {
                        Name = "View Investigations",
                        Exact = true,
                    })
                .ClickAsync();
            await page.GetByRole(
                    AriaRole.Link,
                    new()
                    {
                        Name = "Move money",
                        Exact = true,
                    })
                .ClickAsync();
            await Expect(AccountA(page).Locator("h2 code")).ToHaveTextAsync(accountA);
            await page.Locator(".spring-share-pair summary").PressAsync("Enter");
            string? share = await page.Locator(".spring-share-pair > a").GetAttributeAsync("href");
            Assert.Equal(
                new Uri(
                    Fixture.GatewayBaseUri,
                    $"/operations?a={Uri.EscapeDataString(accountA)}&b={Uri.EscapeDataString(accountB)}").ToString(),
                share);
            Assert.NotNull(share);
            await observer.GotoAsync(share);
            OperationsPage observed = new(observer);
            await observed.WaitForBalanceValueAsync("500.00", ProjectionTimeout);
            await observed.WaitForBalanceValueAsync("500.00", ProjectionTimeout, "B");
            await Expect(observer.Locator("#account-a-panel-heading code")).ToHaveTextAsync(accountA);
            await Expect(observer.Locator("#account-b-panel-heading code")).ToHaveTextAsync(accountB);
            ILocator accountBJump = page.GetByRole(
                AriaRole.Link,
                new()
                {
                    Name = "Account B ↓",
                    Exact = true,
                });
            await accountBJump.FocusAsync();
            await accountBJump.PressAsync("Enter");
            await Expect(page.Locator("#account-b-operations-panel")).ToBeFocusedAsync();
            Assert.Equal("/operations", new Uri(page.Url).AbsolutePath);
            await Expect(AccountA(page).Locator("h2 code")).ToHaveTextAsync(accountA);
            await Expect(page.Locator("#account-b-panel-heading code")).ToHaveTextAsync(accountB);
            await page.SetViewportSizeAsync(320, 740);
            await page.GetByRole(
                    AriaRole.Link,
                    new()
                    {
                        Name = "Account A ↓",
                        Exact = true,
                    })
                .ClickAsync();
            await Expect(page.Locator("#account-a-operations-panel")).ToBeFocusedAsync();
            Assert.Equal("/operations", new Uri(page.Url).AbsolutePath);
            Assert.True(
                await page.EvaluateAsync<bool>(
                    "document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
            await SpringScreenshotEvidence.SaveAsync(page, "shared-pair-and-anchors-320");
        }
        finally
        {
            await observer.CloseAsync();
            await page.CloseAsync();
        }
    }

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
                    page.GetByRole(
                        AriaRole.Textbox,
                        new()
                        {
                            Name = "Account A ID",
                            Exact = true,
                        }))
                .ToHaveValueAsync(string.Empty);
            await Expect(
                    page.GetByRole(
                        AriaRole.Textbox,
                        new()
                        {
                            Name = "Account B ID",
                            Exact = true,
                        }))
                .ToHaveValueAsync(demoB);
            string replacement = $"replacement-{Guid.NewGuid():N}";
            await page.GetByRole(
                    AriaRole.Textbox,
                    new()
                    {
                        Name = "Account A ID",
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
                    page.GetByRole(
                        AriaRole.Textbox,
                        new()
                        {
                            Name = "Account A ID",
                            Exact = true,
                        }))
                .ToHaveValueAsync(replacement);
            await Expect(
                    page.GetByRole(
                        AriaRole.Textbox,
                        new()
                        {
                            Name = "Account B ID",
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
            await SpringScreenshotEvidence.SaveAsync(page, "account-switch-preserved-pair");
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
            await page.Context.Tracing.StartAsync(
                new()
                {
                    Screenshots = true,
                    Snapshots = true,
                    Sources = true,
                });
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
                        "0 failed.",
                        new()
                        {
                            Exact = false,
                        }))
                .ToBeVisibleAsync();
            Assert.Equal(444, acceptedBankingRequests);
            await SpringScreenshotEvidence.SaveAsync(page, "all-bursts-retained-ledger");
        }
        catch (Exception testException)
        {
            try
            {
                await SpringBrowserFixture.SaveBrowserArtifactsAsync(page, "all-bursts");
            }
            catch (Exception artifactException)
            {
                throw new AggregateException(
                    "Command burst journey and artifact capture both failed.",
                    testException,
                    artifactException);
            }

            throw;
        }

        await SpringBrowserFixture.SaveBrowserArtifactsAsync(page, "all-bursts");
    }

    /// <summary>A delayed allowed HTTP read cannot replace a newer persona's real denial.</summary>
    /// <returns>The asynchronous persona-ordering regression.</returns>
    [Fact]
    public async Task DelayedAllowedReadDoesNotReplaceNewPersonaDenialAsync()
    {
        Assert.True(Fixture.IsInitialized, "fixture must be initialized");
        IPage page = await Fixture.CreatePageAsync();
        using SemaphoreSlim release = new(0, 1);
        using SemaphoreSlim intercepted = new(0, 1);
        try
        {
            await page.GotoAsync(new Uri(Fixture.GatewayBaseUri, "/auth-proof").ToString());
            string entity = $"persona-race-{Guid.NewGuid():N}";
            string endpoint = $"/api/projections/auth-proof/{entity}";
            await page.GetByLabel(
                    "Auth Proof entity ID",
                    new()
                    {
                        Exact = true,
                    })
                .FillAsync(entity);
            await page.GetByRole(
                    AriaRole.Button,
                    new()
                    {
                        Name = "Record Authenticated Access",
                        Exact = true,
                    })
                .ClickAsync();
            await Expect(
                    page.GetByRole(
                            AriaRole.Region,
                            new()
                            {
                                Name = "Auth Proof command responses",
                                Exact = true,
                            })
                        .GetByRole(AriaRole.Status))
                .ToContainTextAsync("Latest response: accepted.");
            await WaitForAuthProofCountAsync(page, endpoint, "Full Access", 1);
            await Expect(
                    page.GetByLabel(
                        "Authenticated access events observed",
                        new()
                        {
                            Exact = true,
                        }))
                .ToHaveTextAsync(
                    "1",
                    new()
                    {
                        Timeout = ProjectionTimeout,
                    });
            int requests = 0;
            await page.RouteAsync(
                $"**{endpoint}",
                async route =>
                {
                    if (Interlocked.Increment(ref requests) == 1)
                    {
                        intercepted.Release();
                        await release.WaitAsync(TestContext.Current.CancellationToken);
                    }

                    await route.ContinueAsync();
                });
            Task<IResponse> obsoleteResponse = page.WaitForResponseAsync(response =>
                response.Url.EndsWith(endpoint, StringComparison.Ordinal) && (response.Status == 200));
            await page.GetByRole(
                    AriaRole.Button,
                    new()
                    {
                        Name = "Refresh protected read",
                        Exact = true,
                    })
                .ClickAsync();
            Assert.True(await intercepted.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken));
            Task<IResponse> deniedResponse = page.WaitForResponseAsync(response =>
                response.Url.EndsWith(endpoint, StringComparison.Ordinal) && (response.Status == 401));
            await page.GetByRole(
                    AriaRole.Button,
                    new()
                    {
                        Name = "Unauthenticated",
                        Exact = true,
                    })
                .ClickAsync();
            Assert.Equal(401, (await deniedResponse).Status);
            ILocator readError = page.Locator(".spring-auth-outcome").GetByRole(AriaRole.Alert);
            await Expect(readError).ToContainTextAsync("401");
            await Expect(
                    page.GetByLabel(
                        "Authenticated access events observed",
                        new()
                        {
                            Exact = true,
                        }))
                .ToHaveCountAsync(0);
            release.Release();
            IResponse allowed = await obsoleteResponse;
            Assert.Equal(200, allowed.Status);
            Assert.Null(
                await allowed.FinishedAsync()
                    .WaitAsync(TimeSpan.FromMilliseconds(ProjectionTimeout), TestContext.Current.CancellationToken));
            JsonElement obsoleteBody = Assert.IsType<JsonElement>(
                await allowed.JsonAsync()
                    .WaitAsync(TimeSpan.FromMilliseconds(ProjectionTimeout), TestContext.Current.CancellationToken));
            Assert.Equal(1, obsoleteBody.GetProperty("authenticatedAccessCount").GetInt32());
            await page.Locator("summary")
                .Filter(
                    new()
                    {
                        HasText = "Inspect raw projection and client state snapshots",
                    })
                .ClickAsync();
            await Expect(
                    page.GetByRole(
                        AriaRole.Heading,
                        new()
                        {
                            Name = "Projection Snapshot",
                            Exact = true,
                        }))
                .ToBeVisibleAsync();
            await Expect(readError).ToContainTextAsync("401");
            await Expect(
                    page.GetByRole(
                        AriaRole.Button,
                        new()
                        {
                            Name = "Unauthenticated",
                            Exact = true,
                        }))
                .ToHaveAttributeAsync("aria-pressed", "true");
            await Expect(
                    page.GetByLabel(
                        "Authenticated access events observed",
                        new()
                        {
                            Exact = true,
                        }))
                .ToHaveCountAsync(0);
            await SpringScreenshotEvidence.SaveAsync(page, "obsolete-read-ignored");
        }
        finally
        {
            if (release.CurrentCount == 0)
            {
                release.Release();
            }

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
            await SpringScreenshotEvidence.SaveAsync(page, "first-run-home-390");
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
            await Expect(responses.GetByRole(AriaRole.Status)).ToContainTextAsync("Latest request: failed.");
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
            await SpringScreenshotEvidence.SaveAsync(page, "first-run-outcomes-390");
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
            await SpringScreenshotEvidence.SaveAsync(page, "investigation-populated-390");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>A lost response reports client failure while exposing the actual deposit and contained phone history.</summary>
    /// <param name="viewportWidth">The phone width used to verify the expanded command history.</param>
    /// <returns>The asynchronous real-server lost-reply regression.</returns>
    [Theory]
    [InlineData(320)]
    [InlineData(390)]
    public async Task LostDepositReplyShowsClientFailureAndActualCommittedOutcomeAsync(
        int viewportWidth
    )
    {
        Assert.True(Fixture.IsInitialized, "fixture must be initialized");
        IPage page = await Fixture.CreatePageAsync();
        try
        {
            await page.SetViewportSizeAsync(viewportWidth, 844);
            OperationsPage operations = await BankAccountScenario.PrepareAsync(Fixture, page, ProjectionTimeout);
            await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout);
            string account = await AccountA(page).Locator("h2 code").InnerTextAsync();
            int intercepted = 0;
            int? serverStatus = null;
            bool serverAccepted = false;
            await page.RouteAsync(
                $"**/api/aggregates/bank-account/{Uri.EscapeDataString(account)}/deposit",
                async route =>
                {
                    Interlocked.Increment(ref intercepted);
                    IAPIResponse response = await route.FetchAsync(
                        new()
                        {
                            MaxRetries = 0,
                        });
                    try
                    {
                        serverStatus = response.Status;
                        JsonElement body = Assert.IsType<JsonElement>(await response.JsonAsync());
                        serverAccepted = body.GetProperty("success").GetBoolean();
                        await route.AbortAsync("failed");
                    }
                    finally
                    {
                        await response.DisposeAsync();
                    }
                });
            await operations.EnterDepositAmountAsync(25m);
            await operations.ClickDepositAsync();
            ILocator responses = page.GetByRole(
                AriaRole.Region,
                new()
                {
                    Name = "Banking responses · this browser",
                    Exact = true,
                });
            await Expect(responses.GetByRole(AriaRole.Status)).ToContainTextAsync("Latest request: failed.");
            await Expect(responses.GetByRole(AriaRole.Alert)).ToContainTextAsync("Network error:");
            await Expect(responses).ToContainTextAsync("does not establish the server outcome");
            await operations.WaitForBalanceValueAsync("525.00", ProjectionTimeout);
            ILocator ledger = AccountA(page).Locator("tbody tr");
            await Expect(ledger)
                .ToHaveCountAsync(
                    1,
                    new()
                    {
                        Timeout = ProjectionTimeout,
                    });
            await Expect(ledger.First).ToContainTextAsync("Deposit");
            await Expect(ledger.First).ToContainTextAsync("£25.00");
            await responses.Locator("summary").PressAsync("Enter");
            ILocator request = responses.Locator("tbody tr")
                .Filter(
                    new()
                    {
                        HasText = "DepositFundsAction",
                    });
            await Expect(request).ToHaveCountAsync(1);
            await Expect(request).ToContainTextAsync("Failed");
            await Expect(request).ToContainTextAsync("HttpError");
            Assert.True(
                await page.EvaluateAsync<bool>(
                    "document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
            ILocator history = responses.GetByRole(
                AriaRole.Region,
                new()
                {
                    Name = "Banking responses · this browser command history",
                    Exact = true,
                });
            await Expect(history).ToBeVisibleAsync();
            Assert.True(await history.EvaluateAsync<bool>("element => element.scrollWidth > element.clientWidth"));
            await responses.Locator("summary").PressAsync("Tab");
            Assert.True(await history.EvaluateAsync<bool>("element => element === document.activeElement"));
            await history.PressAsync("ArrowRight");
            await page.WaitForFunctionAsync(
                "() => document.activeElement.scrollLeft > 0",
                null,
                new()
                {
                    Timeout = ProjectionTimeout,
                });
            Assert.Equal(200, serverStatus);
            Assert.True(serverAccepted);
            Assert.Equal(1, intercepted);
            await SpringScreenshotEvidence.SaveAsync(
                page,
                "lost-reply-history-" + viewportWidth.ToString(CultureInfo.InvariantCulture));
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
            int authenticatedCount = 0;
            foreach ((string persona, int authenticated, int policy, int role, int read) in personas)
            {
                Task<IResponse> projectionResponse = page.WaitForResponseAsync(response =>
                    (response.Request.Method == "GET") &&
                    response.Url.EndsWith($"/api/projections/auth-proof/{entityId}", StringComparison.Ordinal) &&
                    HasHttpPersona(response.Request, persona));
                await page.GetByRole(
                        AriaRole.Button,
                        new()
                        {
                            Name = persona,
                            Exact = true,
                        })
                    .ClickAsync();
                IResponse initialRead = await projectionResponse;
                if (read == 200)
                {
                    Assert.True(
                        initialRead.Status is 200 or 404,
                        $"Unexpected protected read: HTTP {initialRead.Status.ToString(CultureInfo.InvariantCulture)}.");
                    await ExpectAuthProofReadAsync(page, initialRead, initialRead.Status);
                    await WaitForAuthProofCountAsync(
                        page,
                        $"/api/projections/auth-proof/{entityId}",
                        persona,
                        authenticatedCount);
                }
                else
                {
                    await ExpectAuthProofReadAsync(page, initialRead, read);
                }

                Task<IResponse> repeatedPersonaRead = page.WaitForResponseAsync(response =>
                    (response.Request.Method == "GET") &&
                    response.Url.EndsWith($"/api/projections/auth-proof/{entityId}", StringComparison.Ordinal) &&
                    HasHttpPersona(response.Request, persona));
                await page.GetByRole(
                        AriaRole.Button,
                        new()
                        {
                            Name = persona,
                            Exact = true,
                        })
                    .ClickAsync();
                IResponse refreshedRead = await repeatedPersonaRead;
                await ExpectAuthProofReadAsync(page, refreshedRead, read);
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
                    if ((route == "authenticated") && (status == 200))
                    {
                        authenticatedCount++;
                    }

                    await Expect(commandResponses.GetByRole(AriaRole.Status))
                        .ToContainTextAsync(status == 200 ? "Latest response: accepted." : "Latest request: failed.");
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

            Assert.Equal(4, authenticatedCount);
            await WaitForAuthProofCountAsync(page, $"/api/projections/auth-proof/{entityId}", "Full Access", 4);
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
            await SpringScreenshotEvidence.SaveAsync(page, "persona-outcomes-390");
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
            await SpringScreenshotEvidence.SaveAsync(page, "transfer-compensated");
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}