using System;
using System.Collections.Generic;


namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Registry providing discovery of saga steps and compensations for a given saga type.
/// </summary>
/// <typeparam name="TSaga">The saga state type used to filter and discover steps.</typeparam>
public interface ISagaStepRegistry<TSaga>
    where TSaga : class
{
    /// <summary>
    ///     Gets the saga type this registry is for.
    /// </summary>
    Type SagaType => typeof(TSaga);

    /// <summary>
    ///     Gets a hash representing the step definitions for version tracking.
    /// </summary>
    string StepHash { get; }

    /// <summary>
    ///     Gets all registered steps in execution order.
    /// </summary>
    IReadOnlyList<ISagaStepInfo> Steps { get; }
}