using Mississippi.Inlet.Client.Abstractions.Actions;


namespace MississippiTests.Inlet.Client.L0Tests.ActionEffects.Commands;

/// <summary>
///     Identifies the aggregate targeted by a test command.
/// </summary>
/// <param name="EntityId">The aggregate identifier.</param>
internal sealed record CommandEffectAction(string EntityId) : ICommandAction;