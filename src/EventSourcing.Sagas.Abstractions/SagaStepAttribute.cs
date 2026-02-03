using System;

namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Marks a class as a saga step and defines its execution order.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SagaStepAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaStepAttribute" /> class.
    /// </summary>
    /// <param name="order">The zero-based step order.</param>
    public SagaStepAttribute(int order)
    {
        Order = order;
    }

    /// <summary>
    ///     Gets the zero-based step order.
    /// </summary>
    public int Order { get; }

    /// <summary>
    ///     Gets or sets the saga state type when explicitly specified.
    /// </summary>
    public Type? Saga { get; set; }
}
