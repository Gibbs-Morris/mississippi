namespace Cascade.L2Tests;

/// <summary>
///     Collection definition for Cascade L2 tests.
///     All tests in this collection share the same <see cref="PlaywrightFixture" />
///     and run sequentially (not in parallel).
/// </summary>
[CollectionDefinition("Cascade L2 Tests", DisableParallelization = true)]
#pragma warning disable CA1515 // Types can be made internal - xUnit requires public collection
public sealed class CascadeL2TestsFixtureDefinition : ICollectionFixture<PlaywrightFixture>
#pragma warning restore CA1515
{
    // This class has no code; it's used as an anchor for the collection definition.
}
