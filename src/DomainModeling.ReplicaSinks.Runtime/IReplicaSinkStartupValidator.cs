namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Validates the minimal runnable onboarding shape for replica sinks.
/// </summary>
internal interface IReplicaSinkStartupValidator
{
    /// <summary>
    ///     Validates the current replica sink registrations and throws when onboarding prerequisites are missing.
    /// </summary>
    void Validate();
}