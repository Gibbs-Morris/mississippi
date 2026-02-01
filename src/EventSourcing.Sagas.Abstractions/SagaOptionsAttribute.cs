using System;


namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Configures saga-level options including compensation strategy and timeouts.
/// </summary>
/// <remarks>
///     <para>
///         Apply this attribute to the saga state type to configure its behavior.
///         The saga state should also be decorated with
///         <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SagaOptionsAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets the compensation strategy to use when a step fails.
    ///     Defaults to <see cref="Abstractions.CompensationStrategy.Immediate" />.
    /// </summary>
    public CompensationStrategy CompensationStrategy { get; set; } = CompensationStrategy.Immediate;

    /// <summary>
    ///     Gets or sets the default timeout for all steps.
    ///     Format: TimeSpan string (e.g., "00:05:00" for 5 minutes).
    ///     Individual steps can override this using <see cref="SagaStepAttribute.Timeout" />.
    /// </summary>
    public string? DefaultStepTimeout { get; set; }

    /// <summary>
    ///     Gets or sets the maximum number of retry attempts for a step before failing.
    ///     Only used when <see cref="CompensationStrategy" /> is
    ///     <see cref="Abstractions.CompensationStrategy.RetryThenCompensate" />.
    ///     Defaults to 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    ///     Gets or sets the behavior when a step times out.
    ///     Defaults to <see cref="Abstractions.TimeoutBehavior.FailAndCompensate" />.
    /// </summary>
    public TimeoutBehavior TimeoutBehavior { get; set; } = TimeoutBehavior.FailAndCompensate;
}