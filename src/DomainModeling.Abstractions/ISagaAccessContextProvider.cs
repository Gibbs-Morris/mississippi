namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Captures an immutable caller-context fingerprint that can be stored with saga recovery metadata.
/// </summary>
/// <remarks>
///     Implementations should return a stable, privacy-safe identifier representing the current caller or tenant scope.
///     The framework stores the returned fingerprint and later supplies it back to <see cref="ISagaAccessAuthorizer" />
///     for resource-level authorization checks on saga reads and manual resume.
/// </remarks>
public interface ISagaAccessContextProvider
{
    /// <summary>
    ///     Gets the current caller-context fingerprint.
    /// </summary>
    /// <returns>
    ///     A stable fingerprint for the current caller, or <c>null</c> when the host does not expose one.
    /// </returns>
    string? GetFingerprint();
}