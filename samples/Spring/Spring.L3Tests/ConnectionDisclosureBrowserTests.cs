using System.Collections.Generic;
using System.Globalization;

using static Microsoft.Playwright.Assertions;


namespace MississippiSamples.Spring.L3Tests;

/// <summary>Verify paired connection disclosures through the running client at phone and desktop sizes.</summary>
[Collection(SpringBrowserCollectionDefinition.Name)]
public sealed class ConnectionDisclosureBrowserTests
{
    private const float ConnectionTimeout = 120_000;

    /// <summary>Initializes a new instance of the <see cref="ConnectionDisclosureBrowserTests" /> class.</summary>
    /// <param name="fixture">The shared browser and real Spring deployment.</param>
    public ConnectionDisclosureBrowserTests(
        SpringBrowserFixture fixture
    ) =>
        Fixture = fixture;

    private SpringBrowserFixture Fixture { get; }

    private static async Task ActivateAsync(
        ILocator control,
        bool phone
    )
    {
        Assert.True(await control.EvaluateAsync<double>("element => element.getBoundingClientRect().height") >= 44);
        if (phone)
        {
            await control.TapAsync();
        }
        else
        {
            await control.FocusAsync();
            await control.PressAsync("Enter");
        }
    }

    /// <summary>Verify stable targets, touch or keyboard activation and actual focus restoration after closing.</summary>
    /// <param name="viewportWidth">The phone or desktop viewport width.</param>
    /// <param name="operations">Whether to exercise Move money rather than Prepare accounts.</param>
    /// <returns>The asynchronous browser regression.</returns>
    [Theory]
    [InlineData(320, false)]
    [InlineData(390, false)]
    [InlineData(1440, false)]
    [InlineData(320, true)]
    [InlineData(390, true)]
    [InlineData(1440, true)]
    public async Task ConnectionDetailsKeepTheirTargetAndReturnFocusAsync(
        int viewportWidth,
        bool operations
    )
    {
        Assert.True(Fixture.IsInitialized, "The real Spring deployment and browser must be initialized.");
        bool phone = viewportWidth < 768;
        string name = "connection-disclosure-" +
                      (operations ? "operations-" : "accounts-") +
                      viewportWidth.ToString(CultureInfo.InvariantCulture);
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
            await page.GotoAsync(new Uri(Fixture.GatewayBaseUri, operations ? "/operations" : "/accounts").AbsoluteUri);
            ILocator trigger = page.Locator(".spring-page-header > button");
            await Expect(trigger)
                .ToHaveAccessibleNameAsync(
                    "Connection status: Connected",
                    new()
                    {
                        Timeout = ConnectionTimeout,
                    });
            ILocator details = page.GetByRole(
                AriaRole.Region,
                new()
                {
                    Name = "Live connection details",
                    Exact = true,
                });
            await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
            Assert.Null(await trigger.GetAttributeAsync("aria-controls"));
            await Expect(details).ToHaveCountAsync(0);
            await SpringScreenshotEvidence.SaveAsync(page, name + "-closed");
            await ActivateAsync(trigger, phone);
            await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
            await Expect(details).ToBeVisibleAsync();
            string? targetId = await trigger.GetAttributeAsync("aria-controls");
            Assert.False(string.IsNullOrWhiteSpace(targetId));
            Assert.Equal(targetId, await details.GetAttributeAsync("id"));
            int targetCount = await page.EvaluateAsync<int>(
                "id => [...document.querySelectorAll('[id]')].filter(element => element.id === id).length",
                targetId);
            Assert.Equal(1, targetCount);
            await Expect(details.Locator("dl > div").First.Locator("dd")).ToHaveTextAsync("Connected");
            Assert.True(
                await page.EvaluateAsync<bool>(
                    "document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
            await SpringScreenshotEvidence.SaveAsync(page, name + "-open");
            ILocator close = details.GetByRole(
                AriaRole.Button,
                new()
                {
                    Name = "Close",
                    Exact = true,
                });
            await ActivateAsync(close, phone);
            await Expect(details).ToHaveCountAsync(0);
            await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
            Assert.Null(await trigger.GetAttributeAsync("aria-controls"));
            await Expect(trigger).ToBeFocusedAsync();
            if (!phone)
            {
                Assert.True(
                    await trigger.EvaluateAsync<double>(
                        "element => Number.parseFloat(getComputedStyle(element).outlineWidth)") >
                    0);
            }

            await SpringScreenshotEvidence.SaveAsync(page, name + "-returned-focus");
            await ActivateAsync(trigger, phone);
            await Expect(details).ToBeVisibleAsync();
            await Expect(trigger).ToHaveAttributeAsync("aria-controls", targetId);
            await Expect(details).ToHaveAttributeAsync("id", targetId);
            Assert.True(
                await page.EvaluateAsync<bool>(
                    "document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
            await SpringScreenshotEvidence.SaveAsync(page, name + "-reopened");
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
                    "Connection disclosure journey and evidence capture both failed.",
                    failure,
                    captureFailure);
            }

            throw;
        }

        await SpringBrowserFixture.SaveBrowserArtifactsAsync(page, name);
    }
}