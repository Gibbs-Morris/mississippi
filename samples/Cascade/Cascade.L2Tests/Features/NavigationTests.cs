using Allure.Xunit.Attributes;

using Cascade.L2Tests.PageObjects;

using Microsoft.Playwright;


namespace Cascade.L2Tests.Features;

/// <summary>
///     Tests for navigation and routing behavior.
/// </summary>
[AllureParentSuite("Cascade Chat App")]
[AllureSuite("Navigation")]
[AllureSubSuite("Routing")]
public class NavigationTests : TestBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="NavigationTests" /> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public NavigationTests(
        PlaywrightFixture fixture
    )
        : base(fixture)
    {
    }

    /// <summary>
    ///     Verifies that browser back button works correctly after login.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task BrowserBackButtonWorksAfterLogin()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("BackButtonUser");
        ChannelListPage channelList = new(page);
        string channelName = $"nav-test-{Guid.NewGuid():N}"[..24];

        // Create and select a channel
        await channelList.CreateChannelAsync(channelName);
        await channelList.SelectChannelAsync(channelName);

        // Act - go back
        await page.GoBackAsync();

        // Assert - should still be on channels page
        Assert.Contains("/channels", page.Url, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies that the channels page has proper page title after login.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task ChannelsPageHasCorrectTitle()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("TitleChecker");

        // Assert
        string title = await page.TitleAsync();
        Assert.Contains("Cascade", title, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Verifies that direct navigation to a channel works.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task DirectChannelNavigationRedirectsToLoginWhenNotAuthenticated()
    {
        // Arrange
        IPage page = await Fixture.CreatePageAsync();

        // Act - try to navigate directly to a channel
        await page.GotoAsync(Fixture.BaseUrl + "/channels/some-channel-id");

        // Assert - should redirect to login
        await page.WaitForURLAsync("**/login");
        Assert.Contains("/login", page.Url, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies that the error page is accessible.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task ErrorPageLoads()
    {
        // Arrange
        IPage page = await Fixture.CreatePageAsync();

        // Act
        await page.GotoAsync(Fixture.BaseUrl + "/Error");

        // Assert - page should load (may have error content or redirect)
        IResponse? response = await page.GotoAsync(Fixture.BaseUrl + "/Error");
        Assert.NotNull(response);
    }

    /// <summary>
    ///     Verifies that the home page displays landing content with login link.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task HomePageDisplaysLandingWithLoginLink()
    {
        // Arrange
        IPage page = await Fixture.CreatePageAsync();

        // Act
        await page.GotoAsync(Fixture.BaseUrl);

        // Assert - home page shows welcome and has link to login
        await page.WaitForSelectorAsync("a[href='login']");
        IElementHandle? loginLink = await page.QuerySelectorAsync("a[href='login']");
        Assert.NotNull(loginLink);
    }

    /// <summary>
    ///     Verifies that the login page has proper page title.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task LoginPageHasCorrectTitle()
    {
        // Arrange
        IPage page = await Fixture.CreatePageAsync();

        // Act
        await page.GotoAsync(Fixture.BaseUrl + "/login");

        // Assert
        string title = await page.TitleAsync();
        Assert.Contains("Login", title, StringComparison.OrdinalIgnoreCase);
    }
}