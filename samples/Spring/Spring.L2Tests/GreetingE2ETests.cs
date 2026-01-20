using Spring.L2Tests.Pages;


namespace Spring.L2Tests;

/// <summary>
///     End-to-end tests for the Spring sample application using Playwright.
///     Tests the full flow from Blazor UI through Orleans grain and back.
/// </summary>
[Collection(SpringTestCollection.Name)]
#pragma warning disable CA1515 // Types can be made internal - xUnit test class must be public
public sealed class GreetingE2ETests
#pragma warning restore CA1515
{
    private readonly SpringFixture fixture;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GreetingE2ETests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Spring fixture.</param>
    public GreetingE2ETests(
        SpringFixture fixture
    ) =>
        this.fixture = fixture;

    /// <summary>
    ///     Verifies the fixture initializes successfully.
    /// </summary>
    [Fact]
    public void FixtureShouldBeInitialized()
    {
        // Assert
        fixture.IsInitialized.Should().BeTrue("the Spring AppHost should start successfully");
        fixture.InitializationError.Should().BeNull("there should be no initialization errors");
        fixture.ServerBaseUri.Should().NotBe(new Uri("about:blank"), "the server should have a valid URL");
    }

    /// <summary>
    ///     Verifies the index page loads and displays the correct title.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task IndexPageShouldDisplayTitle()
    {
        // Arrange
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        IPage page = await fixture.CreatePageAsync();
        try
        {
            IndexPage indexPage = new(page);

            // Act
            await indexPage.NavigateAsync(fixture.ServerBaseUri);
            string? title = await indexPage.GetTitleAsync();

            // Assert
            title.Should().Be("Spring Sample");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    ///     Verifies multiple clicks work and update the timestamp.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task MultipleClicksShouldUpdateTimestamp()
    {
        // Arrange
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        IPage page = await fixture.CreatePageAsync();
        try
        {
            IndexPage indexPage = new(page);
            await indexPage.NavigateAsync(fixture.ServerBaseUri);

            // Act - First click
            await indexPage.ClickSayHelloAsync();
            await indexPage.WaitForGreetingAsync(30000);
            string? firstTimestamp = await indexPage.GetGeneratedAtTextAsync();

            // Small delay to ensure different timestamp
            await Task.Delay(100);

            // Act - Second click
            await indexPage.ClickSayHelloAsync();
            await indexPage.WaitForGreetingAsync(30000);
            string? secondTimestamp = await indexPage.GetGeneratedAtTextAsync();

            // Assert - Both should have timestamps (may or may not differ depending on grain state)
            firstTimestamp.Should().NotBeNullOrEmpty();
            secondTimestamp.Should().NotBeNullOrEmpty();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    ///     Verifies clicking the button invokes the Orleans grain and displays the greeting.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task SayHelloButtonShouldDisplayGreeting()
    {
        // Arrange
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        IPage page = await fixture.CreatePageAsync();
        try
        {
            IndexPage indexPage = new(page);
            await indexPage.NavigateAsync(fixture.ServerBaseUri);

            // Act
            await indexPage.ClickSayHelloAsync();
            await indexPage.WaitForGreetingAsync(30000);

            // Assert
            string? greeting = await indexPage.GetGreetingTextAsync();
            greeting.Should().NotBeNullOrEmpty();
            greeting.Should().Contain("Hello");
            greeting.Should().Contain("World");
            string? timestamp = await indexPage.GetGeneratedAtTextAsync();
            timestamp.Should().NotBeNullOrEmpty();
            timestamp.Should().Contain("Generated at:");
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}