using System;


namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Provides saga-level recovery metadata.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public sealed class SagaRecoveryInfoProvider<TSaga> : ISagaRecoveryInfoProvider<TSaga>
    where TSaga : class, ISagaState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaRecoveryInfoProvider{TSaga}" /> class.
    /// </summary>
    /// <param name="recovery">The saga recovery metadata.</param>
    public SagaRecoveryInfoProvider(
        SagaRecoveryInfo recovery
    )
    {
        ArgumentNullException.ThrowIfNull(recovery);
        Recovery = recovery;
    }

    /// <inheritdoc />
    public SagaRecoveryInfo Recovery { get; }
}
