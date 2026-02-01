using System;


namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Marks a class as a saga step and specifies its execution order.
/// </summary>
/// <remarks>
///     <para>
///         Steps are executed in ascending order based on the <see cref="Order" /> property.
///         Each step class should inherit from <see cref="SagaStepBase{TSaga}" />.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SagaStepAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaStepAttribute" /> class.
    /// </summary>
    /// <param name="order">The execution order of this step. Steps are executed in ascending order.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="order" /> is less than 1.</exception>
    public SagaStepAttribute(
        int order
    )
    {
        if (order < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(order), order, "Step order must be at least 1.");
        }

        Order = order;
    }

    /// <summary>
    ///     Gets the execution order of this step.
    ///     Steps are executed in ascending order, starting from the lowest order value.
    /// </summary>
    public int Order { get; }

    /// <summary>
    ///     Gets or sets an optional timeout for this step, overriding the saga-level default.
    ///     Format: TimeSpan string (e.g., "00:05:00" for 5 minutes).
    /// </summary>
    public string? Timeout { get; set; }
}