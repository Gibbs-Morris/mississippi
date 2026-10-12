namespace MississippiTests.Inlet.Gateway.L0Tests.Authentication;

/// <summary>
///     Controls and records custom connection policy decisions for authentication regressions.
/// </summary>
internal sealed class ConnectionEvaluatorControl
{
    /// <summary>
    ///     Gets or sets the number of authentication calls.
    /// </summary>
    public int Authentications { get; set; }

    /// <summary>
    ///     Gets or sets the number of authorization calls.
    /// </summary>
    public int Authorizations { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the evaluator permits authentication.
    /// </summary>
    public bool CanAuthenticate { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether the evaluator permits authorization.
    /// </summary>
    public bool CanAuthorize { get; set; } = true;
}