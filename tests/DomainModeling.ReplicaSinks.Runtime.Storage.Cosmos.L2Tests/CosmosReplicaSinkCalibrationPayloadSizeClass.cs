namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.L2Tests;

/// <summary>
///     Defines the deterministic payload envelope sizes used by the calibration scenarios.
/// </summary>
internal enum CosmosReplicaSinkCalibrationPayloadSizeClass
{
    /// <summary>
    ///     Uses the small payload envelope.
    /// </summary>
    Small = 0,

    /// <summary>
    ///     Uses the medium payload envelope.
    /// </summary>
    Medium = 1,
}
