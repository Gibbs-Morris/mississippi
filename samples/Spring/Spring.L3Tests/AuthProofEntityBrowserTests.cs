using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;

using static Microsoft.Playwright.Assertions;


namespace MississippiSamples.Spring.L3Tests;

/// <summary>Verify explicit entity selection and real protected outcomes without requests for partial drafts.</summary>
[Collection(SpringBrowserCollectionDefinition.Name)]
public sealed class AuthProofEntityBrowserTests
{
    private const float ProjectionTimeout = 120_000;

    /// <summary>Initializes a new instance of the <see cref="AuthProofEntityBrowserTests" /> class.</summary>
    /// <param name="fixture">The shared browser and real Spring deployment with local auth enabled.</param>
    public AuthProofEntityBrowserTests(
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

    private static string GetRequestedEntity(
        string path
    )
    {
        const string prefix = "/api/projections/auth-proof/";
        Assert.StartsWith(prefix, path, StringComparison.Ordinal);
        string[] segments = path[prefix.Length..].Split('/');
        Assert.True((segments.Length == 1) || (segments.Length == 3));
        if (segments.Length == 3)
        {
            Assert.Equal("at", segments[1]);
            Assert.True(long.TryParse(segments[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var _));
        }

        return Uri.UnescapeDataString(segments[0]);
    }

    private static async Task ObserveAuthenticatedCountAsync(
        IPage page,
        string endpoint,
        bool phone
    )
    {
        using CancellationTokenSource deadline =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromMilliseconds(ProjectionTimeout));
        ILocator refresh = page.GetByRole(
            AriaRole.Button,
            new()
            {
                Name = "Refresh protected read",
                Exact = true,
            });
        ILocator outcome = page.Locator(".spring-auth-outcome");
        while (true)
        {
            deadline.Token.ThrowIfCancellationRequested();
            await Expect(refresh).ToBeEnabledAsync().WaitAsync(deadline.Token);
            Task<IResponse> responseTask = page.WaitForResponseAsync(response =>
                (response.Request.Method == "GET") && response.Url.EndsWith(endpoint, StringComparison.Ordinal));
            await ActivateAsync(refresh, phone).WaitAsync(deadline.Token);
            IResponse response = await responseTask.WaitAsync(deadline.Token);
            Assert.True(response.Status is 200 or 404);
            await Expect(outcome.Locator("p[role='status']")).ToHaveCountAsync(0).WaitAsync(deadline.Token);
            await Expect(outcome.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0).WaitAsync(deadline.Token);
            if (response.Status == 200)
            {
                JsonElement body = Assert.IsType<JsonElement>(await response.JsonAsync().WaitAsync(deadline.Token));
                int count = body.GetProperty("authenticatedAccessCount").GetInt32();
                Assert.InRange(count, 0, 1);
                if (count == 1)
                {
                    await Expect(
                            page.GetByLabel(
                                "Authenticated access events observed",
                                new()
                                {
                                    Exact = true,
                                }))
                        .ToHaveTextAsync("1")
                        .WaitAsync(deadline.Token);
                    return;
                }
            }
        }
    }

    /// <summary>Verify explicit touch or Enter selection, committed command targets and real persona denial while drafting.</summary>
    /// <param name="viewportWidth">The phone or desktop viewport width.</param>
    /// <returns>The asynchronous real-browser regression.</returns>
    [Theory]
    [InlineData(320)]
    [InlineData(390)]
    [InlineData(1440)]
    public async Task DraftEditingKeepsTheSelectedEntityUntilSubmissionAsync(
        int viewportWidth
    )
    {
        Assert.True(Fixture.IsInitialized, "The real Spring deployment and browser must be initialized.");
        bool phone = viewportWidth < 768;
        string name = "auth-entity-selection-" + viewportWidth.ToString(CultureInfo.InvariantCulture);
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
        ConcurrentQueue<string> projectionRequests = new();
        List<string> pageErrors = [];
        page.PageError += (_, error) => pageErrors.Add(error);
        page.Request += (_, request) =>
        {
            string path = new Uri(request.Url).AbsolutePath;
            if ((request.Method == "GET") && path.StartsWith("/api/projections/auth-proof/", StringComparison.Ordinal))
            {
                projectionRequests.Enqueue(path);
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
            await page.GotoAsync(new Uri(Fixture.GatewayBaseUri, "/auth-proof").AbsoluteUri);
            ILocator input = page.GetByLabel(
                "Auth Proof entity ID",
                new()
                {
                    Exact = true,
                });
            ILocator selected = page.Locator(".spring-auth-selected-entity code");
            ILocator use = page.GetByRole(
                AriaRole.Button,
                new()
                {
                    Name = "Use this entity",
                    Exact = true,
                });
            ILocator outcome = page.Locator(".spring-auth-outcome");
            await Expect(selected).ToHaveTextAsync("auth-proof");
            await Expect(
                    outcome.GetByRole(
                        AriaRole.Button,
                        new()
                        {
                            Name = "Refresh protected read",
                            Exact = true,
                        }))
                .ToBeEnabledAsync();
            string entity = $"explicit-entity-{Guid.NewGuid():N}";
            string endpoint = "/api/projections/auth-proof/" + entity;
            foreach (string draft in new[] { "e", "ex", entity })
            {
                await input.FillAsync(draft);
                await Expect(input).ToHaveValueAsync(draft);
                await Expect(selected).ToHaveTextAsync("auth-proof");
                await Expect(
                        page.GetByText(
                            "Entity draft has not been applied.",
                            new()
                            {
                                Exact = false,
                            }))
                    .ToBeVisibleAsync();
            }

            await use.FocusAsync();
            await Expect(selected).ToHaveTextAsync("auth-proof");
            Assert.Contains(projectionRequests, path => GetRequestedEntity(path) == "auth-proof");
            Assert.DoesNotContain(projectionRequests, path => GetRequestedEntity(path) == entity);
            Assert.All(projectionRequests, path => Assert.Equal("auth-proof", GetRequestedEntity(path)));
            await SpringScreenshotEvidence.SaveAsync(page, name + "-draft");
            Task<IResponse> selectedResponse = page.WaitForResponseAsync(response =>
                (response.Request.Method == "GET") && response.Url.EndsWith(endpoint, StringComparison.Ordinal));
            if (phone)
            {
                await ActivateAsync(use, true);
            }
            else
            {
                await input.FocusAsync();
                await input.PressAsync("Enter");
            }

            IResponse selectedRead = await selectedResponse;
            Assert.Equal(404, selectedRead.Status);
            Assert.Contains(projectionRequests, path => GetRequestedEntity(path) == entity);
            await Expect(selected).ToHaveTextAsync(entity);
            await Expect(outcome).ToContainTextAsync("No projection data received yet.");
            await Expect(outcome.Locator("p[role='status']")).ToHaveCountAsync(0);
            await Expect(outcome.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0);
            await Expect(phone ? use : input).ToBeFocusedAsync();
            await SpringScreenshotEvidence.SaveAsync(page, name + "-selected-empty");
            string unapplied = "unapplied-" + new string('w', 160);
            await input.FillAsync(unapplied);
            await Expect(selected).ToHaveTextAsync(entity);
            Task<IResponse> commandResponse = page.WaitForResponseAsync(response =>
                (response.Request.Method == "POST") &&
                response.Url.EndsWith("/" + entity + "/authenticated", StringComparison.Ordinal));
            await ActivateAsync(
                page.GetByRole(
                    AriaRole.Button,
                    new()
                    {
                        Name = "Record Authenticated Access",
                        Exact = true,
                    }),
                phone);
            Assert.Equal(200, (await commandResponse).Status);
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
            await ObserveAuthenticatedCountAsync(page, endpoint, phone);
            await Expect(input).ToHaveValueAsync(unapplied);
            await Expect(selected).ToHaveTextAsync(entity);
            await SpringScreenshotEvidence.SaveAsync(page, name + "-accepted-count");
            Task<IResponse> deniedResponse = page.WaitForResponseAsync(response =>
                (response.Request.Method == "GET") &&
                response.Url.EndsWith(endpoint, StringComparison.Ordinal) &&
                (response.Status == 401));
            await ActivateAsync(
                page.GetByRole(
                    AriaRole.Button,
                    new()
                    {
                        Name = "Unauthenticated",
                        Exact = true,
                    }),
                phone);
            IResponse denied = await deniedResponse;
            Assert.Equal("true", denied.Request.Headers["x-spring-anonymous"]);
            await Expect(outcome.GetByRole(AriaRole.Alert)).ToContainTextAsync("401");
            await Expect(
                    page.GetByLabel(
                        "Authenticated access events observed",
                        new()
                        {
                            Exact = true,
                        }))
                .ToHaveCountAsync(0);
            await Expect(outcome.Locator("p[role='status']")).ToHaveCountAsync(0);
            await Expect(selected).ToHaveTextAsync(entity);
            await Expect(input).ToHaveValueAsync(unapplied);
            Assert.All(
                projectionRequests,
                path =>
                {
                    string requested = GetRequestedEntity(path);
                    Assert.True(
                        (requested == "auth-proof") || (requested == entity),
                        $"Unexpected entity read: {path}");
                });
            Assert.True(
                await page.EvaluateAsync<bool>(
                    "document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
            await SpringScreenshotEvidence.SaveAsync(page, name + "-draft-denied");
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
                    "Auth Proof entity journey and evidence capture both failed.",
                    failure,
                    captureFailure);
            }

            throw;
        }

        await SpringBrowserFixture.SaveBrowserArtifactsAsync(page, name);
    }
}