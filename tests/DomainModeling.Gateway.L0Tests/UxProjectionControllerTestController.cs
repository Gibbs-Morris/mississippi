using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Gateway;
using Mississippi.DomainModeling.Gateway.L0Tests;


namespace MississippiTests.DomainModeling.Gateway.L0Tests;

/// <summary>
///     Exposes the mapped projection endpoint to isolated header tests.
/// </summary>
internal sealed class UxProjectionControllerTestController : UxProjectionControllerBase<TestProjection, TestDto>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UxProjectionControllerTestController" /> class.
    /// </summary>
    /// <param name="factory">The projection grain factory.</param>
    /// <param name="mapper">The projection mapper.</param>
    public UxProjectionControllerTestController(
        IUxProjectionGrainFactory factory,
        IMapper<TestProjection, TestDto> mapper
    )
        : base(factory, mapper, NullLogger<UxProjectionControllerBase<TestProjection, TestDto>>.Instance) =>
        ControllerContext = new()
        {
            HttpContext = new DefaultHttpContext(),
        };
}