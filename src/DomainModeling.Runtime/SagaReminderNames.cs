namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Defines framework-owned Orleans reminder names for saga recovery.
/// </summary>
internal static class SagaReminderNames
{
    /// <summary>
    ///     The reminder name used for saga recovery wake-ups.
    /// </summary>
    public const string Recovery = "saga-recovery";
}