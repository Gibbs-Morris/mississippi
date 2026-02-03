using System;

namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Describes a saga step for orchestration.
/// </summary>
public sealed record SagaStepInfo
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaStepInfo" /> record.
    /// </summary>
    /// <param name="stepIndex">The zero-based step index.</param>
    /// <param name="stepName">The step name.</param>
    /// <param name="stepType">The step implementation type.</param>
    /// <param name="hasCompensation">Whether the step supports compensation.</param>
    public SagaStepInfo(
        int stepIndex,
        string stepName,
        Type stepType,
        bool hasCompensation
    )
    {
        ArgumentNullException.ThrowIfNull(stepName);
        ArgumentNullException.ThrowIfNull(stepType);
        StepIndex = stepIndex;
        StepName = stepName;
        StepType = stepType;
        HasCompensation = hasCompensation;
    }

    /// <summary>
    ///     Gets the zero-based step index.
    /// </summary>
    public int StepIndex { get; }

    /// <summary>
    ///     Gets the step name.
    /// </summary>
    public string StepName { get; }

    /// <summary>
    ///     Gets the step implementation type.
    /// </summary>
    public Type StepType { get; }

    /// <summary>
    ///     Gets a value indicating whether the step supports compensation.
    /// </summary>
    public bool HasCompensation { get; }
}
