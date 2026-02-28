using System.Collections.Generic;
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
        new(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, "user-1")], "TestAuthType"));

    /// <summary>
    ///     Creates an <see cref="InletHub" /> with configurable authorization collaborators.
    /// </summary>
    /// <returns>A configured <see cref="InletHub" /> test instance.</returns>
    private static AuthorizationDependencies CreateAuthorizationDependencies(
        GeneratedApiAuthorizationMode mode,
        ProjectionAuthorizationMetadata? metadata,
        AuthorizationResult authorizationResult,
        string? defaultPolicy = null,
        bool allowAnonymousOptOut = true
    )
    {
        IGrainFactory grainFactory = Substitute.For<IGrainFactory>();
        IProjectionAuthorizationRegistry projectionAuthorizationRegistry =
            Substitute.For<IProjectionAuthorizationRegistry>();
        IAuthorizationService authorizationService = Substitute.For<IAuthorizationService>();
        IAuthorizationPolicyProvider authorizationPolicyProvider = Substitute.For<IAuthorizationPolicyProvider>();
        ILogger<InletHub> logger = Substitute.For<ILogger<InletHub>>();
        projectionAuthorizationRegistry.GetAuthorizationMetadata(Arg.Any<string>()).Returns(metadata);
        authorizationService.AuthorizeAsync(
                Arg.Any<ClaimsPrincipal>(),
                Arg.Any<object?>(),
                Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(authorizationResult);
        authorizationPolicyProvider.GetPolicyAsync(Arg.Any<string>())
            .Returns(callInfo =>
            {
                string? policyName = callInfo.Arg<string>();
                return string.IsNullOrWhiteSpace(policyName)
                    ? null
                    : new AuthorizationPolicyBuilder().RequireClaim(policyName).Build();
            });
        authorizationPolicyProvider.GetDefaultPolicyAsync()
            .Returns(new AuthorizationPolicyBuilder().RequireClaim("default-policy").Build());
        InletServerOptions options = new()
        {
            GeneratedApiAuthorization = new()
            {
                Mode = mode,
                DefaultPolicy = defaultPolicy,
                AllowAnonymousOptOut = allowAnonymousOptOut,
            },
        };
        HubCallerContext context = Substitute.For<HubCallerContext>();
        context.ConnectionId.Returns("connection-1");
        context.User.Returns(CreateAuthenticatedUser());
        return new(
            grainFactory,
            projectionAuthorizationRegistry,
            authorizationService,
            authorizationPolicyProvider,
            Options.Create(options),
            logger,
            context);
    }

    private static InletHub CreateHub(
        AuthorizationDependencies dependencies
    )
    {
        InletHub hub = new(
            dependencies.GrainFactory,
            dependencies.ProjectionAuthorizationRegistry,
            dependencies.AuthorizationService,
            dependencies.AuthorizationPolicyProvider,
            dependencies.InletServerOptions,
            dependencies.Logger);
        hub.Context = dependencies.Context;
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

    private sealed record AuthorizationDependencies(
        IGrainFactory GrainFactory,
        IProjectionAuthorizationRegistry ProjectionAuthorizationRegistry,
        IAuthorizationService AuthorizationService,
        IAuthorizationPolicyProvider AuthorizationPolicyProvider,
        IOptions<InletServerOptions> InletServerOptions,
        ILogger<InletHub> Logger,
        HubCallerContext Context
    );

    /// <summary>
    ///     AllowAnonymous metadata should not bypass authorization when opt-out is disabled.
    /// </summary>
    /// <returns>A task that completes when the assertion has been verified.</returns>
    [Fact]
    public async Task AuthorizeSubscriptionAppliesDefaultPolicyWhenAllowAnonymousOptOutDisabled()
    {
        // Arrange
        ProjectionAuthorizationMetadata metadata = new(null, null, null, false, true);
        AuthorizationDependencies dependencies = CreateAuthorizationDependencies(
            GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints,
            metadata,
            AuthorizationResult.Success(),
            "generated-default",
            false);
        using InletHub hub = CreateHub(dependencies);

        // Act
        await InvokeAuthorizeSubscriptionAsync(hub);

        // Assert
        await dependencies.AuthorizationPolicyProvider.Received(1).GetPolicyAsync("generated-default");
        await dependencies.AuthorizationService.Received(1)
            .AuthorizeAsync(
                Arg.Any<ClaimsPrincipal>(),
                Arg.Any<object?>(),
                Arg.Any<IEnumerable<IAuthorizationRequirement>>());
    }

    /// <summary>
    ///     Authentication schemes should be enforced for projection metadata authorization.
    /// </summary>
    /// <returns>A task that completes when the assertion has been verified.</returns>
    [Fact]
    public async Task AuthorizeSubscriptionDeniesWhenAuthenticationSchemeDoesNotMatch()
    {
        // Arrange
        ProjectionAuthorizationMetadata metadata = new(null, null, "Bearer", true, false);
        AuthorizationDependencies dependencies = CreateAuthorizationDependencies(
            GeneratedApiAuthorizationMode.Disabled,
            metadata,
            AuthorizationResult.Success());
        using InletHub hub = CreateHub(dependencies);

        // Act
        HubException exception = await Assert.ThrowsAsync<HubException>(() => InvokeAuthorizeSubscriptionAsync(hub));

        // Assert
        Assert.Equal(InletHubConstants.SubscriptionDeniedMessage, exception.Message);
        await dependencies.AuthorizationService.DidNotReceive()
            .AuthorizeAsync(
                Arg.Any<ClaimsPrincipal>(),
                Arg.Any<object?>(),
                Arg.Any<IEnumerable<IAuthorizationRequirement>>());
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
        AuthorizationDependencies dependencies = CreateAuthorizationDependencies(
            GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints,
            metadata,
            AuthorizationResult.Failed(),
            "generated-default");
        using InletHub hub = CreateHub(dependencies);

        // Act
        await InvokeAuthorizeSubscriptionAsync(hub);

        // Assert
        await dependencies.AuthorizationPolicyProvider.DidNotReceive().GetPolicyAsync(Arg.Any<string>());
        await dependencies.AuthorizationService.DidNotReceive()
            .AuthorizeAsync(
                Arg.Any<ClaimsPrincipal>(),
                Arg.Any<object?>(),
                Arg.Any<IEnumerable<IAuthorizationRequirement>>());
    }

    /// <summary>
    ///     Conflicting metadata should follow ASP.NET precedence and skip authorization when allow-anonymous opt-out is
    ///     enabled.
    /// </summary>
    /// <returns>A task that completes when the assertion has been verified.</returns>
    [Fact]
    public async Task AuthorizeSubscriptionSkipsWhenMetadataConflictsAndAllowAnonymousOptOutEnabled()
    {
        // Arrange
        ProjectionAuthorizationMetadata metadata = new("projection.read", null, null, true, true);
        AuthorizationDependencies dependencies = CreateAuthorizationDependencies(
            GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints,
            metadata,
            AuthorizationResult.Success(),
            "generated-default");
        using InletHub hub = CreateHub(dependencies);

        // Act
        await InvokeAuthorizeSubscriptionAsync(hub);

        // Assert
        await dependencies.AuthorizationPolicyProvider.DidNotReceive().GetPolicyAsync(Arg.Any<string>());
        await dependencies.AuthorizationService.DidNotReceive()
            .AuthorizeAsync(
                Arg.Any<ClaimsPrincipal>(),
                Arg.Any<object?>(),
                Arg.Any<IEnumerable<IAuthorizationRequirement>>());
    }

    /// <summary>
    ///     Mode disabled with no metadata should skip authorization checks.
    /// </summary>
    /// <returns>A task that completes when the assertion has been verified.</returns>
    [Fact]
    public async Task AuthorizeSubscriptionSkipsWhenModeDisabledAndNoMetadata()
    {
        // Arrange
        AuthorizationDependencies dependencies = CreateAuthorizationDependencies(
            GeneratedApiAuthorizationMode.Disabled,
            null,
            AuthorizationResult.Success());
        using InletHub hub = CreateHub(dependencies);

        // Act
        await InvokeAuthorizeSubscriptionAsync(hub);

        // Assert
        await dependencies.AuthorizationPolicyProvider.DidNotReceive().GetPolicyAsync(Arg.Any<string>());
        await dependencies.AuthorizationService.DidNotReceive()
            .AuthorizeAsync(
                Arg.Any<ClaimsPrincipal>(),
                Arg.Any<object?>(),
                Arg.Any<IEnumerable<IAuthorizationRequirement>>());
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
        AuthorizationDependencies dependencies = CreateAuthorizationDependencies(
            GeneratedApiAuthorizationMode.Disabled,
            metadata,
            AuthorizationResult.Failed());
        using InletHub hub = CreateHub(dependencies);

        // Act
        HubException exception = await Assert.ThrowsAsync<HubException>(() => InvokeAuthorizeSubscriptionAsync(hub));

        // Assert
        Assert.Equal(InletHubConstants.SubscriptionDeniedMessage, exception.Message);
    }

    /// <summary>
    ///     Force mode with no projection metadata should deny when default policy evaluation fails.
    /// </summary>
    /// <returns>A task that completes when the assertion has been verified.</returns>
    [Fact]
    public async Task AuthorizeSubscriptionThrowsWhenForceModeDefaultPolicyFails()
    {
        // Arrange
        AuthorizationDependencies dependencies = CreateAuthorizationDependencies(
            GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints,
            null,
            AuthorizationResult.Failed(),
            "generated-default");
        using InletHub hub = CreateHub(dependencies);

        // Act
        HubException exception = await Assert.ThrowsAsync<HubException>(() => InvokeAuthorizeSubscriptionAsync(hub));

        // Assert
        Assert.Equal(InletHubConstants.SubscriptionDeniedMessage, exception.Message);
        await dependencies.AuthorizationPolicyProvider.Received(1).GetPolicyAsync("generated-default");
    }

    /// <summary>
    ///     Conflicting metadata should evaluate authorize metadata when allow-anonymous opt-out is disabled.
    /// </summary>
    /// <returns>A task that completes when the assertion has been verified.</returns>
    [Fact]
    public async Task AuthorizeSubscriptionUsesAuthorizeWhenMetadataConflictsAndAllowAnonymousOptOutDisabled()
    {
        // Arrange
        ProjectionAuthorizationMetadata metadata = new("projection.read", null, null, true, true);
        AuthorizationDependencies dependencies = CreateAuthorizationDependencies(
            GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints,
            metadata,
            AuthorizationResult.Success(),
            "generated-default",
            false);
        using InletHub hub = CreateHub(dependencies);

        // Act
        await InvokeAuthorizeSubscriptionAsync(hub);

        // Assert
        await dependencies.AuthorizationPolicyProvider.Received(1).GetPolicyAsync("projection.read");
        await dependencies.AuthorizationPolicyProvider.DidNotReceive().GetPolicyAsync("generated-default");
        await dependencies.AuthorizationService.Received(1)
            .AuthorizeAsync(
                Arg.Any<ClaimsPrincipal>(),
                Arg.Any<object?>(),
                Arg.Any<IEnumerable<IAuthorizationRequirement>>());
    }

    /// <summary>
    ///     Force mode with no metadata should use default policy and allow when authorized.
    /// </summary>
    /// <returns>A task that completes when the assertion has been verified.</returns>
    [Fact]
    public async Task AuthorizeSubscriptionUsesDefaultPolicyWhenForceModeEnabled()
    {
        // Arrange
        AuthorizationDependencies dependencies = CreateAuthorizationDependencies(
            GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints,
            null,
            AuthorizationResult.Success(),
            "generated-default");
        using InletHub hub = CreateHub(dependencies);

        // Act
        await InvokeAuthorizeSubscriptionAsync(hub);

        // Assert
        await dependencies.AuthorizationPolicyProvider.Received(1).GetPolicyAsync("generated-default");
        await dependencies.AuthorizationService.Received(1)
            .AuthorizeAsync(
                Arg.Any<ClaimsPrincipal>(),
                Arg.Any<object?>(),
                Arg.Any<IEnumerable<IAuthorizationRequirement>>());
    }

    /// <summary>
    ///     GenerateAuthorization metadata should use the projection policy when force mode is enabled.
    /// </summary>
    /// <returns>A task that completes when the assertion has been verified.</returns>
    [Fact]
    public async Task AuthorizeSubscriptionUsesProjectionPolicyInsteadOfDefaultWhenMetadataPresent()
    {
        // Arrange
        ProjectionAuthorizationMetadata metadata = new("projection.read", null, null, true, false);
        AuthorizationDependencies dependencies = CreateAuthorizationDependencies(
            GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints,
            metadata,
            AuthorizationResult.Success(),
            "generated-default");
        using InletHub hub = CreateHub(dependencies);

        // Act
        await InvokeAuthorizeSubscriptionAsync(hub);

        // Assert
        await dependencies.AuthorizationPolicyProvider.Received(1).GetPolicyAsync("projection.read");
        await dependencies.AuthorizationPolicyProvider.DidNotReceive().GetPolicyAsync("generated-default");
    }

    /// <summary>
    ///     GenerateAuthorization metadata without explicit policy should use the configured ASP.NET default policy.
    /// </summary>
    /// <returns>A task that completes when the assertion has been verified.</returns>
    [Fact]
    public async Task AuthorizeSubscriptionUsesAspNetDefaultPolicyWhenMetadataHasAuthorizeWithoutPolicy()
    {
        // Arrange
        ProjectionAuthorizationMetadata metadata = new(null, null, null, true, false);
        AuthorizationDependencies dependencies = CreateAuthorizationDependencies(
            GeneratedApiAuthorizationMode.Disabled,
            metadata,
            AuthorizationResult.Success());
        using InletHub hub = CreateHub(dependencies);

        // Act
        await InvokeAuthorizeSubscriptionAsync(hub);

        // Assert
        await dependencies.AuthorizationPolicyProvider.Received(1).GetDefaultPolicyAsync();
        await dependencies.AuthorizationService.Received(1)
            .AuthorizeAsync(
                Arg.Any<ClaimsPrincipal>(),
                Arg.Any<object?>(),
                Arg.Any<IEnumerable<IAuthorizationRequirement>>());
    }
}