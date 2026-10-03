using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
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
        IAuthenticationService? authenticationService = null,
        AuthorizationPolicy? defaultPolicy = null
    )
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddAuthentication(BearerScheme).AddBearerToken(BearerScheme).AddBearerToken(OtherScheme);
        if (authenticationService is not null)
        {
            services.AddSingleton(authenticationService);
        }

        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(
                defaultPolicy ??
                new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireClaim("permission", "read").Build());
        return services.BuildServiceProvider();
    }

    /// <summary>
    ///     An unregistered selected scheme should fail with the existing generic subscription denial.
    /// </summary>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Fact]
    public async Task SubscribeDeniesUnregisteredSelectedScheme()
    {
        await using ServiceProvider services = CreateServices();
        using InletHub hub = CreateHub(
            services,
            "Unregistered",
            out IInletSubscriptionGrain grain,
            out ILogger<InletHub> _);
        HubException exception =
            await Assert.ThrowsAsync<HubException>(() => hub.SubscribeAsync(ProjectionPath, EntityId));
        Assert.Equal(InletHubConstants.SubscriptionDeniedMessage, exception.Message);
        await grain.DidNotReceive().SubscribeAsync(Arg.Any<string>(), Arg.Any<string>());
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

    /// <summary>
    ///     The last successful selected handler should have the same primary identity as ASP.NET.
    /// </summary>
    /// <param name="reverseSchemes">Whether the policy selects the handlers in reverse order.</param>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SubscribePreservesSelectedHandlerIdentityPrecedence(
        bool reverseSchemes
    )
    {
        string firstScheme = reverseSchemes ? OtherScheme : BearerScheme;
        string lastScheme = reverseSchemes ? BearerScheme : OtherScheme;
        IAuthenticationService authenticationService = Substitute.For<IAuthenticationService>();
        foreach (string scheme in new[] { BearerScheme, OtherScheme })
        {
            authenticationService.AuthenticateAsync(Arg.Any<HttpContext>(), scheme)
                .Returns(AuthenticateResult.Success(new(CreatePrincipal(scheme), scheme)));
        }

        AuthorizationPolicy policy = new AuthorizationPolicyBuilder(firstScheme, lastScheme).RequireAuthenticatedUser()
            .RequireAssertion(context => context.User.Identity?.AuthenticationType == lastScheme)
            .Build();
        await using ServiceProvider services = CreateServices(authenticationService, policy);
        DefaultHttpContext referenceContext = new()
        {
            RequestServices = services,
        };
        IPolicyEvaluator evaluator = services.GetRequiredService<IPolicyEvaluator>();
        AuthenticateResult authentication = await evaluator.AuthenticateAsync(policy, referenceContext);
        Assert.True(authentication.Succeeded);
        Assert.Equal(
            new[] { lastScheme, firstScheme },
            referenceContext.User.Identities.Select(identity => identity.AuthenticationType));
        Assert.True((await evaluator.AuthorizeAsync(policy, authentication, referenceContext, null)).Succeeded);
        using InletHub hub = CreateHub(
            services,
            firstScheme + ", " + lastScheme,
            out IInletSubscriptionGrain grain,
            out ILogger<InletHub> _);
        Assert.Equal("subscription-1", await hub.SubscribeAsync(ProjectionPath, EntityId));
        await grain.Received(1).SubscribeAsync(ProjectionPath, EntityId);
    }

    /// <summary>
    ///     Errors inside registered handlers should retain their original failure behavior.
    /// </summary>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Fact]
    public async Task SubscribePropagatesRegisteredHandlerFailures()
    {
        IAuthenticationService authenticationService = Substitute.For<IAuthenticationService>();
        InvalidOperationException failure = new("Registered handler failure");
        authenticationService.AuthenticateAsync(Arg.Any<HttpContext>(), BearerScheme)
            .Returns(Task.FromException<AuthenticateResult>(failure));
        await using ServiceProvider services = CreateServices(authenticationService);
        using InletHub hub = CreateHub(
            services,
            BearerScheme,
            out IInletSubscriptionGrain grain,
            out ILogger<InletHub> _);
        Assert.Same(
            failure,
            await Assert.ThrowsAsync<InvalidOperationException>(() => hub.SubscribeAsync(ProjectionPath, EntityId)));
        await grain.DidNotReceive().SubscribeAsync(Arg.Any<string>(), Arg.Any<string>());
    }
}