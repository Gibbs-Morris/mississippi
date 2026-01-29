using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.Actions;

/// <summary>
///     Action dispatched when the application begins initialization.
/// </summary>
/// <remarks>
///     <para>
///         Dispatch this action at the start of your application's initialization sequence,
///         typically in <c>OnInitializedAsync</c> of your root component or in <c>Program.cs</c>.
///     </para>
///     <para>
///         This is a fire-and-forget action. The reducer updates <see cref="State.LifecycleState" />
///         to <see cref="State.LifecyclePhase.Initializing" />. You can register effects to perform
///         custom initialization logic.
///     </para>
/// </remarks>
public sealed record AppInitAction : IAction;