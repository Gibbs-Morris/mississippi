using Mississippi.Reservoir.Abstractions.Actions;


namespace MississippiTests.Refraction.Client.StateManagement.L0Tests.Scenes;

/// <summary>
///     Requests one counter update for scene rendering tests.
/// </summary>
internal sealed record IncrementSceneCounterAction : IAction;