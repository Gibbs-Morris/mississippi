namespace Mississippi.DomainModeling.TestHarness.Architecture;

/// <summary>
///     Identifies why an exception rule exists.
/// </summary>
public enum AliasExceptionClassification
{
    /// <summary>
    ///     The exception exists only to exclude generated artifacts.
    /// </summary>
    GeneratedArtifactExclusion,

    /// <summary>
    ///     The exception exists because the type is not a contract the validator should enforce.
    /// </summary>
    NonContractHelper,

    /// <summary>
    ///     The exception exists because the alias intentionally preserves a non-current identity.
    /// </summary>
    IntentionalPreservedIdentity,
}