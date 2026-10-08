using System.Reflection;

using Microsoft.AspNetCore.Mvc.Controllers;


namespace Mississippi.Inlet.Gateway.L0Tests.CommandUrls;

/// <summary>Discovers the internal fixture without changing MVC routing or model binding.</summary>
internal sealed class CommandRouteBindingFeatureProvider : ControllerFeatureProvider
{
    /// <inheritdoc />
    protected override bool IsController(
        TypeInfo typeInfo
    ) =>
        typeInfo.AsType() == typeof(CommandRouteBindingController);
}