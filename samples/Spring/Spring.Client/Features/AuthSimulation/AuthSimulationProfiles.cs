namespace Mississippi.Spring.Client.Features.AuthSimulation;

/// <summary>
///     Predefined local-auth simulation persona actions.
/// </summary>
internal static class AuthSimulationProfiles
{
    /// <summary>
    ///     Gets persona action for auth-proof claim only.
    /// </summary>
    public static SetAuthSimulationProfileAction AuthProofClaim { get; } = new(
        "AuthProof Claim",
        "Expected auth-proof results: claim endpoints 200, role endpoints 403.",
        false,
        "banking-operator,transfer-operator",
        "spring.permission=auth-proof");

    /// <summary>
    ///     Gets persona action for auth-proof role only.
    /// </summary>
    public static SetAuthSimulationProfileAction AuthProofRole { get; } = new(
        "AuthProof Role",
        "Expected auth-proof results: role endpoints 200, claim endpoints 403.",
        false,
        "banking-operator,transfer-operator,auth-proof-operator",
        null);

    /// <summary>
    ///     Gets persona action for full access.
    /// </summary>
    public static SetAuthSimulationProfileAction FullAccess { get; } = new(
        "Full Access",
        "Expected auth-proof results: authenticated endpoints 200, role endpoints 200, claim endpoints 200.",
        false,
        "banking-operator,transfer-operator,auth-proof-operator",
        "spring.permission=auth-proof");

    /// <summary>
    ///     Gets persona action for operator roles without auth-proof role/claim.
    /// </summary>
    public static SetAuthSimulationProfileAction OperatorRoles { get; } = new(
        "Operator Roles",
        "Expected auth-proof results: authenticated endpoints 200, auth-proof role/claim endpoints 403.",
        false,
        "banking-operator,transfer-operator",
        null);

    /// <summary>
    ///     Gets persona action that forces anonymous requests.
    /// </summary>
    public static SetAuthSimulationProfileAction Unauthenticated { get; } = new(
        "Unauthenticated",
        "Expected auth-proof results: protected endpoints return 401.",
        true,
        null,
        null);
}