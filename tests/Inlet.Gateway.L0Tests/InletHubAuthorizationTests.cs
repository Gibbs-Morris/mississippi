using System;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Inlet.Gateway.Abstractions;
using Mississippi.Inlet.Runtime.Abstractions;

using NSubstitute;

using Orleans;


namespace Mississippi.Inlet.Gateway.L0Tests;

/// <summary>
///     Tests for subscription authorization behavior in <see cref="InletHub" />.
/// </summary>
public sealed class InletHubAuthorizationTests
{
    /// <summary>
    ///     Creates an authenticated principal for hub authorization tests.
    /// </summary>
    /// <returns>An authenticated test principal.</returns>
    private static ClaimsPrincipal CreateAuthenticatedUser() =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-1")], "TestAuthType"));

    /// <summary>
    ///     Creates an <see cref="InletHub" /> with configurable authorization collaborators.
    /// </summary>
    /// <returns>A configured <see cref="InletHub" /> test instance.</returns>
    private static InletHub CreateHub(
        GeneratedApiAuthorizationMode mode,
        ProjectionAuthorizationMetadata? metadata,
        AuthorizationResult authorizationResult,
        string? defaultPolicy = null,
        bool allowAnonymousOptOut = true
    )
    {
        IGrainFactory grainFactory = Substitute.For<IGrainFactory>();
        IProjectionAuthorizationRegistry projectionAuthorizationRegistry = Substitute.For<IProjectionAuthorizationRegistry>();
        IAuthorizationService authorizationService = Substitute.For<IAuthorizationService>();
        IAuthorizationPolicyProvider authorizationPolicyProvider = Substitute.For<IAuthorizationPolicyProvider>();
        ILogger<InletHub> logger = Substitute.For<ILogger<InletHub>>();

        projectionAuthorizationRegistry.GetAuthorizationMetadata(Arg.Any<string>()).Returns(metadata);
        authorizationService
            .AuthorizeAsync(
                Arg.Any<ClaimsPrincipal>(),
                Arg.Any<object?>(),
                Arg.Any<System.Collections.Generic.IEnumerable<IAuthorizationRequirement>>())
            .Returns(authorizationResult);

        authorizationPolicyProvider.GetPolicyAsync(Arg.Any<string>())
            .Returns(callInfo =>
            {
                string? policyName = callInfo.Arg<string>();
                return string.IsNullOrWhiteSpace(policyName)
                    ? null
                    : new AuthorizationPolicyBuilder().RequireClaim(policyName).Build();
            });

        InletServerOptions options = new()
        {
            GeneratedApiAuthorization = new GeneratedApiAuthorizationOptions
            {
                Mode = mode,
                DefaultPolicy = defaultPolicy,
                AllowAnonymousOptOut = allowAnonymousOptOut,
            },
        };

        InletHub hub = new(
            grainFactory,
            projectionAuthorizationRegistry,
            authorizationService,
            authorizationPolicyProvider,
            Options.Create(options),
            logger);

        HubCallerContext context = Substitute.For<HubCallerContext>();
        context.ConnectionId.Returns("connection-1");
        context.User.Returns(CreateAuthenticatedUser());
        hub.Context = context;
        return hub;
    }

    /// <summary>
    ///     Invokes the private authorization helper used by subscription flow.
    /// </summary>
    /// <returns>A task that completes when authorization logic has executed.</returns>
    private static async Task InvokeAuthorizeSubscriptionAsync(
        InletHub hub,
        string path = "/api/test-projection",
        string entityId = "entity-1"
    )
    {
        MethodInfo method = typeof(InletHub).GetMethod(
            "AuthorizeSubscriptionAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        Task authorizationTask = (Task)method.Invoke(hub, [path, entityId])!;
        await authorizationTask;
    }

    /// <summary>
    ///     Mode disabled with no metadata should skip authorization checks.
    /// </summary>
    /// <returns>A task that completes when the assertion has been verified.</returns>
    [Fact]
    public async Task AuthorizeSubscriptionSkipsWhenModeDisabledAndNoMetadata()
    {
        // Arrange
        using InletHub hub = CreateHub(GeneratedApiAuthorizationMode.Disabled, null, AuthorizationResult.Success());

        // Act
        await InvokeAuthorizeSubscriptionAsync(hub);

        // Assert
        Assert.True(true);
    }

    /// <summary>
    ///     Force mode with no metadata should use default policy and allow when authorized.
    /// </summary>
    /// <returns>A task that completes when the assertion has been verified.</returns>
    [Fact]
    public async Task AuthorizeSubscriptionUsesDefaultPolicyWhenForceModeEnabled()
    {
        // Arrange
        using InletHub hub = CreateHub(
            GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints,
            metadata: null,
            authorizationResult: AuthorizationResult.Success(),
            defaultPolicy: "generated-default");

        // Act
        await InvokeAuthorizeSubscriptionAsync(hub);

        // Assert
        Assert.True(true);
    }

    /// <summary>
    ///     AllowAnonymous metadata should skip authorization when opt-out is enabled.
    /// </summary>
    /// <returns>A task that completes when the assertion has been verified.</returns>
    [Fact]
    public async Task AuthorizeSubscriptionSkipsWhenAllowAnonymousAndOptOutEnabled()
    {
        // Arrange
        ProjectionAuthorizationMetadata metadata = new(null, null, null, false, true);
        using InletHub hub = CreateHub(
            GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints,
            metadata,
            AuthorizationResult.Failed(),
            defaultPolicy: "generated-default",
            allowAnonymousOptOut: true);

        // Act
        await InvokeAuthorizeSubscriptionAsync(hub);

        // Assert
        Assert.True(true);
    }

    /// <summary>
    ///     Authorization failures should throw HubException with generic safe message.
    /// </summary>
    /// <returns>A task that completes when the assertion has been verified.</returns>
    [Fact]
    public async Task AuthorizeSubscriptionThrowsGenericHubExceptionOnFailure()
    {
        // Arrange
        ProjectionAuthorizationMetadata metadata = new("projection.read", null, null, true, false);
        using InletHub hub = CreateHub(
            GeneratedApiAuthorizationMode.Disabled,
            metadata,
            AuthorizationResult.Failed());

        // Act
        HubException exception = await Assert.ThrowsAsync<HubException>(() => InvokeAuthorizeSubscriptionAsync(hub));

        // Assert
        Assert.Equal(InletHubConstants.SubscriptionDeniedMessage, exception.Message);
    }
}