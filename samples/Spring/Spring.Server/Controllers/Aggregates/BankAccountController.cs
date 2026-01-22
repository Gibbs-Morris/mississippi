#if FALSE // Replaced by generated code
// NOTE: The generated version is in:
//   obj/[Config]/[TFM]/generated/Mississippi.Sdk.Server.Generators/
//   Mississippi.Sdk.Server.Generators.AggregateControllerGenerator/BankAccountController.g.cs
using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Aggregates.Api;
using Mississippi.Sdk.Generators.Abstractions;

using Spring.Domain.Aggregates.BankAccount;
using Spring.Domain.Aggregates.BankAccount.Commands;


namespace Spring.Server.Controllers.Aggregates;

/// <summary>
///     Controller for BankAccount aggregate commands.
///     This is a test controller to exercise the aggregate via HTTP.
/// </summary>
[Route("api/aggregates/bank-account/{entityId}")]
[PendingSourceGenerator]
public sealed class BankAccountController : AggregateControllerBase<BankAccountAggregate>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BankAccountController" /> class.
    /// </summary>
    /// <param name="aggregateGrainFactory">Factory for resolving aggregate grains.</param>
    /// <param name="openAccountMapper">Mapper for open account DTOs.</param>
    /// <param name="depositFundsMapper">Mapper for deposit funds DTOs.</param>
    /// <param name="withdrawFundsMapper">Mapper for withdraw funds DTOs.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public BankAccountController(
        IAggregateGrainFactory aggregateGrainFactory,
        IMapper<OpenAccountDto, OpenAccount> openAccountMapper,
        IMapper<DepositFundsDto, DepositFunds> depositFundsMapper,
        IMapper<WithdrawFundsDto, WithdrawFunds> withdrawFundsMapper,
        ILogger<BankAccountController> logger
    )
        : base(logger)
    {
        AggregateGrainFactory = aggregateGrainFactory;
        OpenAccountMapper = openAccountMapper;
        DepositFundsMapper = depositFundsMapper;
        WithdrawFundsMapper = withdrawFundsMapper;
    }

    private IAggregateGrainFactory AggregateGrainFactory { get; }

    private IMapper<DepositFundsDto, DepositFunds> DepositFundsMapper { get; }

    private IMapper<OpenAccountDto, OpenAccount> OpenAccountMapper { get; }

    private IMapper<WithdrawFundsDto, WithdrawFunds> WithdrawFundsMapper { get; }

    /// <summary>
    ///     Deposits funds into the bank account.
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="request">The deposit request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The operation result.</returns>
    [HttpPost("deposit")]
    public Task<ActionResult<OperationResult>> DepositFundsAsync(
        [FromRoute] string entityId,
        [FromBody] DepositFundsDto request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        return ExecuteAsync(entityId, DepositFundsMapper.Map(request), ExecuteCommandAsync, cancellationToken);
    }

    /// <summary>
    ///     Opens a new bank account.
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="request">The open account request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The operation result.</returns>
    [HttpPost("open")]
    public Task<ActionResult<OperationResult>> OpenAccountAsync(
        [FromRoute] string entityId,
        [FromBody] OpenAccountDto request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        return ExecuteAsync(entityId, OpenAccountMapper.Map(request), ExecuteCommandAsync, cancellationToken);
    }

    /// <summary>
    ///     Withdraws funds from the bank account.
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="request">The withdrawal request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The operation result.</returns>
    [HttpPost("withdraw")]
    public Task<ActionResult<OperationResult>> WithdrawFundsAsync(
        [FromRoute] string entityId,
        [FromBody] WithdrawFundsDto request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        return ExecuteAsync(entityId, WithdrawFundsMapper.Map(request), ExecuteCommandAsync, cancellationToken);
    }

    private Task<OperationResult> ExecuteCommandAsync<TCommand>(
        string entityId,
        TCommand command,
        CancellationToken cancellationToken
    )
        where TCommand : class
    {
        IGenericAggregateGrain<BankAccountAggregate> grain =
            AggregateGrainFactory.GetGenericAggregate<BankAccountAggregate>(entityId);
        return grain.ExecuteAsync(command, cancellationToken);
    }
}
#endif