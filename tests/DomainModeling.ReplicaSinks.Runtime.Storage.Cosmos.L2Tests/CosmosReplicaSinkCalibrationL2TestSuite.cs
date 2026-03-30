namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.L2Tests;

/// <summary>
///     Shares a single Cosmos emulator AppHost across the replica-sink calibration L2 suite.
/// </summary>
[CollectionDefinition(Name)]
#pragma warning disable CA1515 // xUnit collection definitions must be public for the existing repo test pattern
public sealed class CosmosReplicaSinkCalibrationL2TestSuite
    : ICollectionFixture<CosmosReplicaSinkCalibrationAppHostFixture>
#pragma warning restore CA1515
{
    /// <summary>
    ///     The shared xUnit collection name for the Cosmos calibration L2 suite.
    /// </summary>
    public const string Name = "Replica sink Cosmos calibration L2 tests";
}
