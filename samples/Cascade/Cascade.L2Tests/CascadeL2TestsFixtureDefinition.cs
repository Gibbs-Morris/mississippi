namespace Cascade.L2Tests;

/// <summary>
///     Collection definition for Cascade L2 tests.
///     All tests in this collection share the same <see cref="PlaywrightFixture" />
///     and run sequentially (not in parallel).
/// </summary>
[CollectionDefinition("Cascade L2 Tests", DisableParallelization = true)]
public sealed class CascadeL2TestsFixtureDefinition : ICollectionFixture<PlaywrightFixture>
{
    // This class has no code; it's used as an anchor for the collection definition.
}