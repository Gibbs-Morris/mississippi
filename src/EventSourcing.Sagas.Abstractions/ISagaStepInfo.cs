using System;


namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Describes a registered saga step including its order, type, and associated compensation.
/// </summary>
public interface ISagaStepInfo
{
    /// <summary>
    ///     Gets the compensation type for this step, or <c>null</c> if none.
    /// </summary>
    Type? CompensationType { get; }

    /// <summary>
    ///     Gets the step name (typically the class name).
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Gets the step execution order (1-based).
    /// </summary>
    int Order { get; }

    /// <summary>
    ///     Gets the step implementation type.
    /// </summary>
    Type StepType { get; }

    /// <summary>
    ///     Gets the step timeout, or <c>null</c> to use saga defaults.
    /// </summary>
    TimeSpan? Timeout { get; }
}