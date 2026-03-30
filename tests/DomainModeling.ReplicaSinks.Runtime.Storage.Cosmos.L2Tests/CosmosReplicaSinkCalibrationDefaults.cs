namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.L2Tests;

/// <summary>
///     Defines the shared constants and key formats used by the Cosmos calibration harness.
/// </summary>
internal static class CosmosReplicaSinkCalibrationDefaults
{
    /// <summary>
    ///     The keyed DI registration name used for the emulator-backed Cosmos client.
    /// </summary>
    public const string ClientKey = "cosmos-emulator";

    /// <summary>
    ///     The stable contract identity emitted by the deterministic workload runner.
    /// </summary>
    public const string ContractIdentity = "MississippiTests.ReplicaSinks.Calibration.V1";

    /// <summary>
    ///     The provider-neutral target name used by the calibration scenarios.
    /// </summary>
    public const string TargetName = "orders-read";

    /// <summary>
    ///     Creates the runtime delivery key for a deterministic calibration entity.
    /// </summary>
    /// <param name="sinkKey">The sink key receiving the write.</param>
    /// <param name="entityIndex">The deterministic entity index.</param>
    /// <returns>The runtime delivery key for the deterministic entity.</returns>
    public static string CreateDeliveryKey(
        string sinkKey,
        int entityIndex
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sinkKey);
        ArgumentOutOfRangeException.ThrowIfNegative(entityIndex);
        return $"Calibration::{sinkKey}::{TargetName}::entity-{entityIndex:D4}";
    }
}
