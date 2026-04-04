namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Authorizes access to saga resources using stored and current caller-context fingerprints.
/// </summary>
public interface ISagaAccessAuthorizer
{
    /// <summary>
    ///     Evaluates whether the current caller may perform the requested operation for the saga resource.
    /// </summary>
    /// <param name="entityId">The saga entity identifier.</param>
    /// <param name="action">The saga operation being requested.</param>
    /// <param name="storedAccessContextFingerprint">The fingerprint persisted with the saga recovery metadata.</param>
    /// <param name="currentAccessContextFingerprint">The fingerprint captured for the current caller context.</param>
    /// <returns>The authorization decision for the requested saga operation.</returns>
    SagaAccessAuthorizationResult Authorize(
        string entityId,
        SagaAccessAction action,
        string? storedAccessContextFingerprint,
        string? currentAccessContextFingerprint
    );
}

/// <summary>
///     Identifies the saga resource operation being authorized.
/// </summary>
public enum SagaAccessAction
{
    /// <summary>
    ///     Reads the raw saga state.
    /// </summary>
    ReadState,

    /// <summary>
    ///     Reads the metadata-only runtime recovery status.
    /// </summary>
    ReadRuntimeStatus,

    /// <summary>
    ///     Requests a manual saga resume.
    /// </summary>
    Resume,
}

/// <summary>
///     Represents the outcome of a saga access-authorization check.
/// </summary>
public sealed record SagaAccessAuthorizationResult
{
    /// <summary>
    ///     Gets a value indicating whether the request is authorized.
    /// </summary>
    public required bool IsAuthorized { get; init; }

    /// <summary>
    ///     Gets an optional denial reason suitable for operator-visible responses.
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    ///     Creates an authorization result that allows the request.
    /// </summary>
    /// <returns>An authorized result.</returns>
    public static SagaAccessAuthorizationResult Allow() => new()
    {
        IsAuthorized = true,
    };

    /// <summary>
    ///     Creates an authorization result that denies the request.
    /// </summary>
    /// <param name="failureReason">The optional operator-visible denial reason.</param>
    /// <returns>A denied result.</returns>
    public static SagaAccessAuthorizationResult Deny(
        string? failureReason = null
    ) => new()
    {
        FailureReason = failureReason,
        IsAuthorized = false,
    };
}
