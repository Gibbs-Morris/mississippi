using Microsoft.AspNetCore.Mvc;

using Mississippi.Core.Abstractions.Cqrs.Query;
using Mississippi.Core.Cqrs.Query.Grains;


namespace Mississippi.Core.Web;

[ApiController]
[Route("query/{**rest}")]
public sealed class QueryController : ControllerBase
{
    public QueryController(
        IClusterClient clusterClient
    )
    {
        ClusterClient = clusterClient;
    }

    private IClusterClient ClusterClient { get; }
    // Notes = this is the single endpoint
    [HttpGet]
    Task<QuerySnapshot<object>> Handle([FromRoute] string rest)
    {
        // decode rest here and then route.
        // if not match throw error.
        var path = "";
        var version = 0;
        var x = ClusterClient.GetGrain<IQueryGrain<object>>(path);
    }
}

[Route("command/{**rest}")]
public sealed class CommandController
{
    [HttpPost]
    Task<QuerySnapshot<object>> Handle([FromRoute] string rest, [FromBody] object Command)
    {
        // decode rest here and then route.
        // if not match throw error.
    }
}



// One Controller /query/get
// One Service /query/get<T>
// One Grain /query/get<t>

