using System;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.Actions;

/// <summary>
///     Action dispatched when the application has completed initialization and is ready for use.
/// </summary>
/// <remarks>
///     <para>
///         Dispatch this action after all initialization tasks have completed,
///         such as loading user preferences, establishing connections, or fetching initial data.
///     </para>
///     <para>
///         This is a fire-and-forget action. The reducer updates <see cref="State.LifecycleState" />
///         to <see cref="State.LifecyclePhase.Ready" />. You can register effects to perform
///         post-initialization tasks.
///     </para>
///     <para>
///         The timestamp must be supplied by the caller at dispatch time (for example, from a
///         component-injected <see cref="TimeProvider" />).
///     </para>
///     <para>
///         Components can conditionally render loading states by checking if the phase is Ready.
///     </para>
/// </remarks>
/// <param name="ReadyAt">The timestamp for when the application became ready.</param>
public sealed record AppReadyAction(DateTimeOffset ReadyAt) : IAction;