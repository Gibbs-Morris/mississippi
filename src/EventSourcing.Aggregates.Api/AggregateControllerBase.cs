using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Mississippi.EventSourcing.Aggregates.Api;

/// <summary>
///     Abstract base controller for exposing aggregate commands via HTTP endpoints.
/// </summary>
/// <typeparam name="TAggregate">The aggregate state type.</typeparam>
/// <remarks>
///     <para>
///         Inherit from this controller to expose aggregate commands as a RESTful API.
///         The derived class must apply a route attribute and can customize behavior
///         or add additional endpoints.
///     </para>
///     <para>
///         Example usage:
///         <code>
///             [Route("api/users/{entityId}")]
///             public class UserController : AggregateControllerBase&lt;UserAggregate&gt;
///             {
///                 public UserController(
///                     IAggregateGrainFactory factory,
///                     ILogger&lt;AggregateControllerBase&lt;UserAggregate&gt;&gt; logger) : base(factory, logger) { }
///
///                 [HttpPost("register")]
///                 public async Task&lt;ActionResult&lt;OperationResult&gt;&gt; RegisterAsync(
///                     [FromRoute] string entityId,
///                     [FromBody] RegisterUser command,
///                     CancellationToken ct = default)
///                 {
///                     return await ExecuteCommandAsync(entityId, command, ct);
///                 }
///             }
///         </code>
///     </para>
///     <para>
///         This base class provides:
///         <list type="bullet">
///             <item>Common logging for command execution.</item>
///             <item>Consistent error handling and response formatting.</item>
///             <item>Access to the aggregate grain factory.</item>
///             <item>The <c>ExecuteCommandAsync</c> helper method for command execution.</item>
///         </list>
///     </para>
/// </remarks>
[ApiController]
public abstract class AggregateControllerBase<TAggregate> : ControllerBase
    where TAggregate : class
{
    private static readonly string AggregateTypeName = typeof(TAggregate).Name;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AggregateControllerBase{TAggregate}" /> class.
    /// </summary>
    /// <param name="aggregateGrainFactory">Factory for resolving aggregate grains.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    protected AggregateControllerBase(
        IAggregateGrainFactory aggregateGrainFactory,
        ILogger<AggregateControllerBase<TAggregate>> logger
    )
    {
        AggregateGrainFactory = aggregateGrainFactory ?? throw new ArgumentNullException(nameof(aggregateGrainFactory));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Gets the factory for resolving aggregate grains.
    /// </summary>
    protected IAggregateGrainFactory AggregateGrainFactory { get; }

    /// <summary>
    ///     Gets the logger for diagnostic output.
    /// </summary>
    protected ILogger<AggregateControllerBase<TAggregate>> Logger { get; }

    /// <summary>
    ///     Executes a command against the aggregate and returns an appropriate HTTP response.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    ///     An <see cref="ActionResult{OperationResult}" /> with status 200 OK if successful,
    ///     or status 400 Bad Request if the command failed.
    /// </returns>
    protected virtual async Task<ActionResult<OperationResult>> ExecuteCommandAsync<TCommand>(
        string entityId,
        TCommand command,
        CancellationToken cancellationToken = default
    )
        where TCommand : class
    {
        string commandType = typeof(TCommand).Name;
        Logger.ExecutingCommand(entityId, commandType, AggregateTypeName);
        try
        {
            IGenericAggregateGrain<TAggregate> grain = AggregateGrainFactory.GetGenericAggregate<TAggregate>(entityId);
            OperationResult result = await grain.ExecuteAsync(command, cancellationToken);
            if (result.Success)
            {
                Logger.CommandSucceeded(entityId, commandType, AggregateTypeName);
                return Ok(result);
            }

            Logger.CommandFailed(entityId, commandType, result.ErrorMessage ?? "Unknown error", AggregateTypeName);
            return BadRequest(result);
        }
        catch (InvalidOperationException ex)
        {
            Logger.CommandException(entityId, commandType, ex, AggregateTypeName);
            return BadRequest(
                OperationResult.Fail("CommandExecutionFailed", $"Command execution failed: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Called after a command is executed successfully. Override to add post-execution logic.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="command">The command that was executed.</param>
    /// <param name="result">The operation result.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task OnAfterExecuteAsync<TCommand>(
        string entityId,
        TCommand command,
        OperationResult result,
        CancellationToken cancellationToken = default
    )
        where TCommand : class =>
        Task.CompletedTask;

    /// <summary>
    ///     Called before a command is executed. Override to add pre-execution logic.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    ///     An <see cref="ActionResult" /> if the request should be short-circuited,
    ///     or <c>null</c> to continue with execution.
    /// </returns>
    protected virtual Task<ActionResult?> OnBeforeExecuteAsync<TCommand>(
        string entityId,
        TCommand command,
        CancellationToken cancellationToken = default
    )
        where TCommand : class =>
        Task.FromResult<ActionResult?>(null);
}