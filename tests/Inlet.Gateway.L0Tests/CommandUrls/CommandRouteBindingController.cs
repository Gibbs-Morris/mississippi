using Microsoft.AspNetCore.Mvc;


namespace Mississippi.Inlet.Gateway.L0Tests.CommandUrls;

/// <summary>Records the identifier delivered through aggregate-style MVC routing.</summary>
[ApiController]
[Route("api/aggregates/customer/{entityId}")]
internal sealed class CommandRouteBindingController : ControllerBase
{
    /// <summary>Gets the identifier received by the command dispatch boundary.</summary>
    public string? DispatchedEntityId { get; private set; }

    /// <summary>Dispatches the route-bound identifier without additional decoding.</summary>
    /// <param name="entityId">The identifier supplied by MVC route binding.</param>
    /// <returns>A successful command response.</returns>
    [HttpPost("submit")]
    public IActionResult Submit(
        [FromRoute] string entityId
    )
    {
        DispatchedEntityId = entityId;
        return Ok(
            new
            {
                Success = true,
            });
    }
}