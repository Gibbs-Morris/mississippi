using System;

using Mississippi.Ripples.Abstractions.Actions;


namespace Mississippi.Ripples.Abstractions;

/// <summary>
///     Intercepts actions before and after they reach reducers.
/// </summary>
/// <remarks>
///     <para>
///         Middleware forms a pipeline between <see cref="IRippleStore.Dispatch" /> and reducers.
///         Each middleware can inspect, transform, or block actions, and perform side operations
///         like logging, analytics, or persistence.
///     </para>
///     <para>
///         Call the <c>dispatch</c> delegate to continue the pipeline. If not called, the action
///         is effectively blocked from reaching subsequent middleware and reducers.
///     </para>
/// </remarks>
/// <example>
///     <code>
///         public sealed class LoggingMiddleware : IMiddleware
///         {
///             private readonly ILogger _logger;
///
///             public LoggingMiddleware(ILogger logger) => _logger = logger;
///
///             public void Invoke(IAction action, Action&lt;IAction&gt; dispatch)
///             {
///                 _logger.LogDebug("Dispatching: {ActionType}", action.GetType().Name);
///                 dispatch(action);
///                 _logger.LogDebug("Dispatched: {ActionType}", action.GetType().Name);
///             }
///         }
///     </code>
/// </example>
public interface IMiddleware
{
    /// <summary>
    ///     Processes an action in the middleware pipeline.
    /// </summary>
    /// <param name="action">The action being dispatched.</param>
    /// <param name="dispatch">The dispatch function to continue the pipeline.</param>
    void Invoke(
        IAction action,
        Action<IAction> dispatch
    );
}