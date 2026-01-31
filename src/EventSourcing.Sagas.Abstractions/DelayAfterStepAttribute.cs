using System;


namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Specifies a delay to wait after this step completes before proceeding to the next step.
/// </summary>
/// <remarks>
///     <para>
///         The delay is applied after successful step execution but before the next step begins.
///         This is useful for demonstrations, rate-limiting saga progression, or simulating
///         real-world processing times.
///     </para>
///     <para>
///         During compensation, delays are NOT applied to maintain quick rollback behavior.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     [SagaStep(1)]
///     [DelayAfterStep(10_000)] // 10 second delay after this step
///     internal sealed class DebitSourceAccountStep : SagaStepBase&lt;TransferFundsSagaState&gt;
///     {
///         // Step implementation...
///     }
///     </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DelayAfterStepAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DelayAfterStepAttribute" /> class
    ///     with the specified delay in milliseconds.
    /// </summary>
    /// <param name="delayMilliseconds">The delay in milliseconds. Must be non-negative.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="delayMilliseconds" /> is negative.
    /// </exception>
    public DelayAfterStepAttribute(
        int delayMilliseconds
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegative(delayMilliseconds);
        DelayMilliseconds = delayMilliseconds;
    }

    /// <summary>
    ///     Gets the delay as a <see cref="TimeSpan" />.
    /// </summary>
    public TimeSpan Delay => TimeSpan.FromMilliseconds(DelayMilliseconds);

    /// <summary>
    ///     Gets the delay duration in milliseconds.
    /// </summary>
    public int DelayMilliseconds { get; }
}