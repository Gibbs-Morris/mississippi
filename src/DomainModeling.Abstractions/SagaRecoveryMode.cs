namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Describes whether a saga should arm automatic recovery infrastructure.
/// </summary>
public enum SagaRecoveryMode
{
    /// <summary>
    ///     The saga participates in automatic reminder-driven recovery.
    /// </summary>
    Automatic,

    /// <summary>
    ///     The saga never arms automatic recovery and must be resumed manually.
    /// </summary>
    ManualOnly,
}