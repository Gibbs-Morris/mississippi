using System;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Inlet.Gateway.Abstractions;
using Mississippi.Inlet.Runtime.Abstractions;
using Mississippi.Inlet.Runtime.Grains;

using NSubstitute;

using Orleans;


namespace Mississippi.Inlet.Gateway.L0Tests;

/// <summary>
///     Verifies subscription policies against real in-memory bearer authentication.
/// </summary>
public sealed class InletHubAuthenticationSchemeTests
{
    private const string BearerScheme = "Bearer";

    private const string EntityId = "entity-1";

    private const string IdentityAuthenticationType = "AuthenticationTypes.Federation";

    private const string OtherScheme = "Other";

    private const string PolicyName = "projection.read";

    private const string ProjectionPath = "/api/test-projection";

    private static DefaultHttpContext CreateHttpContext(
        IServiceProvider services,
        string identityAuthenticationType = IdentityAuthenticationType,
        string permission = "read",
        string tokenScheme = BearerScheme
    )
    {
        ClaimsPrincipal principal = new(
            new ClaimsIdentity(
                [
                    new(ClaimTypes.NameIdentifier, "user-1"), new("permission", permission),
                    new(ClaimTypes.Role, "reader"),
                ],
                identityAuthenticationType));
        AuthenticationTicket ticket = new(
            principal,
            new()
            {
                ExpiresUtc = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero),
            },
            tokenScheme + ":AccessToken");
        BearerTokenOptions options =
            services.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>().Get(tokenScheme);
        DefaultHttpContext context = new()
        {
            RequestServices = services,
            User = principal,
        };
        context.Request.Headers.Authorization = "Bearer " + options.BearerTokenProtector.Protect(ticket);
        return context;
    }

    private static InletHub CreateHub(
        IServiceProvider services,
        HttpContext httpContext,
        string policySource,
        out IInletSubscriptionGrain subscriptionGrain,
        string authenticationSchemes = BearerScheme,
        bool includeHttpContext = true
    )
    {
        IGrainFactory grainFactory = Substitute.For<IGrainFactory>();
        subscriptionGrain = Substitute.For<IInletSubscriptionGrain>();
        subscriptionGrain.SubscribeAsync(ProjectionPath, EntityId).Returns("subscription-1");
        grainFactory.GetGrain<IInletSubscriptionGrain>("connection-1").Returns(subscriptionGrain);
        IProjectionAuthorizationRegistry registry = Substitute.For<IProjectionAuthorizationRegistry>();
        ProjectionAuthorizationMetadata? metadata = policySource switch
        {
            "generated-default" => null,
            "named-policy" => new(PolicyName, null, null, true, false),
            "aspnet-default" => new(null, null, null, true, false),
            var _ => new(null, null, authenticationSchemes, true, false),
        };
        registry.GetAuthorizationMetadata(ProjectionPath).Returns(metadata);
        InletServerOptions options = new()
        {
            GeneratedApiAuthorization = new()
            {
                Mode = policySource == "generated-default"
                    ? GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints
                    : GeneratedApiAuthorizationMode.Disabled,
                DefaultAuthenticationSchemes = authenticationSchemes,
            },
        };
        FeatureCollection features = new();
        if (includeHttpContext)
        {
            IHttpContextFeature httpContextFeature = Substitute.For<IHttpContextFeature>();
            httpContextFeature.HttpContext.Returns(httpContext);
            features.Set(httpContextFeature);
        }

        HubCallerContext context = Substitute.For<HubCallerContext>();
        context.ConnectionId.Returns("connection-1");
        context.User.Returns(httpContext.User);
        context.Features.Returns(features);
        return new(
            grainFactory,
            registry,
            services.GetRequiredService<IAuthorizationService>(),
            services.GetRequiredService<IAuthorizationPolicyProvider>(),
            Options.Create(options),
            NullLogger<InletHub>.Instance)
        {
            Context = context,
        };
    }

    private static ServiceProvider CreateServices(
        bool defaultPolicySelectsBearer = false
    )
    {
        TimeProvider timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        ServiceCollection services = new();
        services.AddLogging();
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.AddAuthentication(BearerScheme)
            .AddBearerToken(BearerScheme, options => options.TimeProvider = timeProvider)
            .AddBearerToken(OtherScheme, options => options.TimeProvider = timeProvider);
        services.AddAuthorization(options =>
        {
            AuthorizationPolicyBuilder defaultPolicy = new();
            if (defaultPolicySelectsBearer)
            {
                defaultPolicy.AddAuthenticationSchemes(BearerScheme);
            }

            options.DefaultPolicy = RequireReadAccess(defaultPolicy).Build();
            options.AddPolicy(PolicyName, RequireReadAccess(new(BearerScheme)).Build());
        });
        return services.BuildServiceProvider();
    }

    private static AuthorizationPolicyBuilder RequireReadAccess(
        AuthorizationPolicyBuilder builder
    ) =>
        builder.RequireAuthenticatedUser().RequireClaim("permission", "read").RequireRole("reader");

    /// <summary>
    ///     Selected handler authentication should allow a different identity label through every policy source.
    /// </summary>
    /// <param name="policySource">The source of the policy's authentication scheme.</param>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Theory]
    [InlineData("projection")]
    [InlineData("generated-default")]
    [InlineData("named-policy")]
    [InlineData("aspnet-default")]
    public async Task SubscribeAcceptsSelectedHandlerWithDifferentIdentityLabel(
        string policySource
    )
    {
        await using ServiceProvider services = CreateServices(policySource == "aspnet-default");
        HttpContext httpContext = CreateHttpContext(services);
        IPolicyEvaluator evaluator = services.GetRequiredService<IPolicyEvaluator>();
        AuthorizationPolicy policy = RequireReadAccess(new(BearerScheme)).Build();
        AuthenticateResult authentication = await evaluator.AuthenticateAsync(policy, httpContext);
        Assert.True(authentication.Succeeded);
        Assert.Equal(IdentityAuthenticationType, httpContext.User.Identity?.AuthenticationType);
        PolicyAuthorizationResult httpAuthorization = await evaluator.AuthorizeAsync(
            policy,
            authentication,
            httpContext,
            null);
        Assert.True(httpAuthorization.Succeeded);
        using InletHub hub = CreateHub(services, httpContext, policySource, out IInletSubscriptionGrain grain);
        string subscriptionId = await hub.SubscribeAsync(ProjectionPath, EntityId);
        Assert.Equal("subscription-1", subscriptionId);
        await grain.Received(1).SubscribeAsync(ProjectionPath, EntityId);
    }

    /// <summary>
    ///     Handler authentication should also allow an identity label that equals the handler name.
    /// </summary>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Fact]
    public async Task SubscribeAcceptsSelectedHandlerWithMatchingIdentityLabel()
    {
        await using ServiceProvider services = CreateServices();
        HttpContext httpContext = CreateHttpContext(services, BearerScheme);
        Assert.True((await httpContext.AuthenticateAsync(BearerScheme)).Succeeded);
        using InletHub hub = CreateHub(services, httpContext, "projection", out IInletSubscriptionGrain grain);
        Assert.Equal("subscription-1", await hub.SubscribeAsync(ProjectionPath, EntityId));
        await grain.Received(1).SubscribeAsync(ProjectionPath, EntityId);
    }

    /// <summary>
    ///     Any successful selected handler should satisfy authentication when another selected handler fails.
    /// </summary>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Fact]
    public async Task SubscribeAcceptsWhenOneSelectedHandlerAuthenticates()
    {
        await using ServiceProvider services = CreateServices();
        HttpContext httpContext = CreateHttpContext(services);
        Assert.False((await httpContext.AuthenticateAsync(OtherScheme)).Succeeded);
        Assert.True((await httpContext.AuthenticateAsync(BearerScheme)).Succeeded);
        using InletHub hub = CreateHub(
            services,
            httpContext,
            "projection",
            out IInletSubscriptionGrain grain,
            OtherScheme + ", " + BearerScheme);
        Assert.Equal("subscription-1", await hub.SubscribeAsync(ProjectionPath, EntityId));
        await grain.Received(1).SubscribeAsync(ProjectionPath, EntityId);
    }

    /// <summary>
    ///     Failed selected-handler authentication must not create a subscription.
    /// </summary>
    /// <param name="failure">The authentication prerequisite that fails.</param>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Theory]
    [InlineData("missing-token")]
    [InlineData("invalid-token")]
    [InlineData("other-handler")]
    [InlineData("missing-http-context")]
    public async Task SubscribeDeniesWhenSelectedHandlerCannotAuthenticate(
        string failure
    )
    {
        await using ServiceProvider services = CreateServices();
        HttpContext httpContext = CreateHttpContext(
            services,
            tokenScheme: failure == "other-handler" ? OtherScheme : BearerScheme);
        if (failure == "missing-token")
        {
            httpContext.Request.Headers.Remove("Authorization");
        }
        else if (failure == "invalid-token")
        {
            httpContext.Request.Headers.Authorization = "Bearer invalid-token";
        }

        if (failure != "missing-http-context")
        {
            Assert.False((await httpContext.AuthenticateAsync(BearerScheme)).Succeeded);
        }

        using InletHub hub = CreateHub(
            services,
            httpContext,
            "projection",
            out IInletSubscriptionGrain grain,
            includeHttpContext: failure != "missing-http-context");
        HubException exception =
            await Assert.ThrowsAsync<HubException>(() => hub.SubscribeAsync(ProjectionPath, EntityId));
        Assert.Equal(InletHubConstants.SubscriptionDeniedMessage, exception.Message);
        await grain.DidNotReceive().SubscribeAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    /// <summary>
    ///     Successful selected-handler authentication must still satisfy authorization requirements.
    /// </summary>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Fact]
    public async Task SubscribeDeniesWhenSelectedHandlerLacksRequiredClaim()
    {
        await using ServiceProvider services = CreateServices();
        HttpContext httpContext = CreateHttpContext(services, permission: "write");
        Assert.True((await httpContext.AuthenticateAsync(BearerScheme)).Succeeded);
        using InletHub hub = CreateHub(services, httpContext, "projection", out IInletSubscriptionGrain grain);
        HubException exception =
            await Assert.ThrowsAsync<HubException>(() => hub.SubscribeAsync(ProjectionPath, EntityId));
        Assert.Equal(InletHubConstants.SubscriptionDeniedMessage, exception.Message);
        await grain.DidNotReceive().SubscribeAsync(Arg.Any<string>(), Arg.Any<string>());
    }
}