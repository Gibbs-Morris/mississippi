using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Refraction.Client.StateManagement.L0Tests.Scenes;

/// <summary>
///     Test action used by <see cref="ReservoirSceneBaseBehaviorTests" />.
/// </summary>
/// <param name="Value">The marker value captured by the test assertions.</param>
internal sealed record TestReservoirSceneAction(string Value) : IAction;
