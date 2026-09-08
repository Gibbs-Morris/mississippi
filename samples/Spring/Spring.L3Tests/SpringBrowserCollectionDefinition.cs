namespace MississippiSamples.Spring.L3Tests;

/// <summary>
///     Exposes xUnit collection metadata for the shared Spring L3 browser fixture.
/// </summary>
[CollectionDefinition(Name)]
public sealed class SpringBrowserCollectionDefinition : ICollectionFixture<SpringBrowserFixture>
{
    /// <summary>
    ///     The name of the Spring L3 browser test collection.
    /// </summary>
    public const string Name = "Spring L3 Browser Tests";
}