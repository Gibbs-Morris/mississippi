using System.Collections.Generic;
using System.Globalization;
using System.Threading;

using MississippiSamples.Spring.L3Tests.Pages;

using static Microsoft.Playwright.Assertions;


namespace MississippiSamples.Spring.L3Tests;

/// <summary>
///     Verifies failed investigation reads in a real browser, including actual retained server data.
/// </summary>
[Collection(SpringBrowserCollectionDefinition.Name)]
public sealed class InvestigationsQueueStatesTests
{
    private const float ProjectionTimeout = 120_000;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InvestigationsQueueStatesTests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Spring browser fixture.</param>
    public InvestigationsQueueStatesTests(
        SpringBrowserFixture fixture
    ) =>
        Fixture = fixture;

    private SpringBrowserFixture Fixture { get; }

    /// <summary>Verify that a controlled HTTP failure cannot appear as an empty queue or a successful cached read.</summary>
    /// <param name="viewportWidth">The phone or desktop width.</param>
    /// <param name="loadCachedData">Whether to observe a real high-value deposit before failing the next read.</param>
    /// <returns>The asynchronous real-browser state regression.</returns>
    [Theory]
    [InlineData(320, false)]
    [InlineData(390, false)]
    [InlineData(1440, false)]
    [InlineData(320, true)]
    [InlineData(390, true)]
    [InlineData(1440, true)]
    public async Task FailedQueueReadDoesNotShowHealthyOutcomesAsync(
        int viewportWidth,
        bool loadCachedData
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
        string name = "investigations-error-" +
                      (loadCachedData ? "cached-" : "cold-") +
                      viewportWidth.ToString(CultureInfo.InvariantCulture);
        double? observedDocumentOrigin = null;
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
            if (loadCachedData)
            {
                OperationsPage operations = await BankAccountScenario.PrepareAsync(Fixture, page, ProjectionTimeout);
                await operations.WaitForBalanceValueAsync("500.00", ProjectionTimeout);
                string accountId = await page.Locator("#account-a-operations-panel h2 code").InnerTextAsync();
                await operations.EnterDepositAmountAsync(10001m);
                await operations.ClickDepositAsync();
                await operations.WaitForBalanceValueAsync("10,501.00", ProjectionTimeout);
                await page.GetByRole(
                        AriaRole.Button,
                        new()
                        {
                            Name = "View Investigations",
                            Exact = true,
                        })
                    .ClickAsync();
                ILocator queue = page.Locator(".spring-queue");
                await Expect(
                        queue.Locator("tbody tr")
                            .Filter(
                                new()
                                {
                                    HasText = accountId,
                                }))
                    .ToHaveCountAsync(
                        1,
                        new()
                        {
                            Timeout = ProjectionTimeout,
                        });
                await Expect(queue).ToContainTextAsync("Projection version");
                await Expect(queue.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0);
                observedDocumentOrigin = await page.EvaluateAsync<double>("performance.timeOrigin");
                await SpringScreenshotEvidence.SaveAsync(
                    page,
                    "investigations-observed-" + viewportWidth.ToString(CultureInfo.InvariantCulture));
                ILocator home = page.GetByRole(
                    AriaRole.Link,
                    new()
                    {
                        Name = "Start here",
                        Exact = true,
                    });
                if (phone)
                {
                    await home.TapAsync();
                }
                else
                {
                    await home.FocusAsync();
                    await home.PressAsync("Enter");
                }

                await Expect(page.Locator("#welcome-heading")).ToBeVisibleAsync();
            }

            int intercepted = 0;

            // Fail the real projection fetch. No fabricated projection data is supplied.
            await page.RouteAsync(
                "**/api/projections/flagged-transactions/global**",
                async route =>
                {
                    Interlocked.Increment(ref intercepted);
                    await route.FulfillAsync(
                        new()
                        {
                            Status = 503,
                            ContentType = "text/plain",
                            Body = "Controlled investigation read failure.",
                        });
                });
            if (loadCachedData)
            {
                ILocator investigate = page.GetByRole(
                    AriaRole.Link,
                    new()
                    {
                        Name = "Investigate",
                        Exact = true,
                    });
                if (phone)
                {
                    await investigate.TapAsync();
                }
                else
                {
                    await investigate.FocusAsync();
                    await investigate.PressAsync("Enter");
                }
            }
            else
            {
                await page.GotoAsync(new Uri(Fixture.GatewayBaseUri, "/investigations").ToString());
            }

            ILocator failedQueue = page.Locator(".spring-queue");
            await Expect(failedQueue.GetByRole(AriaRole.Alert)).ToContainTextAsync("The queue could not be read");
            await Expect(failedQueue.GetByRole(AriaRole.Alert)).ToContainTextAsync("503");
            await Expect(failedQueue)
                .ToContainTextAsync("This failed read does not tell you whether deposits have been flagged.");
            await Expect(
                    failedQueue.GetByText(
                        "No queue data received yet",
                        new()
                        {
                            Exact = false,
                        }))
                .ToHaveCountAsync(0);
            await Expect(
                    failedQueue.GetByText(
                        "No flagged deposits",
                        new()
                        {
                            Exact = true,
                        }))
                .ToHaveCountAsync(0);
            await Expect(
                    failedQueue.GetByText(
                        "Projection version",
                        new()
                        {
                            Exact = false,
                        }))
                .ToHaveCountAsync(0);
            await Expect(failedQueue.Locator("table, .spring-queue-empty, .spring-queue-scroll, [role='status']"))
                .ToHaveCountAsync(0);
            Assert.True(intercepted > 0, "the controlled failure must reach the real projection fetch");
            if (loadCachedData)
            {
                Assert.NotNull(observedDocumentOrigin);
                Assert.Equal(observedDocumentOrigin.Value, await page.EvaluateAsync<double>("performance.timeOrigin"));
            }

            Assert.Empty(pageErrors);
            Assert.True(
                await page.EvaluateAsync<bool>(
                    "document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
            ILocator errorDetails = failedQueue.Locator("details");
            ILocator summary = errorDetails.Locator("summary");
            await Expect(summary).ToHaveTextAsync("Read error details");
            await Expect(errorDetails.Locator("p")).ToBeHiddenAsync();
            Assert.True(await summary.EvaluateAsync<double>("element => element.getBoundingClientRect().height") >= 44);
            await SpringScreenshotEvidence.SaveAsync(page, name);
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

            await Expect(errorDetails.Locator("p")).ToBeVisibleAsync();
            await Expect(errorDetails.Locator("p")).ToContainTextAsync("503");
            Assert.True(
                await page.EvaluateAsync<bool>(
                    "document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
            await SpringScreenshotEvidence.SaveAsync(page, name + "-details");
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
                    "Investigation journey failed and browser evidence capture also failed.",
                    failure,
                    captureFailure);
            }

            throw;
        }

        await SpringBrowserFixture.SaveBrowserArtifactsAsync(page, name);
    }
}