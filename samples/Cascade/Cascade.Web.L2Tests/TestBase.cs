using Microsoft.Playwright;


namespace Cascade.Web.L2Tests;

/// <summary>
///     Base class for E2E tests providing common helper methods.
/// </summary>
[Collection("Cascade.Web L2 Tests")]
#pragma warning disable CA1515 // Types can be made internal - xUnit test base must be public
public abstract class TestBase
#pragma warning restore CA1515
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TestBase" /> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    protected TestBase(
        PlaywrightFixture fixture
    ) =>
        Fixture = fixture;

    /// <summary>
    ///     Gets the Playwright fixture with browser and AppHost.
    /// </summary>
    protected PlaywrightFixture Fixture { get; }

    /// <summary>
    ///     Creates a new Playwright page.
    /// </summary>
    /// <returns>The new page.</returns>
    protected Task<IPage> CreatePageAsync() =>
        Fixture.CreatePageAsync();
}
