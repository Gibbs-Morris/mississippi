using System;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Client.Abstractions.Actions;

/// <summary>
///     Represents the primary action dispatched to start a saga.
/// </summary>
/// <remarks>
///     <para>
///         This interface extends <see cref="IAction" /> with the required <see cref="SagaId" />
///         property for identifying saga instances. Unlike command actions which target an entity
///         by string ID, saga actions target a saga instance by <see cref="Guid" />.
///     </para>
///     <para>
///         For example, a <c>StartTransferFundsSagaAction</c> would implement this interface
///         and provide a unique saga instance ID.
///     </para>
/// </remarks>
public interface ISagaAction : IAction
{
    /// <summary>
    ///     Gets an optional correlation ID for distributed tracing.
    /// </summary>
    /// <remarks>
    ///     Used to correlate related operations across services.
    /// </remarks>
    string? CorrelationId { get; }

    /// <summary>
    ///     Gets the unique identifier for the saga instance.
    /// </summary>
    /// <remarks>
    ///     This identifies which saga instance should be started or targeted.
    /// </remarks>
    Guid SagaId { get; }
}