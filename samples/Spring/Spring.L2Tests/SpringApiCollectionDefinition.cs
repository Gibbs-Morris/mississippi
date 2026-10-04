namespace MississippiSamples.Spring.L2Tests;

/// <summary>
///     Exposes xUnit collection metadata so Spring L2 API tests share a single AppHost instance.
///     This prevents container thrashing and significantly reduces test execution time.
/// </summary>
[CollectionDefinition(Name)]
public sealed class SpringApiCollectionDefinition : ICollectionFixture<SpringApplicationFixture>
{
    /// <summary>
    ///     The name of the test collection.
    /// </summary>
    public const string Name = "Spring L2 API Tests";
}