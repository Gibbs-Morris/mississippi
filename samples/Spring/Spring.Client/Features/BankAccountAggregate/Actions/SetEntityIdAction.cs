#if false
using Mississippi.Reservoir.Abstractions.Actions;


namespace Spring.Client.Features.BankAccountAggregate.Actions;

/// <summary>
///     Action dispatched to set the current entity ID for targeting commands.
/// </summary>
/// <remarks>
///     This is an application-specific action for tracking UI selection state.
///     It is not part of the framework's generated code because entity selection
///     patterns vary by application.
/// </remarks>
/// <param name="EntityId">The entity ID to set as current.</param>
internal sealed record SetEntityIdAction(string EntityId) : IAction;
#endif