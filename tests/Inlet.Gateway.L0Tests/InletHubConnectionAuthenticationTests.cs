using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Inlet.Gateway.Abstractions;
using Mississippi.Inlet.Gateway.Authentication;
using Mississippi.Inlet.Runtime.Abstractions;
using Mississippi.Inlet.Runtime.Grains;

using MississippiTests.Inlet.Gateway.L0Tests.Authentication;

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
        clone.SetEndpoint(original.GetEndpoint());
        return clone;
    }

    private static DefaultHttpContext CreateBearerContext(
        IServiceProvider services,
        string permission,
        string scheme = "Bearer"
    )
    {
        BearerTokenOptions options = services.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>().Get(scheme);
        FakeTimeProvider clock = Assert.IsType<FakeTimeProvider>(options.TimeProvider);
        AuthenticationTicket ticket = new(
            CreatePrincipal(permission),
            new()
            {
                ExpiresUtc = clock.GetUtcNow().AddHours(1),
            },
            scheme + ":AccessToken");
        DefaultHttpContext context = new()
        {
            RequestServices = services,
        };
        context.Request.Headers.Authorization = "Bearer " + options.BearerTokenProtector.Protect(ticket);
        return context;
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

    private static PermissionHandler CreatePermissionHandler(
        IServiceProvider services
    ) =>
        new(
            services.GetRequiredService<IOptionsMonitor<AuthenticationSchemeOptions>>(),
            services.GetRequiredService<ILoggerFactory>(),
            services.GetRequiredService<UrlEncoder>(),
            services.GetRequiredService<HandlerLifetime>());

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

    private sealed class HandlerLifetime
    {
        public int Disposals { get; set; }
    }

    private sealed class PermissionHandler
        : AuthenticationHandler<AuthenticationSchemeOptions>,
          IAsyncDisposable
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PermissionHandler" /> class.
        /// </summary>
        /// <param name="options">The scheme options.</param>
        /// <param name="logger">The handler logger factory.</param>
        /// <param name="encoder">The URL encoder.</param>
        /// <param name="lifetime">Tracks disposal after the decision.</param>
        public PermissionHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            HandlerLifetime lifetime
        )
            : base(options, logger, encoder) =>
            Lifetime = lifetime;

        private HandlerLifetime Lifetime { get; }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            Lifetime.Disposals++;
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(
                AuthenticateResult.Success(
                    new(CreatePrincipal(Scheme.Name == TlsScheme ? "read" : "write"), Scheme.Name)));
    }

    private sealed class ScopedTenantEvaluator : IPolicyEvaluator
    {
        public ScopedTenantEvaluator(
            IAuthorizationService authorization,
            TenantScope tenant
        )
        {
            Evaluator = new(authorization);
            Tenant = tenant;
        }

        public string? AuthenticatedTenant { get; private set; }

        private PolicyEvaluator Evaluator { get; }

        private TenantScope Tenant { get; }

        public async Task<AuthenticateResult> AuthenticateAsync(
            AuthorizationPolicy policy,
            HttpContext context
        )
        {
            AuthenticateResult result = await Evaluator.AuthenticateAsync(policy, context);
            AuthenticatedTenant = result.Principal?.FindFirst("tenant-scope")?.Value;
            return result;
        }

        public Task<PolicyAuthorizationResult> AuthorizeAsync(
            AuthorizationPolicy policy,
            AuthenticateResult authenticationResult,
            HttpContext context,
            object? resource
        ) =>
            Task.FromResult(
                ReferenceEquals(Tenant, context.RequestServices.GetRequiredService<TenantScope>()) &&
                ReferenceEquals(Tenant, context.RequestServices.GetRequiredKeyedService<TenantScope>("tenant")) &&
                ReferenceEquals(Tenant, context.RequestServices.GetKeyedService<TenantScope>("tenant")) &&
                (AuthenticatedTenant == Tenant.Id)
                    ? PolicyAuthorizationResult.Success()
                    : PolicyAuthorizationResult.Forbid());
    }

    private sealed class ScopedTenantHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public ScopedTenantHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            TenantScope tenant
        )
            : base(options, logger, encoder) =>
            Tenant = tenant;

        private TenantScope Tenant { get; }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            ClaimsPrincipal principal = CreatePrincipal("read");
            ((ClaimsIdentity)principal.Identity!).AddClaim(new("tenant-scope", Tenant.Id));
            return Task.FromResult(AuthenticateResult.Success(new(principal, Scheme.Name)));
        }
    }

    private sealed class ServicesWithoutScopeFactory : IServiceProvider
    {
        public ServicesWithoutScopeFactory(
            IServiceProvider services
        ) =>
            Services = services;

        private IServiceProvider Services { get; }

        public object? GetService(
            Type serviceType
        ) =>
            serviceType == typeof(IServiceScopeFactory) ? null : Services.GetService(serviceType);
    }

    private sealed class TenantScope
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
    }

    /// <summary>
    ///     Refreshing framework caches does not require creating another service scope.
    /// </summary>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Fact]
    public async Task SubscribeAuthenticatesWithoutCreatingAnotherFrameworkScope()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSignalR();
        builder.Services.AddSingleton<HandlerLifetime>();
        builder.Services.AddTransient(CreatePermissionHandler);
        builder.Services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, PermissionHandler>(TlsScheme, _ => { });
        builder.Services.AddAuthorizationBuilder().SetDefaultPolicy(CreatePolicy(TlsScheme));
        await using WebApplication app = builder.Build();
        await using AsyncServiceScope connectionScope = app.Services.CreateAsyncScope();
        DefaultHttpContext retained =
            CreateOriginalContext(new ServicesWithoutScopeFactory(connectionScope.ServiceProvider));
        retained.User = CreatePrincipal("read");
        using InletHub hub = CreateHub(retained, retained.User, TlsScheme, out IInletSubscriptionGrain grain);
        Assert.Equal("subscription-1", await hub.SubscribeAsync(ProjectionPath, EntityId));
        await grain.Received(1).SubscribeAsync(ProjectionPath, EntityId);
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
            await customEvaluator.Received(1)
                .AuthenticateAsync(
                    Arg.Any<AuthorizationPolicy>(),
                    Arg.Is<HttpContext>(context =>
                        !ReferenceEquals(context, clone) &&
                        ReferenceEquals(context.RequestServices, clone.RequestServices) &&
                        (context.Request.Path == clone.Request.Path) &&
                        ReferenceEquals(context.GetEndpoint(), clone.GetEndpoint())));
        }
    }

    /// <summary>
    ///     A retained request must not authenticate old credentials for a new uncached caller principal.
    /// </summary>
    /// <param name="cachedOriginalCaller">Whether SignalR cached the first principal before the repoll.</param>
    /// <param name="authenticationMode">The host authentication extension point being exercised.</param>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Theory]
    [InlineData(true, "framework")]
    [InlineData(false, "framework")]
    [InlineData(true, "evaluator")]
    [InlineData(false, "evaluator")]
    [InlineData(true, "service")]
    [InlineData(false, "service")]
    [InlineData(true, "handler-provider")]
    [InlineData(false, "handler-provider")]
    [InlineData(true, "uncached-semicolon-policy")]
    [InlineData(false, "uncached-semicolon-policy")]
    public async Task SubscribeDoesNotGrantOldBearerCredentialsToRepollCaller(
        bool cachedOriginalCaller,
        string authenticationMode
    )
    {
        string scheme = authenticationMode == "uncached-semicolon-policy" ? "Bearer;Alias" : "Bearer";
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSignalR();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        FakeTimeProvider clock = new(new(2040, 1, 1, 0, 0, 0, TimeSpan.Zero));
        builder.Services.AddAuthentication(scheme).AddBearerToken(scheme, options => options.TimeProvider = clock);
        if (authenticationMode == "evaluator")
        {
            builder.Services.AddScoped<IPolicyEvaluator>(services =>
            {
                PolicyEvaluator framework = new(services.GetRequiredService<IAuthorizationService>());
                IPolicyEvaluator custom = Substitute.For<IPolicyEvaluator>();
                custom.AuthenticateAsync(Arg.Any<AuthorizationPolicy>(), Arg.Any<HttpContext>())
                    .Returns(call => framework.AuthenticateAsync(
                        call.Arg<AuthorizationPolicy>(),
                        call.Arg<HttpContext>()));
                custom.AuthorizeAsync(
                        Arg.Any<AuthorizationPolicy>(),
                        Arg.Any<AuthenticateResult>(),
                        Arg.Any<HttpContext>(),
                        Arg.Any<object?>())
                    .Returns(call => framework.AuthorizeAsync(
                        call.Arg<AuthorizationPolicy>(),
                        call.Arg<AuthenticateResult>(),
                        call.Arg<HttpContext>(),
                        call.ArgAt<object?>(3)));
                return custom;
            });
        }
        else if (authenticationMode == "service")
        {
            builder.Services.AddScoped<IAuthenticationService>(services =>
            {
                AuthenticationService framework = new(
                    services.GetRequiredService<IAuthenticationSchemeProvider>(),
                    services.GetRequiredService<IAuthenticationHandlerProvider>(),
                    services.GetRequiredService<IClaimsTransformation>(),
                    services.GetRequiredService<IOptions<AuthenticationOptions>>());
                IAuthenticationService custom = Substitute.For<IAuthenticationService>();
                custom.AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string>())
                    .Returns(call => framework.AuthenticateAsync(call.Arg<HttpContext>(), call.Arg<string>()));
                return custom;
            });
        }
        else if (authenticationMode == "handler-provider")
        {
            builder.Services.AddScoped<IAuthenticationHandlerProvider>(services =>
            {
                AuthenticationHandlerProvider framework = new(
                    services.GetRequiredService<IAuthenticationSchemeProvider>());
                IAuthenticationHandlerProvider custom = Substitute.For<IAuthenticationHandlerProvider>();
                custom.GetHandlerAsync(Arg.Any<HttpContext>(), Arg.Any<string>())
                    .Returns(call => framework.GetHandlerAsync(call.Arg<HttpContext>(), call.Arg<string>()));
                return custom;
            });
        }

        builder.Services.AddAuthorizationBuilder().SetDefaultPolicy(CreatePolicy(scheme));
        if (authenticationMode == "uncached-semicolon-policy")
        {
            IAuthorizationPolicyProvider provider = Substitute.For<IAuthorizationPolicyProvider>();
            provider.AllowsCachingPolicies.Returns(false);
            provider.GetDefaultPolicyAsync().Returns(CreatePolicy(scheme));
            builder.Services.AddSingleton(provider);
        }

        await using WebApplication app = builder.Build();
        await using AsyncServiceScope firstScope = app.Services.CreateAsyncScope();
        DefaultHttpContext first = CreateBearerContext(firstScope.ServiceProvider, "read", scheme);
        AuthenticationMiddleware middleware = new(
            _ => Task.CompletedTask,
            app.Services.GetRequiredService<IAuthenticationSchemeProvider>());
        await middleware.Invoke(first);
        ClaimsPrincipal original = first.User;
        Assert.True(original.Identity?.IsAuthenticated);
        AuthorizationPolicy connectionPolicy =
            new AuthorizationPolicyBuilder(scheme).RequireAuthenticatedUser().Build();
        await DispatchHubEndpointAsync(app, first, connectionPolicy);
        await using AsyncServiceScope pollScope = app.Services.CreateAsyncScope();
        DefaultHttpContext poll = CreateBearerContext(pollScope.ServiceProvider, "write", scheme);
        await middleware.Invoke(poll);
        Assert.True(
            (await app.Services.GetRequiredService<IAuthorizationService>()
                .AuthorizeAsync(poll.User, null, connectionPolicy.Requirements)).Succeeded);
        Assert.False(
            (await app.Services.GetRequiredService<IAuthorizationService>()
                .AuthorizeAsync(poll.User, null, CreatePolicy(scheme).Requirements)).Succeeded);
        await using AsyncServiceScope connectionScope = app.Services.CreateAsyncScope();
        DefaultHttpContext retained = CloneConnectionContext(first, connectionScope.ServiceProvider);
        retained.User = poll.User;
        Assert.Equal(first.Request.Headers.Authorization, retained.Request.Headers.Authorization);
        Assert.NotEqual(poll.Request.Headers.Authorization, retained.Request.Headers.Authorization);
        ClaimsPrincipal caller = cachedOriginalCaller ? original : poll.User;
        using InletHub hub = CreateHub(retained, caller, scheme, out IInletSubscriptionGrain grain);
        if (cachedOriginalCaller)
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

        Assert.Same(poll.User, retained.User);
        Assert.Same(caller, hub.Context.User);
    }

    /// <summary>
    ///     An anonymous first request must not lend its bearer credentials to a later cookie caller.
    /// </summary>
    /// <param name="cachedOriginalCaller">Whether SignalR retained the initial anonymous principal.</param>
    /// <returns>A task that completes when the subscription assertions have been verified.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SubscribeDoesNotGrantUnboundBearerCredentialsToRepollCaller(
        bool cachedOriginalCaller
    )
    {
        const string bearerScheme = "Bearer";
        const string cookieScheme = "Cookies";
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSignalR();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        FakeTimeProvider clock = new(new(2040, 1, 1, 0, 0, 0, TimeSpan.Zero));
        builder.Services.AddAuthentication(cookieScheme)
            .AddCookie(cookieScheme, options => options.TimeProvider = clock)
            .AddBearerToken(bearerScheme, options => options.TimeProvider = clock);
        builder.Services.AddAuthorizationBuilder().SetDefaultPolicy(CreatePolicy(bearerScheme));
        await using WebApplication app = builder.Build();
        await using AsyncServiceScope firstScope = app.Services.CreateAsyncScope();
        DefaultHttpContext first = CreateBearerContext(firstScope.ServiceProvider, "read");
        AuthenticationMiddleware middleware = new(
            _ => Task.CompletedTask,
            app.Services.GetRequiredService<IAuthenticationSchemeProvider>());
        await middleware.Invoke(first);
        ClaimsPrincipal original = first.User;
        Assert.False(original.Identity?.IsAuthenticated);
        AuthenticateResult bearer = await first.AuthenticateAsync(bearerScheme);
        Assert.True(bearer.Succeeded);
        Assert.True(bearer.Principal?.HasClaim("permission", "read"));
        Assert.False(first.Features.Get<IAuthenticateResultFeature>()?.AuthenticateResult?.Succeeded ?? false);
        await DispatchHubEndpointAsync(app, first, CreatePolicy(bearerScheme), false);
        await using AsyncServiceScope pollScope = app.Services.CreateAsyncScope();
        CookieAuthenticationOptions cookie = app.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(cookieScheme);
        ISecureDataFormat<AuthenticationTicket> cookieFormat =
            Assert.IsType<ISecureDataFormat<AuthenticationTicket>>(cookie.TicketDataFormat, false);
        Assert.NotNull(cookie.Cookie.Name);
        ClaimsPrincipal cookiePrincipal = new(
            new ClaimsIdentity(
                [new(ClaimTypes.NameIdentifier, "poll-cookie-user"), new("permission", "write")],
                cookieScheme));
        AuthenticationTicket cookieTicket = new(
            cookiePrincipal,
            new()
            {
                ExpiresUtc = clock.GetUtcNow().AddHours(1),
            },
            cookieScheme);
        DefaultHttpContext poll = new()
        {
            RequestServices = pollScope.ServiceProvider,
        };
        poll.Request.Headers.Cookie = cookie.Cookie.Name + "=" + cookieFormat.Protect(cookieTicket);
        await middleware.Invoke(poll);
        Assert.True(poll.User.Identity?.IsAuthenticated);
        Assert.True(poll.User.HasClaim("permission", "write"));
        Assert.False(poll.User.HasClaim("permission", "read"));
        await using AsyncServiceScope connectionScope = app.Services.CreateAsyncScope();
        DefaultHttpContext retained = CloneConnectionContext(first, connectionScope.ServiceProvider);
        retained.User = poll.User;
        Assert.Equal(first.Request.Headers.Authorization, retained.Request.Headers.Authorization);
        ClaimsPrincipal caller = cachedOriginalCaller ? original : poll.User;
        using InletHub hub = CreateHub(retained, caller, bearerScheme, out IInletSubscriptionGrain grain);
        if (cachedOriginalCaller)
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

        Assert.Same(poll.User, retained.User);
        Assert.Same(caller, hub.Context.User);
    }

    /// <summary>
    ///     A default forwarding scheme must retain its requested name across a reduced connection context.
    /// </summary>
    /// <param name="usesFallbackPolicy">Whether the endpoint uses a scheme-selecting fallback policy.</param>
    /// <param name="fallbackMatchesAlias">Whether that policy selects the default forwarding alias.</param>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public async Task SubscribePreservesDefaultForwardingAlias(
        bool usesFallbackPolicy,
        bool fallbackMatchesAlias
    )
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSignalR();
        builder.Services.AddAuthentication(PolicyAlias)
            .AddPolicyScheme(PolicyAlias, null, options => options.ForwardDefault = TlsScheme)
            .AddPolicyScheme(OtherScheme, null, options => options.ForwardDefault = TlsScheme)
            .AddScheme<AuthenticationSchemeOptions, FeatureDependentHandler>(TlsScheme, _ => { });
        builder.Services.AddAuthorizationBuilder().SetDefaultPolicy(CreatePolicy(PolicyAlias));
        if (usesFallbackPolicy)
        {
            builder.Services.AddAuthorizationBuilder()
                .SetFallbackPolicy(CreatePolicy(fallbackMatchesAlias ? PolicyAlias : OtherScheme));
        }

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
        if (usesFallbackPolicy)
        {
            AuthorizationMiddleware authorization = new(
                _ => Task.CompletedTask,
                app.Services.GetRequiredService<IAuthorizationPolicyProvider>());
            await authorization.Invoke(original);
            AuthenticateResult fallbackAuthentication = Assert.IsType<AuthenticateResult>(
                original.Features.Get<IAuthenticateResultFeature>()?.AuthenticateResult);
            Assert.True(fallbackAuthentication.Succeeded);
            Assert.Equal(
                fallbackMatchesAlias ? PolicyAlias : OtherScheme,
                fallbackAuthentication.Ticket?.AuthenticationScheme);
            Assert.Same(fallbackAuthentication.Principal, original.User);
        }

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
        if (usesFallbackPolicy && !fallbackMatchesAlias)
        {
            HubException exception =
                await Assert.ThrowsAsync<HubException>(() => hub.SubscribeAsync(ProjectionPath, EntityId));
            Assert.Equal(InletHubConstants.SubscriptionDeniedMessage, exception.Message);
            await grain.DidNotReceive().SubscribeAsync(Arg.Any<string>(), Arg.Any<string>());
        }
        else
        {
            Assert.Equal("subscription-1", await hub.SubscribeAsync(ProjectionPath, EntityId));
            await grain.Received(1).SubscribeAsync(ProjectionPath, EntityId);
        }
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

    /// <summary>
    ///     Handler dependencies, the custom evaluator and its context must share the host tenant scope.
    /// </summary>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Fact]
    public async Task SubscribePreservesScopedTenantAcrossAuthenticationAndAuthorization()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSignalR();
        builder.Services.AddScoped(_ => new TenantScope());
        builder.Services.AddKeyedScoped<TenantScope>(
            "tenant",
            (services, _) => services.GetRequiredService<TenantScope>());
        builder.Services.AddTransient<ScopedTenantHandler>(services => new(
            services.GetRequiredService<IOptionsMonitor<AuthenticationSchemeOptions>>(),
            services.GetRequiredService<ILoggerFactory>(),
            services.GetRequiredService<UrlEncoder>(),
            services.GetRequiredService<TenantScope>()));
        builder.Services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, ScopedTenantHandler>(TlsScheme, _ => { });
        builder.Services.AddAuthorizationBuilder().SetDefaultPolicy(CreatePolicy(TlsScheme));
        builder.Services.AddScoped<IPolicyEvaluator>(services => new ScopedTenantEvaluator(
            services.GetRequiredService<IAuthorizationService>(),
            services.GetRequiredService<TenantScope>()));
        await using WebApplication app = builder.Build();
        await using AsyncServiceScope connectionScope = app.Services.CreateAsyncScope();
        TenantScope tenant = connectionScope.ServiceProvider.GetRequiredService<TenantScope>();
        ScopedTenantEvaluator evaluator = Assert.IsType<ScopedTenantEvaluator>(
            connectionScope.ServiceProvider.GetRequiredService<IPolicyEvaluator>());
        DefaultHttpContext retained = CreateOriginalContext(connectionScope.ServiceProvider);
        retained.User = CreatePrincipal("read");
        using InletHub hub = CreateHub(retained, retained.User, TlsScheme, out IInletSubscriptionGrain grain);
        Assert.Equal("subscription-1", await hub.SubscribeAsync(ProjectionPath, EntityId));
        await grain.Received(1).SubscribeAsync(ProjectionPath, EntityId);
        Assert.Equal(tenant.Id, evaluator.AuthenticatedTenant);
        Assert.Same(connectionScope.ServiceProvider, retained.RequestServices);
    }

    /// <summary>
    ///     Wrapped and derived evaluators retain verified TLS authentication while preserving their vetoes.
    /// </summary>
    /// <param name="useSubclass">Whether the host derives from the framework evaluator.</param>
    /// <param name="authenticateAllowed">Whether the evaluator accepts subscription authentication.</param>
    /// <param name="authorizeAllowed">Whether the evaluator accepts subscription authorization.</param>
    /// <returns>A task that completes when reduced-context and veto assertions have been verified.</returns>
    [Theory]
    [InlineData(false, true, true)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(true, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public async Task SubscribePreservesVerifiedTlsAuthenticationWithCustomEvaluator(
        bool useSubclass,
        bool authenticateAllowed,
        bool authorizeAllowed
    )
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSignalR();
        builder.Services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, FeatureDependentHandler>(TlsScheme, _ => { });
        AuthorizationPolicy policy = CreatePolicy(TlsScheme);
        builder.Services.AddAuthorizationBuilder().SetDefaultPolicy(policy);
        ConnectionEvaluatorControl control = new();
        builder.Services.AddScoped<IPolicyEvaluator>(services => useSubclass
            ? new DerivedConnectionPolicyEvaluator(services.GetRequiredService<IAuthorizationService>(), control)
            : new WrappedConnectionPolicyEvaluator(services.GetRequiredService<IAuthorizationService>(), control));
        await using WebApplication app = builder.Build();
        await using AsyncServiceScope requestScope = app.Services.CreateAsyncScope();
        DefaultHttpContext original = CreateOriginalContext(requestScope.ServiceProvider);
        IPolicyEvaluator originalEvaluator = requestScope.ServiceProvider.GetRequiredService<IPolicyEvaluator>();
        AuthenticateResult established = await originalEvaluator.AuthenticateAsync(policy, original);
        Assert.True(established.Succeeded);
        Assert.Equal(PolicyAlias, original.User.Identity?.AuthenticationType);
        Assert.True((await originalEvaluator.AuthorizeAsync(policy, established, original, original)).Succeeded);
        IAuthenticateResultFeature feature = Substitute.For<IAuthenticateResultFeature>();
        feature.AuthenticateResult.Returns(established);
        original.Features.Set(feature);
        await DispatchHubEndpointAsync(app, original, policy);
        ClaimsPrincipal caller = original.User;
        await using AsyncServiceScope connectionScope = app.Services.CreateAsyncScope();
        DefaultHttpContext retained = CloneConnectionContext(original, connectionScope.ServiceProvider);
        Assert.Null(retained.Features.Get<ITlsConnectionFeature>());
        PolicyEvaluator reference = new(app.Services.GetRequiredService<IAuthorizationService>());
        Assert.False((await reference.AuthenticateAsync(policy, retained)).Succeeded);
        retained.User = caller;
        control.CanAuthenticate = authenticateAllowed;
        control.CanAuthorize = authorizeAllowed;
        using InletHub hub = CreateHub(retained, caller, TlsScheme, out IInletSubscriptionGrain grain);
        if (authenticateAllowed && authorizeAllowed)
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

        Assert.Equal(2, control.Authentications);
        Assert.Equal(authenticateAllowed ? 2 : 1, control.Authorizations);
        Assert.Same(caller, hub.Context.User);
        Assert.Same(caller, retained.User);
    }

    /// <summary>
    ///     A forwarding selector must see the retained transport principal for each decision.
    /// </summary>
    /// <param name="replacementAllowed">Whether the new poll selects an authorized handler.</param>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SubscribeReevaluatesForwardingSelectorAfterRepoll(
        bool replacementAllowed
    )
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSignalR();
        HandlerLifetime lifetime = new();
        builder.Services.AddSingleton(lifetime);
        builder.Services.AddTransient(CreatePermissionHandler);
        builder.Services.AddAuthentication()
            .AddPolicyScheme(
                PolicyAlias,
                null,
                options => options.ForwardDefaultSelector = context =>
                    context.User.FindFirst("permission")?.Value == "read" ? TlsScheme : OtherScheme)
            .AddScheme<AuthenticationSchemeOptions, PermissionHandler>(TlsScheme, _ => { })
            .AddScheme<AuthenticationSchemeOptions, PermissionHandler>(OtherScheme, _ => { });
        builder.Services.AddAuthorizationBuilder().SetDefaultPolicy(CreatePolicy(PolicyAlias));
        await using WebApplication app = builder.Build();
        await using AsyncServiceScope connectionScope = app.Services.CreateAsyncScope();
        DefaultHttpContext retained = CreateOriginalContext(connectionScope.ServiceProvider);
        ClaimsPrincipal original = CreatePrincipal(replacementAllowed ? "write" : "read");
        retained.User = original;
        using InletHub hub = CreateHub(retained, original, PolicyAlias, out IInletSubscriptionGrain grain);
        if (replacementAllowed)
        {
            Assert.Equal(
                InletHubConstants.SubscriptionDeniedMessage,
                (await Assert.ThrowsAsync<HubException>(() => hub.SubscribeAsync(ProjectionPath, EntityId))).Message);
        }
        else
        {
            Assert.Equal("subscription-1", await hub.SubscribeAsync(ProjectionPath, EntityId));
        }

        grain.ClearReceivedCalls();
        ClaimsPrincipal replacement = CreatePrincipal(replacementAllowed ? "read" : "write");
        retained.User = replacement;
        if (replacementAllowed)
        {
            Assert.Equal("subscription-1", await hub.SubscribeAsync(ProjectionPath, EntityId));
            await grain.Received(1).SubscribeAsync(ProjectionPath, EntityId);
        }
        else
        {
            Assert.Equal(
                InletHubConstants.SubscriptionDeniedMessage,
                (await Assert.ThrowsAsync<HubException>(() => hub.SubscribeAsync(ProjectionPath, EntityId))).Message);
            await grain.DidNotReceive().SubscribeAsync(Arg.Any<string>(), Arg.Any<string>());
        }

        Assert.Same(replacement, retained.User);
        Assert.Same(original, hub.Context.User);
        Assert.Same(connectionScope.ServiceProvider, retained.RequestServices);
        Assert.Equal(0, lifetime.Disposals);
        await connectionScope.DisposeAsync();
        Assert.Equal(2, lifetime.Disposals);
    }

    /// <summary>
    ///     Scoped handlers must observe changed request permissions for each subscription decision.
    /// </summary>
    /// <param name="replacementAllowed">Whether the second decision has read permission.</param>
    /// <param name="useFactory">Whether handler configuration comes from a custom scoped factory.</param>
    /// <returns>A task that completes when both sequential decisions and the host scope have been verified.</returns>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task SubscribeRefreshesScopedHandlerAfterPermissionChanges(
        bool replacementAllowed,
        bool useFactory
    )
    {
        string? tenantId = null;
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddInletServer();
        builder.Services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, HeaderPermissionAuthenticationHandler>(OtherScheme, _ => { });
        builder.Services.AddScoped<ConnectionHandlerScope>();
        if (useFactory)
        {
            builder.Services.AddScoped<HeaderPermissionAuthenticationHandler>(services => new(
                services.GetRequiredService<IOptionsMonitor<AuthenticationSchemeOptions>>(),
                services.GetRequiredService<ILoggerFactory>(),
                services.GetRequiredService<UrlEncoder>(),
                services.GetRequiredService<ConnectionHandlerScope>(),
                "host-factory"));
        }
        else
        {
            builder.Services.AddScoped<HeaderPermissionAuthenticationHandler>();
        }

        ServiceDescriptor registration = builder.Services.Last(service =>
            service.ServiceType == typeof(HeaderPermissionAuthenticationHandler));
        Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
        builder.Services.AddAuthorizationBuilder()
            .SetDefaultPolicy(
                new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                    .RequireClaim("permission", "read")
                    .RequireAssertion(context => context.User.HasClaim("tenant", tenantId!) &&
                                                 (!useFactory || context.User.HasClaim("factory", "host-factory")))
                    .Build());
        await using WebApplication app = builder.Build();
        await using AsyncServiceScope connectionScope = app.Services.CreateAsyncScope();
        ConnectionHandlerScope state = connectionScope.ServiceProvider.GetRequiredService<ConnectionHandlerScope>();
        tenantId = state.Id;
        HeaderPermissionAuthenticationHandler owned =
            connectionScope.ServiceProvider.GetRequiredService<HeaderPermissionAuthenticationHandler>();
        DefaultHttpContext retained = CreateOriginalContext(connectionScope.ServiceProvider);
        ClaimsPrincipal caller = CreatePrincipal("read");
        retained.User = caller;
        retained.Request.Headers[OtherScheme] = replacementAllowed ? "write" : "read";
        using InletHub hub = CreateHub(retained, caller, OtherScheme, out IInletSubscriptionGrain grain);
        if (replacementAllowed)
        {
            await Assert.ThrowsAsync<HubException>(() => hub.SubscribeAsync(ProjectionPath, EntityId));
        }
        else
        {
            Assert.Equal("subscription-1", await hub.SubscribeAsync(ProjectionPath, EntityId));
        }

        grain.ClearReceivedCalls();
        retained.Request.Headers[OtherScheme] = replacementAllowed ? "read" : "write";
        if (replacementAllowed)
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

        Assert.Same(caller, retained.User);
        Assert.Same(caller, hub.Context.User);
        Assert.Same(connectionScope.ServiceProvider, retained.RequestServices);
        Assert.False(owned.IsDisposed);
        Assert.Equal(2, state.Disposals);
        await connectionScope.DisposeAsync();
        Assert.True(owned.IsDisposed);
        Assert.Equal(3, state.Disposals);
    }

    /// <summary>
    ///     Anonymous repolls use their own credentials only when endpoint and connection token remain bound.
    /// </summary>
    /// <param name="permission">The current poll's bearer permission, or a missing-token marker.</param>
    /// <param name="sameToken">Whether the poll belongs to the original transport token.</param>
    /// <param name="sameEndpoint">Whether the poll uses the original mapped endpoint.</param>
    /// <param name="allowed">Whether the subscription should reach the grain.</param>
    /// <returns>A task that completes when anonymous-poll boundaries have been checked.</returns>
    [Theory]
    [InlineData("read", true, true, true)]
    [InlineData("write", true, true, false)]
    [InlineData("missing", true, true, false)]
    [InlineData("read", false, true, false)]
    [InlineData("read", true, false, false)]
    public async Task SubscribeUsesCurrentAnonymousPollCredentials(
        string permission,
        bool sameToken,
        bool sameEndpoint,
        bool allowed
    )
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSignalR();
        builder.Services.AddDataProtection();
        builder.Services.AddAuthentication()
            .AddBearerToken(
                "Bearer",
                options => options.TimeProvider = new FakeTimeProvider(new(2040, 1, 1, 0, 0, 0, TimeSpan.Zero)));
        builder.Services.AddAuthorizationBuilder().SetDefaultPolicy(CreatePolicy("Bearer"));
        await using WebApplication app = builder.Build();
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        DefaultHttpContext original = CreateBearerContext(scope.ServiceProvider, "read");
        AuthorizationPolicy policy = CreatePolicy("Bearer");
        await DispatchHubEndpointAsync(app, original, policy, false);
        ClaimsPrincipal initial = original.User;
        DefaultHttpContext retained = CloneConnectionContext(original, scope.ServiceProvider);
        DefaultHttpContext poll = CreateBearerContext(scope.ServiceProvider, permission);
        if (permission == "missing")
        {
            poll.Request.Headers.Remove("Authorization");
        }

        poll.Request.QueryString = new(sameToken ? "?id=unavailable-connection" : "?id=another-connection");
        poll.SetEndpoint(
            sameEndpoint ? original.GetEndpoint() : new(_ => Task.CompletedTask, new(), "another endpoint"));
        Assert.NotSame(initial, poll.User);
        Assert.DoesNotContain(poll.User.Identities, identity => identity.IsAuthenticated);
        await ConnectionAuthenticationSnapshot.CaptureAsync(poll);
        ClaimsPrincipal caller = poll.User;
        retained.User = caller;
        using InletHub hub = CreateHub(retained, caller, "Bearer", out IInletSubscriptionGrain grain);
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

        Assert.Same(caller, retained.User);
        Assert.Same(caller, hub.Context.User);
    }

    /// <summary>
    ///     Certificate-only projection policies cannot borrow bearer identities from a composite endpoint result.
    /// </summary>
    /// <param name="certificatePermission">The certificate permission, or no-result marker.</param>
    /// <param name="bearerPermission">The other scheme's permission, or no-result marker.</param>
    /// <param name="allowed">Whether certificate-only authorization should allow the subscription.</param>
    /// <returns>A task that completes when scheme attribution and authorization have been verified.</returns>
    [Theory]
    [InlineData("read", "write", true)]
    [InlineData("write", "read", false)]
    [InlineData("none", "read", false)]
    [InlineData("read", "none", true)]
    public async Task SubscribeUsesForwardingAliasFromCompositeEndpoint(
        string certificatePermission,
        string bearerPermission,
        bool allowed
    )
    {
        List<ClaimsPrincipal> decisions = [];
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddInletServer();
        builder.Services.AddSingleton<HubLifetimeManager<InletHub>, DefaultHubLifetimeManager<InletHub>>();
        builder.Services.AddAuthentication()
            .AddPolicyScheme(PolicyAlias, null, options => options.ForwardAuthenticate = TlsScheme)
            .AddScheme<AuthenticationSchemeOptions, CompositeConnectionAuthenticationHandler>(TlsScheme, _ => { })
            .AddScheme<AuthenticationSchemeOptions, CompositeConnectionAuthenticationHandler>(OtherScheme, _ => { });
        builder.Services.AddAuthorizationBuilder()
            .SetDefaultPolicy(
                new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                    .RequireClaim("permission", "read")
                    .RequireAssertion(context =>
                    {
                        decisions.Add(context.User);
                        return true;
                    })
                    .Build());
        await using WebApplication app = builder.Build();
        await using AsyncServiceScope requestScope = app.Services.CreateAsyncScope();
        DefaultHttpContext original = CreateOriginalContext(requestScope.ServiceProvider);
        original.Request.Headers[TlsScheme] = certificatePermission;
        original.Request.Headers[OtherScheme] = bearerPermission;
        AuthorizationPolicy endpointPolicy =
            new AuthorizationPolicyBuilder(PolicyAlias, OtherScheme).RequireAuthenticatedUser().Build();
        PolicyEvaluator evaluator = new(app.Services.GetRequiredService<IAuthorizationService>());
        AuthenticateResult established = await evaluator.AuthenticateAsync(endpointPolicy, original);
        Assert.True(established.Succeeded);
        Assert.True((await evaluator.AuthorizeAsync(endpointPolicy, established, original, original)).Succeeded);
        IAuthenticateResultFeature feature = Substitute.For<IAuthenticateResultFeature>();
        feature.AuthenticateResult.Returns(established);
        original.Features.Set(feature);
        await DispatchHubEndpointAsync(app, original, endpointPolicy);
        Assert.Equal(1, Assert.IsType<int>(original.Items[TlsScheme + "-calls"]));
        Assert.Equal(1, Assert.IsType<int>(original.Items[OtherScheme + "-calls"]));
        ClaimsPrincipal caller = original.User;
        await using AsyncServiceScope connectionScope = app.Services.CreateAsyncScope();
        DefaultHttpContext retained = CloneConnectionContext(original, connectionScope.ServiceProvider);
        Assert.Null(retained.Features.Get<ITlsConnectionFeature>());
        Assert.False((await evaluator.AuthenticateAsync(CreatePolicy(PolicyAlias), retained)).Succeeded);
        retained.User = caller;
        using InletHub hub = CreateHub(retained, caller, PolicyAlias, out IInletSubscriptionGrain grain);
        if (allowed)
        {
            Assert.Equal("subscription-1", await hub.SubscribeAsync(ProjectionPath, EntityId));
            await grain.Received(1).SubscribeAsync(ProjectionPath, EntityId);
            ClaimsPrincipal selected = Assert.Single(decisions);
            Assert.Equal(TlsScheme, selected.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            Assert.True(selected.HasClaim("permission", "read"));
            Assert.False(selected.HasClaim(ClaimTypes.NameIdentifier, OtherScheme));
            Assert.False(selected.HasClaim("permission", "write"));
        }
        else
        {
            HubException exception =
                await Assert.ThrowsAsync<HubException>(() => hub.SubscribeAsync(ProjectionPath, EntityId));
            Assert.Equal(InletHubConstants.SubscriptionDeniedMessage, exception.Message);
            await grain.DidNotReceive().SubscribeAsync(Arg.Any<string>(), Arg.Any<string>());
        }

        Assert.Same(caller, hub.Context.User);
        Assert.Same(caller, retained.User);
    }

    /// <summary>
    ///     Certificate-only projection policies cannot borrow bearer identities from a composite endpoint result.
    /// </summary>
    /// <param name="certificatePermission">The certificate permission, or no-result marker.</param>
    /// <param name="bearerPermission">The other scheme's permission, or no-result marker.</param>
    /// <param name="allowed">Whether certificate-only authorization should allow the subscription.</param>
    /// <returns>A task that completes when scheme attribution and authorization have been verified.</returns>
    [Theory]
    [InlineData("read", "write", true)]
    [InlineData("write", "read", false)]
    [InlineData("none", "read", false)]
    [InlineData("read", "none", true)]
    public async Task SubscribeUsesOnlySuccessfulSelectedSchemeFromCompositeEndpoint(
        string certificatePermission,
        string bearerPermission,
        bool allowed
    )
    {
        List<ClaimsPrincipal> decisions = [];
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSignalR();
        builder.Services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, CompositeConnectionAuthenticationHandler>(TlsScheme, _ => { })
            .AddScheme<AuthenticationSchemeOptions, CompositeConnectionAuthenticationHandler>(OtherScheme, _ => { });
        builder.Services.AddAuthorizationBuilder()
            .SetDefaultPolicy(
                new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                    .RequireClaim("permission", "read")
                    .RequireAssertion(context =>
                    {
                        decisions.Add(context.User);
                        return true;
                    })
                    .Build());
        await using WebApplication app = builder.Build();
        await using AsyncServiceScope requestScope = app.Services.CreateAsyncScope();
        DefaultHttpContext original = CreateOriginalContext(requestScope.ServiceProvider);
        original.Request.Headers[TlsScheme] = certificatePermission;
        original.Request.Headers[OtherScheme] = bearerPermission;
        AuthorizationPolicy endpointPolicy =
            new AuthorizationPolicyBuilder(TlsScheme, OtherScheme).RequireAuthenticatedUser().Build();
        PolicyEvaluator evaluator = new(app.Services.GetRequiredService<IAuthorizationService>());
        AuthenticateResult established = await evaluator.AuthenticateAsync(endpointPolicy, original);
        Assert.True(established.Succeeded);
        Assert.True((await evaluator.AuthorizeAsync(endpointPolicy, established, original, original)).Succeeded);
        IAuthenticateResultFeature feature = Substitute.For<IAuthenticateResultFeature>();
        feature.AuthenticateResult.Returns(established);
        original.Features.Set(feature);
        await DispatchHubEndpointAsync(app, original, endpointPolicy);
        Assert.Equal(1, Assert.IsType<int>(original.Items[TlsScheme + "-calls"]));
        Assert.Equal(1, Assert.IsType<int>(original.Items[OtherScheme + "-calls"]));
        ClaimsPrincipal caller = original.User;
        await using AsyncServiceScope connectionScope = app.Services.CreateAsyncScope();
        DefaultHttpContext retained = CloneConnectionContext(original, connectionScope.ServiceProvider);
        Assert.Null(retained.Features.Get<ITlsConnectionFeature>());
        Assert.False((await evaluator.AuthenticateAsync(CreatePolicy(TlsScheme), retained)).Succeeded);
        retained.User = caller;
        using InletHub hub = CreateHub(retained, caller, TlsScheme, out IInletSubscriptionGrain grain);
        if (allowed)
        {
            Assert.Equal("subscription-1", await hub.SubscribeAsync(ProjectionPath, EntityId));
            await grain.Received(1).SubscribeAsync(ProjectionPath, EntityId);
            ClaimsPrincipal selected = Assert.Single(decisions);
            Assert.Equal(TlsScheme, selected.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            Assert.True(selected.HasClaim("permission", "read"));
            Assert.False(selected.HasClaim(ClaimTypes.NameIdentifier, OtherScheme));
            Assert.False(selected.HasClaim("permission", "write"));
        }
        else
        {
            HubException exception =
                await Assert.ThrowsAsync<HubException>(() => hub.SubscribeAsync(ProjectionPath, EntityId));
            Assert.Equal(InletHubConstants.SubscriptionDeniedMessage, exception.Message);
            await grain.DidNotReceive().SubscribeAsync(Arg.Any<string>(), Arg.Any<string>());
        }

        Assert.Same(caller, hub.Context.User);
        Assert.Same(caller, retained.User);
    }

    /// <summary>
    ///     A principal established by a current poll must retain its own verified scheme and permissions.
    /// </summary>
    /// <param name="permission">The current caller's permission.</param>
    /// <param name="boundary">The connection, endpoint or scheme boundary being exercised.</param>
    /// <param name="subscriptionAllowed">Whether the current caller may subscribe.</param>
    /// <returns>A task that completes when the current-call provenance assertions pass.</returns>
    [Theory]
    [InlineData("read", "same", true)]
    [InlineData("write", "same", false)]
    [InlineData("read", "other-token", false)]
    [InlineData("read", "other-endpoint", false)]
    [InlineData("read", "other-scheme", false)]
    public async Task SubscribeUsesOnlyTheCurrentPollsBoundAuthentication(
        string permission,
        string boundary,
        bool subscriptionAllowed
    )
    {
        const string scheme = "Bearer";
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSignalR();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        FakeTimeProvider clock = new(new(2040, 1, 1, 0, 0, 0, TimeSpan.Zero));
        builder.Services.AddAuthentication(scheme).AddBearerToken(scheme, options => options.TimeProvider = clock);
        builder.Services.AddAuthentication().AddBearerToken(OtherScheme);
        builder.Services.AddAuthorizationBuilder().SetDefaultPolicy(CreatePolicy(scheme));
        await using WebApplication app = builder.Build();
        AuthenticationMiddleware middleware = new(
            _ => Task.CompletedTask,
            app.Services.GetRequiredService<IAuthenticationSchemeProvider>());
        await using AsyncServiceScope firstScope = app.Services.CreateAsyncScope();
        DefaultHttpContext first = CreateBearerContext(firstScope.ServiceProvider, "read");
        await middleware.Invoke(first);
        Assert.True(first.User.Identity?.IsAuthenticated);
        AuthorizationPolicy connectionPolicy =
            new AuthorizationPolicyBuilder(scheme).RequireAuthenticatedUser().Build();
        await DispatchHubEndpointAsync(app, first, connectionPolicy, false);
        await using AsyncServiceScope pollScope = app.Services.CreateAsyncScope();
        DefaultHttpContext poll = CreateBearerContext(pollScope.ServiceProvider, permission);
        await middleware.Invoke(poll);
        Assert.True(poll.User.Identity?.IsAuthenticated);
        Assert.NotSame(first.User, poll.User);
        poll.Request.Method = first.Request.Method;
        poll.Request.Path = first.Request.Path;
        poll.Request.QueryString = boundary == "other-token"
            ? new("?id=another-connection")
            : first.Request.QueryString;
        poll.SetEndpoint(
            boundary == "other-endpoint" ? new(_ => Task.CompletedTask, new(), "other hub") : first.GetEndpoint());
        await ConnectionAuthenticationSnapshot.CaptureAsync(poll);
        await using AsyncServiceScope connectionScope = app.Services.CreateAsyncScope();
        DefaultHttpContext retained = CloneConnectionContext(first, connectionScope.ServiceProvider);
        retained.User = poll.User;
        Assert.Equal(first.Request.Headers.Authorization, retained.Request.Headers.Authorization);
        Assert.NotEqual(poll.Request.Headers.Authorization, retained.Request.Headers.Authorization);
        string requestedScheme = boundary == "other-scheme" ? OtherScheme : scheme;
        using InletHub hub = CreateHub(retained, poll.User, requestedScheme, out IInletSubscriptionGrain grain);
        if (subscriptionAllowed)
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

        Assert.Same(poll.User, retained.User);
        Assert.Same(poll.User, hub.Context.User);
    }

    /// <summary>
    ///     An injected authentication service must use the replacement poll's proven principal.
    /// </summary>
    /// <param name="replacementAllowed">Whether the replacement caller has read permission.</param>
    /// <returns>A task that completes when stale grant and denial boundaries have been verified.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SubscribeUsesReplacementCallerWithInjectedAuthenticationService(
        bool replacementAllowed
    )
    {
        const string scheme = OtherScheme;
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddInletServer();
        builder.Services.AddSingleton<HubLifetimeManager<InletHub>, DefaultHubLifetimeManager<InletHub>>();
        builder.Services.AddAuthentication(scheme)
            .AddScheme<AuthenticationSchemeOptions, CompositeConnectionAuthenticationHandler>(scheme, _ => { });
        builder.Services.AddAuthorizationBuilder().SetDefaultPolicy(CreatePolicy(scheme));
        builder.Services.AddScoped<IPolicyEvaluator, InjectedConnectionPolicyEvaluator>();
        await using WebApplication app = builder.Build();
        await using AsyncServiceScope connectionScope = app.Services.CreateAsyncScope();
        DefaultHttpContext original = CreateOriginalContext(connectionScope.ServiceProvider);
        original.Request.Headers[scheme] = replacementAllowed ? "write" : "read";
        AuthorizationPolicy policy = CreatePolicy(scheme);
        IPolicyEvaluator evaluator = connectionScope.ServiceProvider.GetRequiredService<IPolicyEvaluator>();
        Assert.True((await evaluator.AuthenticateAsync(policy, original)).Succeeded);
        AuthenticationMiddleware originalMiddleware = new(
            _ => Task.CompletedTask,
            connectionScope.ServiceProvider.GetRequiredService<IAuthenticationSchemeProvider>());
        await originalMiddleware.Invoke(original);
        await DispatchHubEndpointAsync(app, original, policy);
        DefaultHttpContext retained = CloneConnectionContext(original, connectionScope.ServiceProvider);
        await using AsyncServiceScope pollScope = app.Services.CreateAsyncScope();
        DefaultHttpContext poll = CreateOriginalContext(pollScope.ServiceProvider);
        poll.Request.Headers[scheme] = replacementAllowed ? "read" : "write";
        poll.SetEndpoint(original.GetEndpoint());
        poll.Request.QueryString = new("?id=unavailable-connection");
        AuthenticationMiddleware pollMiddleware = new(
            _ => Task.CompletedTask,
            pollScope.ServiceProvider.GetRequiredService<IAuthenticationSchemeProvider>());
        await pollMiddleware.Invoke(poll);
        await ConnectionAuthenticationSnapshot.CaptureAsync(poll);
        ClaimsPrincipal caller = poll.User;
        retained.User = caller;
        Assert.NotNull(ConnectionAuthenticationSnapshot.GetAuthenticationResult(retained, caller, scheme));
        Assert.Equal(1, Assert.IsType<int>(original.Items[scheme + "-calls"]));
        using InletHub hub = CreateHub(retained, caller, scheme, out IInletSubscriptionGrain grain);
        if (replacementAllowed)
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

        Assert.Same(caller, retained.User);
        Assert.Same(caller, hub.Context.User);
        Assert.Same(connectionScope.ServiceProvider, retained.RequestServices);
        Assert.Same(evaluator, connectionScope.ServiceProvider.GetRequiredService<IPolicyEvaluator>());
    }

    /// <summary>
    ///     Certificate-only projection policies cannot borrow bearer identities from a composite endpoint result.
    /// </summary>
    /// <param name="certificatePermission">The certificate permission, or no-result marker.</param>
    /// <param name="bearerPermission">The other scheme's permission, or no-result marker.</param>
    /// <param name="allowed">Whether certificate-only authorization should allow the subscription.</param>
    /// <param name="cloneIdentities">Whether transformation creates independent identities and adds claims.</param>
    /// <returns>A task that completes when scheme attribution and authorization have been verified.</returns>
    [Theory]
    [InlineData("read", "write", true, false)]
    [InlineData("read", "write", true, true)]
    [InlineData("write", "read", false, false)]
    [InlineData("write", "read", false, true)]
    [InlineData("none", "read", false, false)]
    [InlineData("none", "read", false, true)]
    [InlineData("read", "none", true, false)]
    [InlineData("read", "none", true, true)]
    public async Task SubscribeUsesTransformedSelectedCompositeIdentity(
        string certificatePermission,
        string bearerPermission,
        bool allowed,
        bool cloneIdentities
    )
    {
        List<ClaimsPrincipal> decisions = [];
        ConnectionClaimsTransformation transformation = new(cloneIdentities);
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddInletServer();
        builder.Services.AddSingleton<HubLifetimeManager<InletHub>, DefaultHubLifetimeManager<InletHub>>();
        builder.Services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, CompositeConnectionAuthenticationHandler>(TlsScheme, _ => { })
            .AddScheme<AuthenticationSchemeOptions, CompositeConnectionAuthenticationHandler>(OtherScheme, _ => { });
        builder.Services.AddSingleton<IClaimsTransformation>(transformation);
        builder.Services.AddAuthorizationBuilder()
            .SetDefaultPolicy(
                new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                    .RequireClaim("permission", "read")
                    .RequireAssertion(context =>
                    {
                        decisions.Add(context.User);
                        return true;
                    })
                    .Build());
        await using WebApplication app = builder.Build();
        await using AsyncServiceScope requestScope = app.Services.CreateAsyncScope();
        DefaultHttpContext original = CreateOriginalContext(requestScope.ServiceProvider);
        original.Request.Headers[TlsScheme] = certificatePermission;
        original.Request.Headers[OtherScheme] = bearerPermission;
        AuthorizationPolicy endpointPolicy =
            new AuthorizationPolicyBuilder(TlsScheme, OtherScheme).RequireAuthenticatedUser().Build();
        PolicyEvaluator evaluator = new(app.Services.GetRequiredService<IAuthorizationService>());
        AuthenticateResult established = await evaluator.AuthenticateAsync(endpointPolicy, original);
        Assert.True(established.Succeeded);
        Assert.True((await evaluator.AuthorizeAsync(endpointPolicy, established, original, original)).Succeeded);
        IAuthenticateResultFeature feature = Substitute.For<IAuthenticateResultFeature>();
        feature.AuthenticateResult.Returns(established);
        original.Features.Set(feature);
        int transformations = transformation.Calls;
        await DispatchHubEndpointAsync(app, original, endpointPolicy);
        Assert.Equal(transformations, transformation.Calls);
        Assert.Equal(1, Assert.IsType<int>(original.Items[TlsScheme + "-calls"]));
        Assert.Equal(1, Assert.IsType<int>(original.Items[OtherScheme + "-calls"]));
        ClaimsPrincipal caller = original.User;
        await using AsyncServiceScope connectionScope = app.Services.CreateAsyncScope();
        DefaultHttpContext retained = CloneConnectionContext(original, connectionScope.ServiceProvider);
        Assert.Null(retained.Features.Get<ITlsConnectionFeature>());
        Assert.False((await evaluator.AuthenticateAsync(CreatePolicy(TlsScheme), retained)).Succeeded);
        retained.User = caller;
        using InletHub hub = CreateHub(retained, caller, TlsScheme, out IInletSubscriptionGrain grain);
        if (allowed)
        {
            Assert.Equal("subscription-1", await hub.SubscribeAsync(ProjectionPath, EntityId));
            await grain.Received(1).SubscribeAsync(ProjectionPath, EntityId);
            ClaimsPrincipal selected = Assert.Single(decisions);
            Assert.Equal(TlsScheme, selected.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            Assert.True(selected.HasClaim("permission", "read"));
            Assert.False(selected.HasClaim(ClaimTypes.NameIdentifier, OtherScheme));
            Assert.False(selected.HasClaim("permission", "write"));
            Assert.Equal(cloneIdentities, selected.HasClaim("transformed", "yes"));
        }
        else
        {
            HubException exception =
                await Assert.ThrowsAsync<HubException>(() => hub.SubscribeAsync(ProjectionPath, EntityId));
            Assert.Equal(InletHubConstants.SubscriptionDeniedMessage, exception.Message);
            await grain.DidNotReceive().SubscribeAsync(Arg.Any<string>(), Arg.Any<string>());
        }

        Assert.Same(caller, hub.Context.User);
        Assert.Same(caller, retained.User);
    }
}