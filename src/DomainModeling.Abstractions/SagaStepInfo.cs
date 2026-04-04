using System;


namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Describes a saga step for orchestration.
/// </summary>
public sealed record SagaStepInfo
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaStepInfo" /> class.
    /// </summary>
    /// <param name="stepIndex">The zero-based step index.</param>
    /// <param name="stepName">The step name.</param>
    /// <param name="stepType">The step implementation type.</param>
    /// <param name="hasCompensation">Whether the step supports compensation.</param>
    /// <param name="forwardRecoveryPolicy">The forward-path recovery policy.</param>
    /// <param name="compensationRecoveryPolicy">The compensation-path recovery policy, when supported.</param>
    public SagaStepInfo(
        int stepIndex,
        string stepName,
        Type stepType,
        bool hasCompensation,
        SagaStepRecoveryPolicy forwardRecoveryPolicy,
        SagaStepRecoveryPolicy? compensationRecoveryPolicy
    )
    {
        ArgumentNullException.ThrowIfNull(stepName);
        ArgumentNullException.ThrowIfNull(stepType);
        if (hasCompensation && compensationRecoveryPolicy is null)
        {
            throw new ArgumentException(
                "Compensation recovery policy is required when the step supports compensation.",
                nameof(compensationRecoveryPolicy));
        }

        if (!hasCompensation && compensationRecoveryPolicy is not null)
        {
            throw new ArgumentException(
                "Compensation recovery policy must be null when the step does not support compensation.",
                nameof(compensationRecoveryPolicy));
        }

        StepIndex = stepIndex;
        StepName = stepName;
        StepType = stepType;
        HasCompensation = hasCompensation;
        ForwardRecoveryPolicy = forwardRecoveryPolicy;
        CompensationRecoveryPolicy = compensationRecoveryPolicy;
    }

    /// <summary>
    ///     Gets the compensation-path recovery policy when the step supports compensation.
    /// </summary>
    public SagaStepRecoveryPolicy? CompensationRecoveryPolicy { get; }

    /// <summary>
    ///     Gets the forward-path recovery policy.
    /// </summary>
    public SagaStepRecoveryPolicy ForwardRecoveryPolicy { get; }

    /// <summary>
    ///     Gets a value indicating whether the step supports compensation.
    /// </summary>
    public bool HasCompensation { get; }

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
}