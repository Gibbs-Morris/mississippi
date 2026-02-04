using System;
using System.Collections.Generic;


namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Provides saga step metadata for orchestration.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public sealed class SagaStepInfoProvider<TSaga> : ISagaStepInfoProvider<TSaga>
    where TSaga : class, ISagaState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaStepInfoProvider{TSaga}" /> class.
    /// </summary>
    /// <param name="steps">The ordered saga step metadata.</param>
    public SagaStepInfoProvider(
        IReadOnlyList<SagaStepInfo> steps
    )
    {
        ArgumentNullException.ThrowIfNull(steps);
        Steps = steps;
    }

    /// <inheritdoc />
    public IReadOnlyList<SagaStepInfo> Steps { get; }
}