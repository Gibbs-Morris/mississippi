using Allure.Xunit.Attributes;

using Microsoft.Playwright;


namespace Cascade.L2Tests.Features;

/// <summary>
///     Tests for storage operations (Cosmos DB and Blob Storage).
/// </summary>
[AllureParentSuite("Cascade.Web")]
[AllureSuite("Storage")]
[AllureSubSuite("Operations")]
#pragma warning disable CA1515 // Types can be made internal - xUnit test class must be public
public sealed class StorageOperationsTests : TestBase
#pragma warning restore CA1515
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="StorageOperationsTests" /> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public StorageOperationsTests(
        PlaywrightFixture fixture
    )
        : base(fixture)
    {
    }

    /// <summary>
    ///     Verifies Blob Storage read operation succeeds after write.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    [AllureId("web-storage-004")]
    public async Task BlobStorageReadAfterWriteReturnsContent()
    {
        // Arrange
        IPage page = await CreatePageAsync();
        await page.GotoAsync(
            Fixture.BaseUrl,
            new()
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 60000,
            });

        // Write first
        await page.ClickAsync("button:has-text('Write to Blob')");
        await page.WaitForSelectorAsync(
            "text=Written successfully",
            new()
            {
                Timeout = 30000,
            });

        // Act - Read
        await page.ClickAsync("button:has-text('Read from Blob')");

        // Wait for content
        await page.WaitForSelectorAsync(
            "text=Content:",
            new()
            {
                Timeout = 30000,
            });

        // Assert
        ILocator result = page.Locator("text=Content:");
        await Assertions.Expect(result).ToBeVisibleAsync();
    }

    /// <summary>
    ///     Verifies Blob Storage write operation succeeds.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    [AllureId("web-storage-003")]
    public async Task BlobStorageWriteSucceeds()
    {
        // Arrange
        IPage page = await CreatePageAsync();
        await page.GotoAsync(
            Fixture.BaseUrl,
            new()
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 60000,
            });

        // Act
        await page.ClickAsync("button:has-text('Write to Blob')");

        // Wait for success message
        await page.WaitForSelectorAsync(
            "text=Written successfully",
            new()
            {
                Timeout = 30000,
            });

        // Assert - Check blob result specifically
        ILocator result = page.Locator("p:has-text('Blob Result') + p >> text=Written successfully");
        await Assertions.Expect(result)
            .ToBeVisibleAsync(
                new()
                {
                    Timeout = 5000,
                });
    }

    /// <summary>
    ///     Verifies Cosmos DB read operation succeeds after write.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    [AllureId("web-storage-002")]
    public async Task CosmosDbReadAfterWriteReturnsData()
    {
        // Arrange
        IPage page = await CreatePageAsync();
        await page.GotoAsync(
            Fixture.BaseUrl,
            new()
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 60000,
            });

        // Write first
        await page.ClickAsync("button:has-text('Write to Cosmos')");
        await page.WaitForSelectorAsync(
            "text=Written successfully",
            new()
            {
                Timeout = 30000,
            });

        // Act - Read
        await page.ClickAsync("button:has-text('Read from Cosmos')");

        // Wait for data
        await page.WaitForSelectorAsync(
            "text=Found",
            new()
            {
                Timeout = 30000,
            });

        // Assert
        ILocator result = page.Locator("text=Found");
        await Assertions.Expect(result).ToBeVisibleAsync();
    }

    /// <summary>
    ///     Verifies Cosmos DB write operation succeeds.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    [AllureId("web-storage-001")]
    public async Task CosmosDbWriteSucceeds()
    {
        // Arrange
        IPage page = await CreatePageAsync();
        await page.GotoAsync(
            Fixture.BaseUrl,
            new()
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 60000,
            });

        // Act
        await page.ClickAsync("button:has-text('Write to Cosmos')");

        // Wait for success message
        await page.WaitForSelectorAsync(
            "text=Written successfully",
            new()
            {
                Timeout = 30000,
            });

        // Assert
        ILocator result = page.Locator("text=Written successfully");
        await Assertions.Expect(result).ToBeVisibleAsync();
    }
}