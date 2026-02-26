using System;
using System.Linq;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Gateway;

using NSubstitute;

using Orleans;


namespace Mississippi.Inlet.Gateway.L0Tests;

/// <summary>
///     Tests authorization metadata behavior for <see cref="InletServerRegistrations.MapInletHub" />.
/// </summary>
public sealed class MapInletHubAuthTests
{
    /// <summary>
    ///     MapInletHub should not require authorization when generated authorization mode is disabled.
    /// </summary>
    [Fact]
    public void MapInletHubDoesNotRequireAuthorizationWhenModeDisabled()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        RegisterRequiredAqueductServices(builder.Services);
        builder.Services.AddInletServer(options => options.GeneratedApiAuthorization.Mode = GeneratedApiAuthorizationMode.Disabled);
        using WebApplication app = builder.Build();

        // Act
        app.MapInletHub();
        RouteEndpoint endpoint = GetInletHubEndpoint(app);

        // Assert
        Assert.DoesNotContain(endpoint.Metadata, metadata => metadata is IAuthorizeData);
    }

    /// <summary>
    ///     MapInletHub should require authorization when generated authorization mode is forced.
    /// </summary>
    [Fact]
    public void MapInletHubRequiresAuthorizationWhenModeForced()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        RegisterRequiredAqueductServices(builder.Services);
        builder.Services.AddInletServer(options =>
        {
            options.GeneratedApiAuthorization.Mode = GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints;
            options.GeneratedApiAuthorization.DefaultPolicy = "generated-policy";
        });

        using WebApplication app = builder.Build();

        // Act
        app.MapInletHub();
        RouteEndpoint endpoint = GetInletHubEndpoint(app);

        // Assert
        Assert.Contains(endpoint.Metadata, metadata => metadata is IAuthorizeData);
    }

    private static RouteEndpoint GetInletHubEndpoint(
        WebApplication app
    )
    {
        IEndpointRouteBuilder endpointRouteBuilder = app;
        return endpointRouteBuilder.DataSources
            .SelectMany(endpointDataSource => endpointDataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(routeEndpoint =>
            {
                string? rawText = routeEndpoint.RoutePattern.RawText;
                return rawText is not null &&
                       rawText.StartsWith("/hubs/inlet", StringComparison.Ordinal) &&
                       !rawText.Contains("negotiate", StringComparison.Ordinal);
            });
    }

    private static void RegisterRequiredAqueductServices(
        IServiceCollection services
    )
    {
        IServerIdProvider serverIdProvider = Substitute.For<IServerIdProvider>();
        serverIdProvider.ServerId.Returns("server-1");

        services.AddSingleton(serverIdProvider);
        services.AddSingleton(Substitute.For<IGrainFactory>());
        services.AddSingleton(Substitute.For<IConnectionRegistry>());
        services.AddSingleton(Substitute.For<ILocalMessageSender>());
        services.AddSingleton(Substitute.For<IHeartbeatManager>());
        services.AddSingleton(Substitute.For<IStreamSubscriptionManager>());
    }
}