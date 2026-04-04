namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Provides saga-level recovery metadata.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public interface ISagaRecoveryInfoProvider<in TSaga>
    where TSaga : class, ISagaState
{
    /// <summary>
    ///     Gets the registered saga recovery metadata.
    /// </summary>
    SagaRecoveryInfo Recovery { get; }

    /// <summary>
    ///     Determines whether the provider applies to the supplied saga state.
    /// </summary>
    /// <param name="state">The saga state instance or null when starting.</param>
    /// <returns><c>true</c> if the provider applies to the supplied state.</returns>
    bool AppliesTo(
        TSaga? state
    ) =>
        true;
}