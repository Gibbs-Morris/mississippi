using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Mississippi.Common.Abstractions.Attributes;
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Aggregates.Api;

using Spring.Domain.Aggregates.BankAccount;
using Spring.Domain.Aggregates.BankAccount.Commands;


namespace Spring.Server.Controllers.Aggregates;

/// <summary>
///     Controller for BankAccount aggregate commands.
///     This is a test controller to exercise the aggregate via HTTP.
/// </summary>
[Route("api/aggregates/bankaccount/{id}")]
[PendingSourceGenerator]
public sealed class BankAccountController : AggregateControllerBase<BankAccountAggregate>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BankAccountController" /> class.
    /// </summary>
    /// <param name="aggregateGrainFactory">Factory for resolving aggregate grains.</param>
    /// <param name="openAccountMapper">Mapper for open account DTOs.</param>
    /// <param name="depositMapper">Mapper for deposit DTOs.</param>
    /// <param name="withdrawMapper">Mapper for withdraw DTOs.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public BankAccountController(
        IAggregateGrainFactory aggregateGrainFactory,
        IMapper<OpenAccountDto, OpenAccount> openAccountMapper,
        IMapper<DepositDto, DepositFunds> depositMapper,
        IMapper<WithdrawDto, WithdrawFunds> withdrawMapper,
        ILogger<BankAccountController> logger
    )
        : base(logger)
    {
        AggregateGrainFactory = aggregateGrainFactory;
        OpenAccountMapper = openAccountMapper;
        DepositMapper = depositMapper;
        WithdrawMapper = withdrawMapper;
    }

    private IAggregateGrainFactory AggregateGrainFactory { get; }

    private IMapper<DepositDto, DepositFunds> DepositMapper { get; }

    private IMapper<OpenAccountDto, OpenAccount> OpenAccountMapper { get; }

    private IMapper<WithdrawDto, WithdrawFunds> WithdrawMapper { get; }

    /// <summary>
    ///     Deposits funds into the bank account.
    /// </summary>
    /// <param name="id">The account identifier.</param>
    /// <param name="request">The deposit request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The operation result.</returns>
    [HttpPost("deposit")]
    public Task<ActionResult<OperationResult>> DepositAsync(
        [FromRoute] string id,
        [FromBody] DepositDto request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        return ExecuteAsync(id, DepositMapper.Map(request), ExecuteCommandAsync, cancellationToken);
    }

    /// <summary>
    ///     Opens a new bank account.
    /// </summary>
    /// <param name="id">The account identifier.</param>
    /// <param name="request">The open account request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The operation result.</returns>
    [HttpPost("open")]
    public Task<ActionResult<OperationResult>> OpenAsync(
        [FromRoute] string id,
        [FromBody] OpenAccountDto request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        return ExecuteAsync(id, OpenAccountMapper.Map(request), ExecuteCommandAsync, cancellationToken);
    }

    /// <summary>
    ///     Withdraws funds from the bank account.
    /// </summary>
    /// <param name="id">The account identifier.</param>
    /// <param name="request">The withdrawal request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The operation result.</returns>
    [HttpPost("withdraw")]
    public Task<ActionResult<OperationResult>> WithdrawAsync(
        [FromRoute] string id,
        [FromBody] WithdrawDto request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        return ExecuteAsync(id, WithdrawMapper.Map(request), ExecuteCommandAsync, cancellationToken);
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