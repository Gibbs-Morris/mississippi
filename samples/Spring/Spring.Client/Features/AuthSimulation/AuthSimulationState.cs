using Mississippi.Reservoir.Abstractions.State;


namespace MississippiSamples.Spring.Client.Features.AuthSimulation;

/// <summary>
///     Feature state for local-auth simulation headers applied to outgoing API requests.
/// </summary>
internal sealed record AuthSimulationState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "authSimulation";

    /// <summary>
    ///     Gets the claims header value.
    /// </summary>
    public string? Claims { get; init; } = "spring.permission=auth-proof";

    /// <summary>
    ///     Gets the active persona description.
    /// </summary>
    public string Description { get; init; } =
        "Expected auth-proof results: authenticated endpoints 200, role endpoints 200, claim endpoints 200.";

    /// <summary>
    ///     Gets a value indicating whether outgoing requests should force anonymous identity.
    /// </summary>
    public bool IsAnonymous { get; init; }

    /// <summary>
    ///     Gets the active persona display name.
    /// </summary>
    public string Name { get; init; } = "Full Access";

    /// <summary>
    ///     Gets the roles header value.
    /// </summary>
    public string? Roles { get; init; } = "banking-operator,transfer-operator,auth-proof-operator";
}