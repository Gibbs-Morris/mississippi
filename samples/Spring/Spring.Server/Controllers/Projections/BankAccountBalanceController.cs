using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Mississippi.Common.Abstractions.Attributes;
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.EventSourcing.UxProjections.Abstractions;
using Mississippi.EventSourcing.UxProjections.Api;

using Spring.Domain.Projections.BankAccountBalance;


namespace Spring.Server.Controllers.Projections;

/// <summary>
///     Controller for the BankAccountBalance projection.
/// </summary>
/// <remarks>
///     <para>
///         This controller exposes the bank account balance projection as a RESTful API.
///         It maps the projection to a DTO before returning the response.
///     </para>
/// </remarks>
[Route("api/projections/bank-account-balance/{bankAccountBalanceId}")]
[PendingSourceGenerator]
public sealed class BankAccountBalanceController
    : UxProjectionControllerBase<BankAccountBalanceProjection, BankAccountBalanceDto>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BankAccountBalanceController" /> class.
    /// </summary>
    /// <param name="uxProjectionGrainFactory">Factory for resolving UX projection grains.</param>
    /// <param name="mapper">Mapper for projection to DTO conversion.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public BankAccountBalanceController(
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        IMapper<BankAccountBalanceProjection, BankAccountBalanceDto> mapper,
        ILogger<BankAccountBalanceController> logger
    )
        : base(uxProjectionGrainFactory, mapper, logger)
    {
    }
}