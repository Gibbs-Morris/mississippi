using System;

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
///     <para>
///         The timestamp must be supplied by the caller at dispatch time (for example, from a
///         component-injected <see cref="TimeProvider" />).
///     </para>
/// </remarks>
/// <param name="InitializedAt">The timestamp for when initialization began.</param>
public sealed record AppInitAction(DateTimeOffset InitializedAt) : IAction;