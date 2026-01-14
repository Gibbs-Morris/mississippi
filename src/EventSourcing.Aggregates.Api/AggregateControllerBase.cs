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
///                 private readonly IUserService _service;
///                 public UserController(
///                     IUserService service,
///                     ILogger&lt;UserController&gt; logger) : base(logger)
///                 {
///                     _service = service;
///                 }
///                 [HttpPost("register")]
///                 public Task&lt;ActionResult&lt;OperationResult&gt;&gt; RegisterAsync(
///                     [FromRoute] string entityId,
///                     [FromBody] RegisterUser command,
///                     CancellationToken ct = default)
///                     =&gt; ExecuteAsync(entityId, command, _service.RegisterAsync, ct);
///             }
///         </code>
///     </para>
///     <para>
///         This base class provides:
///         <list type="bullet">
///             <item>Common logging for command execution at the API layer.</item>
///             <item>Consistent error handling and HTTP response formatting.</item>
///             <item>The <c>ExecuteAsync</c> helper method that delegates to service methods.</item>
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
    /// <param name="logger">The logger for diagnostic output.</param>
    protected AggregateControllerBase(
        ILogger<AggregateControllerBase<TAggregate>> logger
    ) =>
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    ///     Gets the logger for diagnostic output.
    /// </summary>
    protected ILogger<AggregateControllerBase<TAggregate>> Logger { get; }

    /// <summary>
    ///     Executes a command via the service layer and returns an appropriate HTTP response.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="serviceMethod">The service method to invoke.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    ///     An <see cref="ActionResult{OperationResult}" /> with status 200 OK if successful,
    ///     or status 400 Bad Request if the command failed.
    /// </returns>
    protected virtual async Task<ActionResult<OperationResult>> ExecuteAsync<TCommand>(
        string entityId,
        TCommand command,
        Func<string, TCommand, CancellationToken, Task<OperationResult>> serviceMethod,
        CancellationToken cancellationToken = default
    )
        where TCommand : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(serviceMethod);
        string commandType = typeof(TCommand).Name;
        Logger.ExecutingCommand(entityId, commandType, AggregateTypeName);
        ActionResult? shortCircuit = await OnBeforeExecuteAsync(entityId, command, cancellationToken);
        if (shortCircuit is not null)
        {
            return shortCircuit;
        }

        try
        {
            OperationResult result = await serviceMethod(entityId, command, cancellationToken);
            await OnAfterExecuteAsync(entityId, command, result, cancellationToken);
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
    ///     Called after a command is executed. Override to add post-execution logic
    ///     such as additional response headers or metrics.
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
    ///     Called before a command is executed. Override to add pre-execution logic
    ///     such as additional authorization or request validation.
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