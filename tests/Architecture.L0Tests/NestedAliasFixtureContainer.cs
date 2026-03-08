using Orleans;


namespace Mississippi.Architecture.L0Tests;

/// <summary>
///     Container fixture used to validate nested type alias handling.
/// </summary>
internal static class NestedAliasFixtureContainer
{
    /// <summary>
    ///     Nested alias fixture.
    /// </summary>
    [Alias("Wrong.NestedAliasFixture")]
    internal sealed class NestedAliasFixture
    {
    }
}