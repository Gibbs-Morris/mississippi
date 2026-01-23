// =============================================================================
// HAND-CRAFTED REFERENCE IMPLEMENTATION
// =============================================================================
// This file contains the original hand-crafted version of this controller,
// created before source generation was automated via Sdk.Server.Generators.
//
// Purpose:
// - Serves as a reference implementation to validate generator output
// - Enables test comparisons between generated and expected code
// - Documents the intended structure and behavior of the generated controller
//
// The generator now produces this controller automatically. This file is
// commented out to avoid duplicate type definitions but preserved for testing
// and documentation.
// =============================================================================

#if false
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.EventSourcing.UxProjections.Abstractions;
using Mississippi.EventSourcing.UxProjections.Api;
using Mississippi.Inlet.Projection.Abstractions;
using Mississippi.Sdk.Generators.Abstractions;

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
///     <para>
///         The route must match the <see cref="ProjectionPathAttribute" /> on
///         <see cref="BankAccountBalanceProjection" /> for Inlet's AutoProjectionFetcher to work.
///     </para>
/// </remarks>
[Route("api/projections/bank-account-balance/{entityId}")]
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
#endif