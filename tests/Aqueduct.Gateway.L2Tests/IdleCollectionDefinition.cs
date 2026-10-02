namespace Mississippi.Aqueduct.Gateway.L2Tests;

/// <summary>
///     Runs the collector host after other collections so localhost silo endpoints cannot conflict.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
#pragma warning disable CA1515 // xUnit requires a public collection definition.
public sealed class IdleCollectionDefinition : ICollectionFixture<IdleCollectionFixture>
#pragma warning restore CA1515
{
    /// <summary>
    ///     Identifies the isolated collector test collection.
    /// </summary>
    public const string Name = "Idle routing collection";
}