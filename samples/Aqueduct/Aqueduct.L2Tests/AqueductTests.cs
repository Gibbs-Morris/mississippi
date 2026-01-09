namespace Aqueduct.L2Tests;

/// <summary>
///     Test collection that shares the <see cref="AqueductFixture" /> across all Aqueduct L2 tests.
/// </summary>
[CollectionDefinition(Name)]
#pragma warning disable CA1515 // Types can be made internal - xUnit fixture must be public
#pragma warning disable CA1711 // Rename type - this is the xUnit pattern
public sealed class AqueductTests : ICollectionFixture<AqueductFixture>
#pragma warning restore CA1711
#pragma warning restore CA1515
{
    /// <summary>
    ///     The collection name for xUnit test discovery.
    /// </summary>
    public const string Name = "Aqueduct L2 Tests";
}
