using Mississippi.Inlet.Client.Abstractions.Actions;


namespace Mississippi.Inlet.Client.L0Tests.ActionEffects.CommandUrls;

/// <summary>Identifies the aggregate targeted by a command URL test.</summary>
/// <param name="EntityId">The target identifier.</param>
internal sealed record CommandUrlAction(string EntityId) : ICommandAction;