using System;


namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Associates a compensation class with its corresponding saga step.
/// </summary>
/// <remarks>
///     <para>
///         Compensation classes should inherit from <see cref="SagaCompensationBase{TSaga}" />
///         and are invoked in reverse step order when a saga needs to roll back.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SagaCompensationAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaCompensationAttribute" /> class.
    /// </summary>
    /// <param name="forStep">The step type this compensation undoes.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="forStep" /> is null.</exception>
    public SagaCompensationAttribute(
        Type forStep
    )
    {
        ArgumentNullException.ThrowIfNull(forStep);
        ForStep = forStep;
    }

    /// <summary>
    ///     Gets the step type that this compensation undoes.
    /// </summary>
    public Type ForStep { get; }
}
