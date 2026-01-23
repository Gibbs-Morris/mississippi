using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Blazor.WebAssembly.Abstractions.Actions;

/// <summary>
///     Represents the primary action dispatched to execute an aggregate command.
/// </summary>
/// <remarks>
///     <para>
///         This interface extends <see cref="IAction" /> with the required <see cref="EntityId" />
///         property for targeting aggregate instances. All command actions should implement this
///         to enable the effect infrastructure to extract the target entity.
///     </para>
///     <para>
///         For example, a <c>DepositFundsAction</c> would implement this interface
///         and provide the bank account ID as the <see cref="EntityId" />.
///     </para>
/// </remarks>
public interface ICommandAction : IAction
{
    /// <summary>
    ///     Gets the target entity ID for the command.
    /// </summary>
    /// <remarks>
    ///     This identifies which aggregate instance should process the command.
    /// </remarks>
    string EntityId { get; }
}