using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Spring.Client.Features.AuthSimulation;

/// <summary>
///     Action that sets the active auth simulation persona.
/// </summary>
/// <param name="Name">Persona display name.</param>
/// <param name="Description">Persona expectation description.</param>
/// <param name="IsAnonymous">Whether requests should force anonymous behavior.</param>
/// <param name="Roles">Role header value for requests.</param>
/// <param name="Claims">Claim header value for requests.</param>
internal sealed record SetAuthSimulationProfileAction(
    string Name,
    string Description,
    bool IsAnonymous,
    string? Roles,
    string? Claims
) : IAction;