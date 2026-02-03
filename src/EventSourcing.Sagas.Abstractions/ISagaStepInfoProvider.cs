using System.Collections.Generic;

namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Provides saga step metadata for orchestration.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public interface ISagaStepInfoProvider<TSaga>
    where TSaga : class, ISagaState
{
    /// <summary>
    ///     Gets the ordered set of saga step metadata.
    /// </summary>
    IReadOnlyList<SagaStepInfo> Steps { get; }

    /// <summary>
    ///     Determines whether the provider applies to the supplied saga state.
    /// </summary>
    /// <param name="state">The saga state instance or null when starting.</param>
    /// <returns><c>true</c> if the provider applies to the supplied state.</returns>
    bool AppliesTo(TSaga? state) => true;
}
