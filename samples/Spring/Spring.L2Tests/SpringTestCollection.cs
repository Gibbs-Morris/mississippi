namespace Spring.L2Tests;

/// <summary>
///     xUnit collection definition that ensures all Spring tests share a single AppHost instance.
///     This prevents container thrashing and significantly reduces test execution time.
/// </summary>
[CollectionDefinition(Name)]
#pragma warning disable CA1515 // Types can be made internal - xUnit collection must be public
#pragma warning disable CA1711 // Rename type so it does not end in 'Collection' - required by xUnit
public sealed class SpringTestCollection : ICollectionFixture<SpringFixture>
#pragma warning restore CA1711
#pragma warning restore CA1515
{
    /// <summary>
    ///     The name of the test collection.
    /// </summary>
    public const string Name = "Spring Integration Tests";
}