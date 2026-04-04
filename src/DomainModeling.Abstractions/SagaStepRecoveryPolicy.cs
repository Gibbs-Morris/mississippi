namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Describes whether a saga step or compensation path may be replayed automatically.
/// </summary>
public enum SagaStepRecoveryPolicy
{
    /// <summary>
    ///     The framework may retry the step or compensation path automatically.
    /// </summary>
    Automatic,

    /// <summary>
    ///     The framework must block and wait for explicit operator intervention.
    /// </summary>
    ManualOnly,
}