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
        AuthorizationPolicy? defaultPolicy = null,
        IPolicyEvaluator? policyEvaluator = null,
        bool registerAuthentication = true,
        bool registerPolicyEvaluator = true,
        IAuthenticationHandlerProvider? handlerProvider = null
    )
    {
        ServiceCollection services = new();
        services.AddLogging();
        if (registerAuthentication)
        {
            services.AddAuthentication(BearerScheme).AddBearerToken(BearerScheme).AddBearerToken(OtherScheme);
        }

        if (authenticationService is not null)
        {
            services.AddSingleton(authenticationService);
        }

        if (handlerProvider is not null)
        {
            services.AddSingleton(handlerProvider);
        }

        AuthorizationPolicy authorizationPolicy = defaultPolicy ??
                                                  new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                                                      .RequireClaim("permission", "read")
                                                      .Build();
        if (registerPolicyEvaluator)
        {
            services.AddAuthorizationBuilder().SetDefaultPolicy(authorizationPolicy);
        }
        else
        {
            services.AddAuthorizationCore(options => options.DefaultPolicy = authorizationPolicy);
        }

        if (policyEvaluator is not null)
        {
            services.AddSingleton(policyEvaluator);
        }

        return services.BuildServiceProvider();
    }

    /// <summary>
    ///     Core authorization without a policy evaluator should use the generic subscription denial.
    /// </summary>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Fact]
    public async Task SubscribeDeniesUnavailablePolicyEvaluator()
    {
        await using ServiceProvider services = CreateServices(registerPolicyEvaluator: false);
        Assert.NotNull(services.GetService<IAuthenticationSchemeProvider>());
        Assert.Null(services.GetService<IPolicyEvaluator>());
        using InletHub hub = CreateHub(
            services,
            BearerScheme,
            out IInletSubscriptionGrain grain,
            out ILogger<InletHub> _);
        HubException exception =
            await Assert.ThrowsAsync<HubException>(() => hub.SubscribeAsync(ProjectionPath, EntityId));
        Assert.Equal(InletHubConstants.SubscriptionDeniedMessage, exception.Message);
        await grain.DidNotReceive().SubscribeAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    /// <summary>
    ///     An unavailable authentication scheme provider should use the generic subscription denial.
    /// </summary>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Fact]
    public async Task SubscribeDeniesUnavailableSchemeProvider()
    {
        await using ServiceProvider services = CreateServices(registerAuthentication: false);
        using InletHub hub = CreateHub(
            services,
            BearerScheme,
            out IInletSubscriptionGrain grain,
            out ILogger<InletHub> _);
        HubException exception =
            await Assert.ThrowsAsync<HubException>(() => hub.SubscribeAsync(ProjectionPath, EntityId));
        Assert.Equal(InletHubConstants.SubscriptionDeniedMessage, exception.Message);
        await grain.DidNotReceive().SubscribeAsync(Arg.Any<string>(), Arg.Any<string>());
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
    ///     Custom evaluator authorization decisions and ordinary requirements must both permit subscription.
    /// </summary>
    /// <param name="decision">The evaluator decision or missing requirement being verified.</param>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Theory]
    [InlineData("success")]
    [InlineData("forbid")]
    [InlineData("challenge")]
    [InlineData("missing-permission")]
    public async Task SubscribeHonorsCustomEvaluatorAuthorization(
        string decision
    )
    {
        ArgumentNullException.ThrowIfNull(decision);
        const string virtualScheme = "Virtual";
        IPolicyEvaluator evaluator = Substitute.For<IPolicyEvaluator>();
        ClaimsPrincipal principal = CreatePrincipal(
            "virtual-user",
            decision == "missing-permission" ? "write" : "read");
        AuthenticateResult authentication = AuthenticateResult.Success(new(principal, virtualScheme));
        evaluator.AuthenticateAsync(Arg.Any<AuthorizationPolicy>(), Arg.Any<HttpContext>()).Returns(authentication);
        evaluator.AuthorizeAsync(
                Arg.Any<AuthorizationPolicy>(),
                Arg.Any<AuthenticateResult>(),
                Arg.Any<HttpContext>(),
                Arg.Any<object?>())
            .Returns(call =>
            {
                Assert.Same(principal, call.Arg<HttpContext>().User);
                return decision switch
                {
                    "forbid" => PolicyAuthorizationResult.Forbid(),
                    "challenge" => PolicyAuthorizationResult.Challenge(),
                    var _ => PolicyAuthorizationResult.Success(),
                };
            });
        await using ServiceProvider services = CreateServices(
            policyEvaluator: evaluator,
            registerAuthentication: false);
        using InletHub hub = CreateHub(
            services,
            virtualScheme,
            out IInletSubscriptionGrain grain,
            out ILogger<InletHub> logger);
        if (decision == "success")
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

        await evaluator.Received(1)
            .AuthenticateAsync(
                Arg.Is<AuthorizationPolicy>(policy => policy.AuthenticationSchemes.Contains(virtualScheme)),
                Arg.Any<HttpContext>());
        await evaluator.Received(1)
            .AuthorizeAsync(
                Arg.Is<AuthorizationPolicy>(policy => policy.AuthenticationSchemes.Contains(virtualScheme)),
                authentication,
                hub.Context.GetHttpContext()!,
                Arg.Is<object?>(resource => resource == null));
        int eventId = decision == "success" ? 7 : 8;
        object?[] arguments = Assert.Single(
                logger.ReceivedCalls(),
                call => (call.GetMethodInfo().Name == nameof(ILogger.Log)) &&
                        call.GetArguments()[1] is EventId id &&
                        (id.Id == eventId))
            .GetArguments();
        IEnumerable<KeyValuePair<string, object?>> state =
            Assert.IsType<IEnumerable<KeyValuePair<string, object?>>>(arguments[2], false);
        Assert.Equal("virtual-user", Assert.Single(state, field => field.Key == "UserId").Value);
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

    /// <summary>
    ///     A subscription decision must restore the retained HTTP principal and authentication result.
    /// </summary>
    /// <param name="decision">The authentication or authorization outcome.</param>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Theory]
    [InlineData("success")]
    [InlineData("missing-permission")]
    [InlineData("no-result")]
    [InlineData("custom-success")]
    [InlineData("custom-forbid")]
    [InlineData("custom-error")]
    [InlineData("success-null-result")]
    [InlineData("no-result-null-result")]
    public async Task SubscribeRestoresRetainedHttpAuthentication(
        string decision
    )
    {
        ArgumentNullException.ThrowIfNull(decision);
        ClaimsPrincipal selected = CreatePrincipal(
            "selected-user",
            decision == "missing-permission" ? "write" : "read");
        AuthenticateResult authentication = decision.StartsWith("no-result", StringComparison.Ordinal)
            ? AuthenticateResult.NoResult()
            : AuthenticateResult.Success(new(selected, BearerScheme));
        IAuthenticationService authenticationService = Substitute.For<IAuthenticationService>();
        authenticationService.AuthenticateAsync(Arg.Any<HttpContext>(), BearerScheme).Returns(authentication);
        IPolicyEvaluator? evaluator = null;
        if (decision.StartsWith("custom", StringComparison.Ordinal))
        {
            evaluator = Substitute.For<IPolicyEvaluator>();
            evaluator.AuthenticateAsync(Arg.Any<AuthorizationPolicy>(), Arg.Any<HttpContext>()).Returns(authentication);
            evaluator.AuthorizeAsync(
                    Arg.Any<AuthorizationPolicy>(),
                    Arg.Any<AuthenticateResult>(),
                    Arg.Any<HttpContext>(),
                    Arg.Any<object?>())
                .Returns(call =>
                {
                    Assert.Same(selected, call.Arg<HttpContext>().User);
                    if (decision == "custom-error")
                    {
                        throw new InvalidOperationException("policy evaluator failure");
                    }

                    return decision == "custom-forbid"
                        ? PolicyAuthorizationResult.Forbid()
                        : PolicyAuthorizationResult.Success();
                });
        }

        await using ServiceProvider services = CreateServices(authenticationService, policyEvaluator: evaluator);
        using InletHub hub = CreateHub(
            services,
            BearerScheme,
            out IInletSubscriptionGrain grain,
            out ILogger<InletHub> _);
        HttpContext context = Assert.IsType<HttpContext>(hub.Context.GetHttpContext(), false);
        ClaimsPrincipal original = context.User;
        AuthenticateResult originalResult = AuthenticateResult.Success(new(original, OtherScheme));
        authenticationService.AuthenticateAsync(context, BearerScheme)
            .Returns(call => call.Arg<HttpContext>().Request.Path == "/connect" ? originalResult : authentication);
        context.Request.Path = "/connect";
        AuthenticationMiddleware middleware = new(
            _ => Task.CompletedTask,
            services.GetRequiredService<IAuthenticationSchemeProvider>());
        await middleware.Invoke(context);
        context.Request.Path = "/hubs/inlet";
        IAuthenticateResultFeature resultFeature = Assert.IsType<IAuthenticateResultFeature>(
            context.Features.Get<IAuthenticateResultFeature>(),
            false);
        Assert.Same(originalResult, resultFeature.AuthenticateResult);
        AuthenticateResult? expectedResult = originalResult;
        if (decision.EndsWith("-null-result", StringComparison.Ordinal))
        {
            context.User = original;
            Assert.Null(resultFeature.AuthenticateResult);
            expectedResult = null;
        }

        if (decision is "success" or "custom-success" or "success-null-result")
        {
            Assert.Equal("subscription-1", await hub.SubscribeAsync(ProjectionPath, EntityId));
            await grain.Received(1).SubscribeAsync(ProjectionPath, EntityId);
        }
        else if (decision == "custom-error")
        {
            InvalidOperationException exception =
                await Assert.ThrowsAsync<InvalidOperationException>(() => hub.SubscribeAsync(ProjectionPath, EntityId));
            Assert.Equal("policy evaluator failure", exception.Message);
            await grain.DidNotReceive().SubscribeAsync(Arg.Any<string>(), Arg.Any<string>());
        }
        else
        {
            HubException exception =
                await Assert.ThrowsAsync<HubException>(() => hub.SubscribeAsync(ProjectionPath, EntityId));
            Assert.Equal(InletHubConstants.SubscriptionDeniedMessage, exception.Message);
            await grain.DidNotReceive().SubscribeAsync(Arg.Any<string>(), Arg.Any<string>());
        }

        Assert.Same(original, context.User);
        Assert.Same(original, hub.Context.User);
        Assert.Same(resultFeature, context.Features.Get<IAuthenticateResultFeature>());
        Assert.Same(expectedResult, resultFeature.AuthenticateResult);
    }

    /// <summary>
    ///     Selected-scheme authentication should use the evaluator configured by the host.
    /// </summary>
    /// <param name="authenticated">Whether the configured evaluator authenticates the caller.</param>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SubscribeUsesConfiguredPolicyEvaluator(
        bool authenticated
    )
    {
        IAuthenticationService authenticationService = Substitute.For<IAuthenticationService>();
        authenticationService.AuthenticateAsync(Arg.Any<HttpContext>(), BearerScheme)
            .Returns(
                authenticated
                    ? AuthenticateResult.NoResult()
                    : AuthenticateResult.Success(new(CreatePrincipal("handler-user"), BearerScheme)));
        IPolicyEvaluator evaluator = Substitute.For<IPolicyEvaluator>();
        ClaimsPrincipal selectedPrincipal = CreatePrincipal("configured-user");
        evaluator.AuthenticateAsync(Arg.Any<AuthorizationPolicy>(), Arg.Any<HttpContext>())
            .Returns(call =>
            {
                HttpContext httpContext = call.Arg<HttpContext>();
                httpContext.User = authenticated ? selectedPrincipal : new(new ClaimsIdentity());
                return authenticated
                    ? AuthenticateResult.Success(new(selectedPrincipal, BearerScheme))
                    : AuthenticateResult.NoResult();
            });
        evaluator.AuthorizeAsync(
                Arg.Any<AuthorizationPolicy>(),
                Arg.Any<AuthenticateResult>(),
                Arg.Any<HttpContext>(),
                Arg.Any<object?>())
            .Returns(PolicyAuthorizationResult.Success());
        await using ServiceProvider services = CreateServices(authenticationService, policyEvaluator: evaluator);
        Assert.Same(evaluator, services.GetRequiredService<IPolicyEvaluator>());
        if (!authenticated)
        {
            DefaultHttpContext referenceContext = new()
            {
                RequestServices = services,
            };
            AuthorizationPolicy referencePolicy = new AuthorizationPolicyBuilder(BearerScheme)
                .RequireAuthenticatedUser()
                .RequireClaim("permission", "read")
                .Build();
            PolicyEvaluator defaultEvaluator = new(services.GetRequiredService<IAuthorizationService>());
            AuthenticateResult authentication =
                await defaultEvaluator.AuthenticateAsync(referencePolicy, referenceContext);
            Assert.True(authentication.Succeeded);
            Assert.True(
                (await defaultEvaluator.AuthorizeAsync(referencePolicy, authentication, referenceContext, null))
                .Succeeded);
        }

        using InletHub hub = CreateHub(
            services,
            BearerScheme,
            out IInletSubscriptionGrain grain,
            out ILogger<InletHub> _);
        if (authenticated)
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

        await evaluator.Received(1)
            .AuthenticateAsync(
                Arg.Is<AuthorizationPolicy>(policy => policy.AuthenticationSchemes.Contains(BearerScheme)),
                Arg.Any<HttpContext>());
    }

    /// <summary>
    ///     The default evaluator should delegate virtual schemes to the host's authentication service.
    /// </summary>
    /// <param name="registerAuthentication">Whether standard scheme registrations are available.</param>
    /// <param name="authenticated">Whether the custom service accepts the caller.</param>
    /// <param name="derivedService">Whether the host service derives from the framework's public base class.</param>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Theory]
    [InlineData(true, true, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(false, false, false)]
    [InlineData(true, true, true)]
    [InlineData(false, true, true)]
    public async Task SubscribeUsesCustomAuthenticationServiceForVirtualScheme(
        bool registerAuthentication,
        bool authenticated,
        bool derivedService
    )
    {
        const string virtualScheme = "Virtual";
        IAuthenticationService authentication = derivedService
            ? Substitute.For<AuthenticationService>(
                Substitute.For<IAuthenticationSchemeProvider>(),
                Substitute.For<IAuthenticationHandlerProvider>(),
                Substitute.For<IClaimsTransformation>(),
                Options.Create(new AuthenticationOptions()))
            : Substitute.For<IAuthenticationService>();
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), virtualScheme)
            .Returns(
                authenticated
                    ? AuthenticateResult.Success(new(CreatePrincipal("virtual-service-user"), virtualScheme))
                    : AuthenticateResult.NoResult());
        await using ServiceProvider services = CreateServices(
            authentication,
            registerAuthentication: registerAuthentication);
        Assert.IsType<PolicyEvaluator>(services.GetRequiredService<IPolicyEvaluator>());
        IAuthenticationSchemeProvider? provider = services.GetService<IAuthenticationSchemeProvider>();
        if (provider is not null)
        {
            Assert.Null(await provider.GetSchemeAsync(virtualScheme));
        }

        using InletHub hub = CreateHub(
            services,
            virtualScheme,
            out IInletSubscriptionGrain grain,
            out ILogger<InletHub> _);
        if (authenticated)
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

        await authentication.Received(1).AuthenticateAsync(Arg.Any<HttpContext>(), virtualScheme);
    }

    /// <summary>
    ///     A custom evaluator should handle virtual schemes without default authentication registration.
    /// </summary>
    /// <param name="registerAuthentication">Whether the host registers standard authentication services.</param>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SubscribeUsesCustomEvaluatorForVirtualScheme(
        bool registerAuthentication
    )
    {
        const string virtualScheme = "Virtual";
        IPolicyEvaluator evaluator = Substitute.For<IPolicyEvaluator>();
        ClaimsPrincipal principal = CreatePrincipal("virtual-user");
        evaluator.AuthenticateAsync(Arg.Any<AuthorizationPolicy>(), Arg.Any<HttpContext>())
            .Returns(AuthenticateResult.Success(new(principal, virtualScheme)));
        evaluator.AuthorizeAsync(
                Arg.Any<AuthorizationPolicy>(),
                Arg.Any<AuthenticateResult>(),
                Arg.Any<HttpContext>(),
                Arg.Any<object?>())
            .Returns(PolicyAuthorizationResult.Success());
        await using ServiceProvider services = CreateServices(
            policyEvaluator: evaluator,
            registerAuthentication: registerAuthentication);
        using InletHub hub = CreateHub(
            services,
            virtualScheme,
            out IInletSubscriptionGrain grain,
            out ILogger<InletHub> _);
        Assert.Equal("subscription-1", await hub.SubscribeAsync(ProjectionPath, EntityId));
        await grain.Received(1).SubscribeAsync(ProjectionPath, EntityId);
        await evaluator.Received(1)
            .AuthenticateAsync(
                Arg.Is<AuthorizationPolicy>(policy => policy.AuthenticationSchemes.Contains(virtualScheme)),
                Arg.Any<HttpContext>());
    }

    /// <summary>
    ///     The default authentication service should honor virtual schemes from a custom handler provider.
    /// </summary>
    /// <param name="authenticated">Whether the host-provided handler authenticates the caller.</param>
    /// <returns>A task that completes when the assertions have been verified.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SubscribeUsesCustomHandlerProviderForVirtualScheme(
        bool authenticated
    )
    {
        const string virtualScheme = "Virtual";
        IAuthenticationHandler handler = Substitute.For<IAuthenticationHandler>();
        handler.AuthenticateAsync()
            .Returns(
                authenticated
                    ? AuthenticateResult.Success(new(CreatePrincipal("virtual-handler-user"), virtualScheme))
                    : AuthenticateResult.NoResult());
        IAuthenticationHandlerProvider handlers = Substitute.For<IAuthenticationHandlerProvider>();
        handlers.GetHandlerAsync(Arg.Any<HttpContext>(), virtualScheme).Returns(handler);
        await using ServiceProvider services = CreateServices(handlerProvider: handlers);
        Assert.IsType<PolicyEvaluator>(services.GetRequiredService<IPolicyEvaluator>());
        Assert.IsType<AuthenticationService>(services.GetRequiredService<IAuthenticationService>(), false);
        Assert.Null(await services.GetRequiredService<IAuthenticationSchemeProvider>().GetSchemeAsync(virtualScheme));
        using InletHub hub = CreateHub(
            services,
            virtualScheme,
            out IInletSubscriptionGrain grain,
            out ILogger<InletHub> _);
        if (authenticated)
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

        await handlers.Received(1).GetHandlerAsync(Arg.Any<HttpContext>(), virtualScheme);
        await handler.Received(1).AuthenticateAsync();
    }
}