using Allure.Xunit.Attributes;

using Cascade.L2Tests.PageObjects;

using Microsoft.Playwright;


namespace Cascade.L2Tests.Features;

/// <summary>
///     Tests for the login flow.
/// </summary>
[AllureParentSuite("Cascade Chat App")]
[AllureSuite("Authentication")]
[AllureSubSuite("Login Flow")]
public class LoginTests : TestBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="LoginTests" /> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public LoginTests(
        PlaywrightFixture fixture
    )
        : base(fixture)
    {
    }

    /// <summary>
    ///     Verifies that the login page displays the display name input.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task LoginPageDisplaysInputField()
    {
        // Arrange
        IPage page = await Fixture.CreatePageAsync();

        // Act
        await page.GotoAsync(Fixture.BaseUrl + "/login");
        LoginPage loginPage = new(page);

        // Assert
        bool isVisible = await loginPage.IsVisibleAsync();
        Assert.True(isVisible, "Login input should be visible");
    }

    /// <summary>
    ///     Verifies that logging in with a valid name redirects to the channels page.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task LoginWithValidNameRedirectsToChannels()
    {
        // Arrange
        IPage page = await Fixture.CreatePageAsync();
        await page.GotoAsync(Fixture.BaseUrl);
        await page.WaitForURLAsync("**/login");

        // Act
        LoginPage loginPage = new(page);
        await loginPage.LoginAsync("TestUser");

        // Assert
        Assert.Contains("/channels", page.Url, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies that navigating to a protected route without login redirects to login page.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task NavigateWithoutLoginRedirectsToLogin()
    {
        // Arrange
        IPage page = await Fixture.CreatePageAsync();

        // Act
        await page.GotoAsync(Fixture.BaseUrl + "/channels");
        await page.WaitForURLAsync("**/login");

        // Assert
        Assert.Contains("/login", page.Url, StringComparison.Ordinal);
    }
}