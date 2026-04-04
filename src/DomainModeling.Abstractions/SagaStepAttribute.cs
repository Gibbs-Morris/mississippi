using System;


namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Marks a class as a saga step and binds it to the specified saga state type.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SagaStepAttribute<TSaga> : Attribute
    where TSaga : class, ISagaState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaStepAttribute{TSaga}" /> class.
    /// </summary>
    /// <param name="order">The zero-based step order.</param>
    /// <param name="forwardRecoveryPolicy">The forward-path recovery policy.</param>
    public SagaStepAttribute(
        int order,
        SagaStepRecoveryPolicy forwardRecoveryPolicy
    )
    {
        Order = order;
        ForwardRecoveryPolicy = forwardRecoveryPolicy;
        Saga = typeof(TSaga);
    }

    /// <summary>
    ///     Gets the compensation-path recovery policy when the step implements
    ///     <see cref="ICompensatable{TSaga}" />; otherwise, this value is ignored.
    /// </summary>
    public SagaStepRecoveryPolicy CompensationRecoveryPolicy { get; init; }

    /// <summary>
    ///     Gets the forward-path recovery policy.
    /// </summary>
    public SagaStepRecoveryPolicy ForwardRecoveryPolicy { get; }

    /// <summary>
    ///     Gets the zero-based step order.
    /// </summary>
    public int Order { get; }

    /// <summary>
    ///     Gets the saga state type bound to the step.
    /// </summary>
    public Type Saga { get; }
}