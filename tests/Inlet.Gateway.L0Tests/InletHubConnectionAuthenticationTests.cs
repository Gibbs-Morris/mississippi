using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Inlet.Gateway.Abstractions;
using Mississippi.Inlet.Runtime.Abstractions;
using Mississippi.Inlet.Runtime.Grains;

using NSubstitute;

using Orleans;


namespace Mississippi.Inlet.Gateway.L0Tests;

/// <summary>
///     Verifies established scheme authentication across a reduced long-polling context.
/// </summary>
public sealed class InletHubConnectionAuthenticationTests
{
    private const string EntityId = "entity-1";

    private const string OtherScheme = "Other";

    private const string PolicyAlias = "Forwarded";

    private const string ProjectionPath = "/api/test-projection";

    private const string TlsScheme = "Certificate";

    private static DefaultHttpContext CloneConnectionContext(
        HttpContext original,
        IServiceProvider services
    )
    {
        DefaultHttpContext clone = new()
        {
            RequestServices = services,
            User = original.User,
            Items = new Dictionary<object, object?>(original.Items),
        };
        clone.Features.Set<IHttpRequestFeature>(
            new HttpRequestFeature
            {
                Scheme = original.Request.Scheme,
                Method = original.Request.Method,
                Path = original.Request.Path,
                QueryString = original.Request.QueryString.Value ?? string.Empty,
                Headers = original.Request.Headers,
            });
        return clone;
    }

    private static WebApplication CreateHost(
        string permission = "read",
        IPolicyEvaluator? evaluator = null,
        bool hasAmbiguousScheme = false,
        bool hasNonCacheablePolicyProvider = false,
        ClaimsPrincipal? authenticatedPrincipal = null
    )
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSignalR();
        builder.Services.AddAuthentication().AddBearerToken(TlsScheme).AddBearerToken(OtherScheme);
        if (hasAmbiguousScheme)
        {
            builder.Services.AddAuthentication().AddBearerToken(TlsScheme + ";" + OtherScheme);
        }

        builder.Services.AddAuthorizationBuilder()
            .SetDefaultPolicy(
                new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireClaim("permission", "read").Build());
        if (hasNonCacheablePolicyProvider)
        {
            IAuthorizationPolicyProvider provider = Substitute.For<IAuthorizationPolicyProvider>();
            provider.AllowsCachingPolicies.Returns(false);
            provider.GetDefaultPolicyAsync()
                .Returns(
                    new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                        .RequireClaim("permission", "read")
                        .Build());
            builder.Services.AddSingleton(provider);
        }

        IAuthenticationService authentication = Substitute.For<IAuthenticationService>();
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), TlsScheme)
            .Returns(call => call.Arg<HttpContext>().Features.Get<ITlsConnectionFeature>() is not null
                ? AuthenticateResult.Success(new(authenticatedPrincipal ?? CreatePrincipal(permission), TlsScheme))
                : AuthenticateResult.NoResult());
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), OtherScheme).Returns(AuthenticateResult.NoResult());
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), TlsScheme + ";" + OtherScheme)
            .Returns(call => call.Arg<HttpContext>().Features.Get<ITlsConnectionFeature>() is not null
                ? AuthenticateResult.Success(new(CreatePrincipal(permission), TlsScheme + ";" + OtherScheme))
                : AuthenticateResult.NoResult());
        builder.Services.AddSingleton(authentication);
        if (evaluator is not null)
        {
            builder.Services.AddSingleton(evaluator);
        }

        return builder.Build();
    }

    private static InletHub CreateHub(
        HttpContext httpContext,
        ClaimsPrincipal connectionUser,
        string authenticationSchemes,
        out IInletSubscriptionGrain subscriptionGrain
    )
    {
        IGrainFactory grainFactory = Substitute.For<IGrainFactory>();
        subscriptionGrain = Substitute.For<IInletSubscriptionGrain>();
        subscriptionGrain.SubscribeAsync(ProjectionPath, EntityId).Returns("subscription-1");
        grainFactory.GetGrain<IInletSubscriptionGrain>("connection-1").Returns(subscriptionGrain);
        IProjectionAuthorizationRegistry registry = Substitute.For<IProjectionAuthorizationRegistry>();
        registry.GetAuthorizationMetadata(ProjectionPath)
            .Returns(new ProjectionAuthorizationMetadata(null, null, authenticationSchemes, true, false));
        IHttpContextFeature feature = Substitute.For<IHttpContextFeature>();
        feature.HttpContext.Returns(httpContext);
        FeatureCollection features = new();
        features.Set(feature);
        HubCallerContext context = Substitute.For<HubCallerContext>();
        context.ConnectionId.Returns("connection-1");
        context.User.Returns(connectionUser);
        context.Features.Returns(features);
        return new(
            grainFactory,
            registry,
            httpContext.RequestServices.GetRequiredService<IAuthorizationService>(),
            httpContext.RequestServices.GetRequiredService<IAuthorizationPolicyProvider>(),
            Options.Create(new InletServerOptions()),
            NullLogger<InletHub>.Instance)
        {
            Context = context,
        };
    }

    private static DefaultHttpContext CreateOriginalContext(
        IServiceProvider services
    )
    {
        DefaultHttpContext context = new()
        {
            RequestServices = services,
        };
        context.Request.Scheme = "https";
        context.Features.Set(Substitute.For<ITlsConnectionFeature>());
        return context;
    }

    private static AuthorizationPolicy CreatePolicy(
        string authenticationSchemes
    ) =>
        new AuthorizationPolicyBuilder(authenticationSchemes.Split(',', StringSplitOptions.TrimEntries))
            .RequireAuthenticatedUser()
            .RequireClaim("permission", "read")
            .Build();

    private static ClaimsPrincipal CreatePrincipal(
        string permission
    ) =>
        new(
            new ClaimsIdentity(
                [new(ClaimTypes.NameIdentifier, "certificate-user"), new("permission", permission)],
                TlsScheme));

    private static async Task DispatchHubEndpointAsync(
        WebApplication app,
        HttpContext context,
        AuthorizationPolicy policy,
        bool requiresAuthorization = true
    )
    {
        HubEndpointConventionBuilder mapped = app.MapInletHub();
        if (requiresAuthorization)
        {
            mapped.RequireAuthorization(policy);
        }

        IEndpointRouteBuilder routeBuilder = app;
        RouteEndpoint endpoint = routeBuilder.DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(candidate => candidate.RoutePattern.RawText == "/hubs/inlet");
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/hubs/inlet";
        context.Request.QueryString = new("?id=unavailable-connection");
        context.SetEndpoint(endpoint);
        Assert.NotNull(endpoint.RequestDelegate);
        await endpoint.RequestDelegate(context);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    private sealed class FeatureDependentHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="FeatureDependentHandler" /> class.
        /// </summary>
        /// <param name="options">The scheme options.</param>
        /// <param name="logger">The handler logger factory.</param>
        /// <param name="encoder">The URL encoder.</param>
        public FeatureDependentHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder
        )
            : base(options, logger, encoder)
        {
        }

        /// <inheritdoc />
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(
                Context.Features.Get<ITlsConnectionFeature>() is not null
                    ? AuthenticateResult.Success(
                        new(
                            new(
                                new ClaimsIdentity(
                                    [new(ClaimTypes.NameIdentifier, "forwarded-user"), new("permission", "read")],
                                    PolicyAlias)),
                            Scheme.Name))
                    : AuthenticateResult.NoResult());
    }

    /// <summary>
    ///     SignalR's initial Windows clone can retain authentication without authorizing later replacements.
    /// </summary>
    /// <param name="replacesAfterConnection">Whether the retained principal changes after connection startup.</param>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SubscribeBindsInitialWindowsCloneOnly(
        bool replacesAfterConnection
    )
    {
        using WindowsIdentity? windowsIdentity = OperatingSystem.IsWindows() ? WindowsIdentity.GetCurrent() : null;
        ClaimsIdentity identity = windowsIdentity ?? new ClaimsIdentity(TlsScheme);
        identity.AddClaims([new(ClaimTypes.NameIdentifier, "windows-user"), new("permission", "read")]);
        await using WebApplication app = CreateHost(authenticatedPrincipal: new(identity));
        DefaultHttpContext original = CreateOriginalContext(app.Services);
        PolicyEvaluator referenceEvaluator = new(app.Services.GetRequiredService<IAuthorizationService>());
        AuthorizationPolicy policy = CreatePolicy(TlsScheme);
        AuthenticateResult established = await referenceEvaluator.AuthenticateAsync(policy, original);
        Assert.True(established.Succeeded);
        Assert.True((await referenceEvaluator.AuthorizeAsync(policy, established, original, null)).Succeeded);
        IAuthenticateResultFeature resultFeature = Substitute.For<IAuthenticateResultFeature>();
        resultFeature.AuthenticateResult.Returns(established);
        original.Features.Set(resultFeature);
        await DispatchHubEndpointAsync(app, original, policy);
        await using AsyncServiceScope connectionScope = app.Services.CreateAsyncScope();
        DefaultHttpContext clone = CloneConnectionContext(original, connectionScope.ServiceProvider);
        ClaimsIdentity connectionIdentity = original.User.Identities.Single().Clone();
        using WindowsIdentity? clonedWindowsIdentity = connectionIdentity as WindowsIdentity;
        ClaimsPrincipal connectionPrincipal = new(connectionIdentity);
        Assert.NotSame(original.User, connectionPrincipal);
        Assert.NotSame(original.User.Identity, connectionPrincipal.Identity);
        clone.User = connectionPrincipal;
        Assert.Null(clone.Features.Get<ITlsConnectionFeature>());
        Assert.Null(clone.Features.Get<IAuthenticateResultFeature>());
        Assert.False((await referenceEvaluator.AuthenticateAsync(policy, clone)).Succeeded);
        using InletHub hub = CreateHub(clone, connectionPrincipal, TlsScheme, out IInletSubscriptionGrain grain);
        await hub.OnConnectedAsync();
        ClaimsIdentity replacementIdentity = connectionIdentity.Clone();
        using WindowsIdentity? replacementWindowsIdentity = replacementIdentity as WindowsIdentity;
        if (replacesAfterConnection)
        {
            ClaimsPrincipal replacement = new(replacementIdentity);
            clone.User = replacement;
            hub.Context.User.Returns(replacement);
            await hub.OnConnectedAsync();
        }

        if (OperatingSystem.IsWindows() && !replacesAfterConnection)
        {
            Assert.IsType<WindowsIdentity>(connectionIdentity);
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
    }

    /// <summary>
    ///     A retained scheme snapshot must authorize only the principal it authenticated.
    /// </summary>
    /// <param name="retainsOriginalPrincipal">Whether SignalR retains the originally authenticated principal.</param>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SubscribeBindsSnapshotToRetainedPrincipal(
        bool retainsOriginalPrincipal
    )
    {
        await using WebApplication app = CreateHost();
        DefaultHttpContext original = CreateOriginalContext(app.Services);
        PolicyEvaluator referenceEvaluator = new(app.Services.GetRequiredService<IAuthorizationService>());
        AuthorizationPolicy policy = CreatePolicy(TlsScheme);
        AuthenticateResult established = await referenceEvaluator.AuthenticateAsync(policy, original);
        Assert.True(established.Succeeded);
        Assert.True((await referenceEvaluator.AuthorizeAsync(policy, established, original, null)).Succeeded);
        IAuthenticateResultFeature resultFeature = Substitute.For<IAuthenticateResultFeature>();
        resultFeature.AuthenticateResult.Returns(established);
        original.Features.Set(resultFeature);
        await DispatchHubEndpointAsync(app, original, policy);
        await using AsyncServiceScope connectionScope = app.Services.CreateAsyncScope();
        DefaultHttpContext clone = CloneConnectionContext(original, connectionScope.ServiceProvider);
        ClaimsPrincipal repollPrincipal = new(
            new ClaimsIdentity(
                [new(ClaimTypes.NameIdentifier, "repoll-user"), new("permission", "read")],
                OtherScheme));
        clone.User = repollPrincipal;
        Assert.True(clone.User.Identity?.IsAuthenticated);
        Assert.True(
            (await app.Services.GetRequiredService<IAuthorizationService>()
                .AuthorizeAsync(clone.User, null, policy.Requirements)).Succeeded);
        Assert.Null(clone.Features.Get<ITlsConnectionFeature>());
        Assert.Null(clone.Features.Get<IAuthenticateResultFeature>());
        Assert.Equal(original.Items.Count, clone.Items.Count);
        ClaimsPrincipal connectionPrincipal = retainsOriginalPrincipal ? original.User : repollPrincipal;
        using InletHub hub = CreateHub(clone, connectionPrincipal, TlsScheme, out IInletSubscriptionGrain grain);
        if (retainsOriginalPrincipal)
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
    }

    /// <summary>
    ///     Connection authentication must not bypass provenance, permission or custom evaluator checks.
    /// </summary>
    /// <param name="failure">The prerequisite that prevents a subscription.</param>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Theory]
    [InlineData("different-scheme")]
    [InlineData("no-result")]
    [InlineData("unrelated-principal")]
    [InlineData("no-feature")]
    [InlineData("missing-permission")]
    [InlineData("custom-veto")]
    [InlineData("ambiguous-composite")]
    [InlineData("ambiguous-selection")]
    [InlineData("removed-ambiguous-scheme")]
    [InlineData("non-cacheable-policy")]
    public async Task SubscribeDeniesUnprovenOrUnauthorizedConnection(
        string failure
    )
    {
        ArgumentNullException.ThrowIfNull(failure);
        IPolicyEvaluator? customEvaluator = null;
        if (failure == "custom-veto")
        {
            customEvaluator = Substitute.For<IPolicyEvaluator>();
            customEvaluator.AuthenticateAsync(Arg.Any<AuthorizationPolicy>(), Arg.Any<HttpContext>())
                .Returns(AuthenticateResult.NoResult());
        }

        await using WebApplication app = CreateHost(
            failure == "missing-permission" ? "write" : "read",
            customEvaluator,
            failure.StartsWith("ambiguous", StringComparison.Ordinal) || (failure == "removed-ambiguous-scheme"),
            failure == "non-cacheable-policy");
        DefaultHttpContext original = CreateOriginalContext(app.Services);
        PolicyEvaluator referenceEvaluator = new(app.Services.GetRequiredService<IAuthorizationService>());
        string establishedSchemes = failure switch
        {
            "ambiguous-composite" => TlsScheme + ";" + OtherScheme,
            "removed-ambiguous-scheme" => TlsScheme + ";" + OtherScheme,
            "ambiguous-selection" => TlsScheme + ", " + OtherScheme,
            "non-cacheable-policy" => TlsScheme + ", " + OtherScheme,
            var _ => TlsScheme,
        };
        string selectedSchemes = failure switch
        {
            "ambiguous-composite" => TlsScheme + ", " + OtherScheme,
            "removed-ambiguous-scheme" => TlsScheme + ", " + OtherScheme,
            "ambiguous-selection" => TlsScheme + ";" + OtherScheme,
            "non-cacheable-policy" => TlsScheme + ", " + OtherScheme,
            var _ => TlsScheme,
        };
        AuthorizationPolicy establishedPolicy = CreatePolicy(establishedSchemes);
        AuthenticateResult established = await referenceEvaluator.AuthenticateAsync(establishedPolicy, original);
        Assert.True(established.Succeeded);
        if (failure != "no-feature")
        {
            IAuthenticateResultFeature resultFeature = Substitute.For<IAuthenticateResultFeature>();
            resultFeature.AuthenticateResult.Returns(
                failure switch
                {
                    "different-scheme" => AuthenticateResult.Success(new(original.User, OtherScheme)),
                    "no-result" => AuthenticateResult.NoResult(),
                    "unrelated-principal" => AuthenticateResult.Success(new(CreatePrincipal("read"), TlsScheme)),
                    var _ => established,
                });
            original.Features.Set(resultFeature);
        }

        await DispatchHubEndpointAsync(app, original, establishedPolicy);
        if (failure == "removed-ambiguous-scheme")
        {
            IAuthenticationSchemeProvider provider = app.Services.GetRequiredService<IAuthenticationSchemeProvider>();
            provider.RemoveScheme(TlsScheme + ";" + OtherScheme);
            Assert.Null(await provider.GetSchemeAsync(TlsScheme + ";" + OtherScheme));
        }

        await using AsyncServiceScope connectionScope = app.Services.CreateAsyncScope();
        DefaultHttpContext clone = CloneConnectionContext(original, connectionScope.ServiceProvider);
        Assert.Equal(TlsScheme, original.User.Identity?.AuthenticationType);
        using InletHub hub = CreateHub(clone, original.User, selectedSchemes, out IInletSubscriptionGrain grain);
        HubException exception =
            await Assert.ThrowsAsync<HubException>(() => hub.SubscribeAsync(ProjectionPath, EntityId));
        Assert.Equal(InletHubConstants.SubscriptionDeniedMessage, exception.Message);
        await grain.DidNotReceive().SubscribeAsync(Arg.Any<string>(), Arg.Any<string>());
        if (customEvaluator is not null)
        {
            await customEvaluator.Received(1).AuthenticateAsync(Arg.Any<AuthorizationPolicy>(), clone);
        }
    }

    /// <summary>
    ///     A default forwarding scheme must retain its requested name across a reduced connection context.
    /// </summary>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Fact]
    public async Task SubscribePreservesDefaultForwardingAlias()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSignalR();
        builder.Services.AddAuthentication(PolicyAlias)
            .AddPolicyScheme(PolicyAlias, null, options => options.ForwardDefault = TlsScheme)
            .AddScheme<AuthenticationSchemeOptions, FeatureDependentHandler>(TlsScheme, _ => { });
        builder.Services.AddAuthorizationBuilder().SetDefaultPolicy(CreatePolicy(PolicyAlias));
        await using WebApplication app = builder.Build();
        DefaultHttpContext original = CreateOriginalContext(app.Services);
        AuthenticationMiddleware middleware = new(
            _ => Task.CompletedTask,
            app.Services.GetRequiredService<IAuthenticationSchemeProvider>());
        await middleware.Invoke(original);
        AuthenticateResult established = Assert.IsType<AuthenticateResult>(
            original.Features.Get<IAuthenticateResultFeature>()?.AuthenticateResult);
        Assert.True(established.Succeeded);
        Assert.Equal(TlsScheme, established.Ticket?.AuthenticationScheme);
        Assert.Equal(PolicyAlias, original.User.Identity?.AuthenticationType);
        AuthorizationPolicy policy = CreatePolicy(PolicyAlias);
        Assert.True(
            (await app.Services.GetRequiredService<IAuthorizationService>()
                .AuthorizeAsync(original.User, null, policy.Requirements)).Succeeded);
        await DispatchHubEndpointAsync(app, original, policy, false);
        await using AsyncServiceScope connectionScope = app.Services.CreateAsyncScope();
        DefaultHttpContext clone = CloneConnectionContext(original, connectionScope.ServiceProvider);
        Assert.Null(clone.Features.Get<ITlsConnectionFeature>());
        Assert.Null(clone.Features.Get<IAuthenticateResultFeature>());
        PolicyEvaluator evaluator = new(app.Services.GetRequiredService<IAuthorizationService>());
        Assert.False((await evaluator.AuthenticateAsync(policy, clone)).Succeeded);
        using InletHub hub = CreateHub(clone, original.User, PolicyAlias, out IInletSubscriptionGrain grain);
        await hub.OnConnectedAsync();
        Assert.Equal("subscription-1", await hub.SubscribeAsync(ProjectionPath, EntityId));
        await grain.Received(1).SubscribeAsync(ProjectionPath, EntityId);
    }

    /// <summary>
    ///     An established scheme result should survive the loss of request authentication features.
    /// </summary>
    /// <param name="schemes">The schemes selected by both the connection and subscription policies.</param>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Theory]
    [InlineData(TlsScheme)]
    [InlineData(OtherScheme + ", " + TlsScheme)]
    [InlineData(TlsScheme + ";" + OtherScheme)]
    public async Task SubscribePreservesEstablishedFeatureDependentAuthentication(
        string schemes
    )
    {
        ArgumentNullException.ThrowIfNull(schemes);
        await using WebApplication
            app = CreateHost(hasAmbiguousScheme: schemes.Contains(';', StringComparison.Ordinal));
        DefaultHttpContext original = CreateOriginalContext(app.Services);
        PolicyEvaluator referenceEvaluator = new(app.Services.GetRequiredService<IAuthorizationService>());
        AuthorizationPolicy policy = CreatePolicy(schemes);
        AuthenticateResult established = await referenceEvaluator.AuthenticateAsync(policy, original);
        Assert.True(established.Succeeded);
        Assert.True((await referenceEvaluator.AuthorizeAsync(policy, established, original, null)).Succeeded);
        IAuthenticateResultFeature resultFeature = Substitute.For<IAuthenticateResultFeature>();
        resultFeature.AuthenticateResult.Returns(established);
        original.Features.Set(resultFeature);
        await DispatchHubEndpointAsync(app, original, policy);
        await using AsyncServiceScope connectionScope = app.Services.CreateAsyncScope();
        DefaultHttpContext clone = CloneConnectionContext(original, connectionScope.ServiceProvider);
        Assert.Null(clone.Features.Get<ITlsConnectionFeature>());
        Assert.Null(clone.Features.Get<IAuthenticateResultFeature>());
        Assert.False((await referenceEvaluator.AuthenticateAsync(policy, clone)).Succeeded);
        using InletHub hub = CreateHub(clone, original.User, schemes, out IInletSubscriptionGrain grain);
        Assert.Equal("subscription-1", await hub.SubscribeAsync(ProjectionPath, EntityId));
        await grain.Received(1).SubscribeAsync(ProjectionPath, EntityId);
    }
}