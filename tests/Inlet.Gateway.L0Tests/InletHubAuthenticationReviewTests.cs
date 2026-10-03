using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Inlet.Gateway.Abstractions;
using Mississippi.Inlet.Runtime.Abstractions;
using Mississippi.Inlet.Runtime.Grains;

using NSubstitute;

using Orleans;


namespace Mississippi.Inlet.Gateway.L0Tests;

/// <summary>
///     Verifies subscription authentication review cases against ASP.NET authorization.
/// </summary>
public sealed class InletHubAuthenticationReviewTests
{
    private const string BearerScheme = "Bearer";

    private const string EntityId = "entity-1";

    private const string OtherScheme = "Other";

    private const string ProjectionPath = "/api/test-projection";

    private static InletHub CreateHub(
        IServiceProvider services,
        string authenticationSchemes,
        out IInletSubscriptionGrain subscriptionGrain,
        out ILogger<InletHub> logger
    )
    {
        IGrainFactory grainFactory = Substitute.For<IGrainFactory>();
        subscriptionGrain = Substitute.For<IInletSubscriptionGrain>();
        subscriptionGrain.SubscribeAsync(ProjectionPath, EntityId).Returns("subscription-1");
        grainFactory.GetGrain<IInletSubscriptionGrain>("connection-1").Returns(subscriptionGrain);
        IProjectionAuthorizationRegistry registry = Substitute.For<IProjectionAuthorizationRegistry>();
        registry.GetAuthorizationMetadata(ProjectionPath)
            .Returns(new ProjectionAuthorizationMetadata(null, null, authenticationSchemes, true, false));
        DefaultHttpContext httpContext = new()
        {
            RequestServices = services,
            User = CreatePrincipal("connection-user"),
        };
        IHttpContextFeature feature = Substitute.For<IHttpContextFeature>();
        feature.HttpContext.Returns(httpContext);
        FeatureCollection features = new();
        features.Set(feature);
        HubCallerContext context = Substitute.For<HubCallerContext>();
        context.ConnectionId.Returns("connection-1");
        context.User.Returns(httpContext.User);
        context.Features.Returns(features);
        logger = Substitute.For<ILogger<InletHub>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        return new(
            grainFactory,
            registry,
            services.GetRequiredService<IAuthorizationService>(),
            services.GetRequiredService<IAuthorizationPolicyProvider>(),
            Options.Create(new InletServerOptions()),
            logger)
        {
            Context = context,
        };
    }

    private static ClaimsPrincipal CreatePrincipal(
        string userId,
        string permission = "read"
    ) =>
        new(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, userId), new("permission", permission)], userId));

    private static ServiceProvider CreateServices(
        IAuthenticationService authenticationService
    )
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddAuthentication(BearerScheme).AddBearerToken(BearerScheme).AddBearerToken(OtherScheme);
        services.AddSingleton(authenticationService);
        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(
                new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireClaim("permission", "read").Build());
        return services.BuildServiceProvider();
    }

    /// <summary>
    ///     Authorization decisions should log the principal returned by the selected handler.
    /// </summary>
    /// <param name="allowed">Whether the selected principal has the required permission.</param>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SubscribeLogsSelectedPrincipalForAuthorizationDecision(
        bool allowed
    )
    {
        IAuthenticationService authenticationService = Substitute.For<IAuthenticationService>();
        ClaimsPrincipal selectedPrincipal = CreatePrincipal("selected-user", allowed ? "read" : "write");
        authenticationService.AuthenticateAsync(Arg.Any<HttpContext>(), BearerScheme)
            .Returns(AuthenticateResult.Success(new(selectedPrincipal, BearerScheme)));
        await using ServiceProvider services = CreateServices(authenticationService);
        using InletHub hub = CreateHub(
            services,
            BearerScheme,
            out IInletSubscriptionGrain grain,
            out ILogger<InletHub> logger);
        if (allowed)
        {
            Assert.Equal("subscription-1", await hub.SubscribeAsync(ProjectionPath, EntityId));
            await grain.Received(1).SubscribeAsync(ProjectionPath, EntityId);
        }
        else
        {
            HubException exception =
                await Assert.ThrowsAsync<HubException>(() => hub.SubscribeAsync(ProjectionPath, EntityId));
            Assert.Equal(InletHubConstants.SubscriptionDeniedMessage, exception.Message);
            await grain.DidNotReceive().SubscribeAsync(Arg.Any<string>(), Arg.Any<string>());
        }

        int eventId = allowed ? 7 : 8;
        object?[] arguments = Assert.Single(
                logger.ReceivedCalls(),
                call => (call.GetMethodInfo().Name == nameof(ILogger.Log)) &&
                        call.GetArguments()[1] is EventId id &&
                        (id.Id == eventId))
            .GetArguments();
        IEnumerable<KeyValuePair<string, object?>> state =
            Assert.IsType<IEnumerable<KeyValuePair<string, object?>>>(arguments[2], false);
        Assert.Equal("selected-user", Assert.Single(state, field => field.Key == "UserId").Value);
        Assert.Equal("connection-1", Assert.Single(state, field => field.Key == "ConnectionId").Value);
        Assert.Equal(allowed ? LogLevel.Debug : LogLevel.Warning, arguments[0]);
        Assert.Null(arguments[3]);
    }
}