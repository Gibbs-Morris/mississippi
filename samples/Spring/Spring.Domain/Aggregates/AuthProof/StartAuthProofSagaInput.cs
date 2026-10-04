using Orleans;


namespace MississippiSamples.Spring.Domain.Aggregates.AuthProof;

/// <summary>
///     Input used to start the auth-proof saga endpoint surface.
/// </summary>
[GenerateSerializer]
[Alias("MississippiSamples.Spring.Domain.Aggregates.AuthProof.StartAuthProofSagaInput")]
public sealed record StartAuthProofSagaInput
{
    /// <summary>
    ///     Gets the marker value used for local auth-proof requests.
    /// </summary>
    [Id(0)]
    public string Marker { get; init; } = "auth-proof";
}