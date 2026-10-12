using System;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


namespace Mississippi.Inlet.Gateway.Authentication;

/// <summary>
///     Refreshes framework authentication caches while preserving the host's scoped services.
/// </summary>
internal sealed class SubscriptionAuthenticationServices : IKeyedServiceProvider
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SubscriptionAuthenticationServices" /> class.
    /// </summary>
    /// <param name="services">The host's connection services.</param>
    /// <param name="authentication">The framework authentication service whose configuration is retained.</param>
    /// <param name="schemes">The handler provider's scheme registry.</param>
    /// <param name="context">The isolated subscription decision context.</param>
    /// <param name="principalProvider">The established caller without eagerly caching its user.</param>
    public SubscriptionAuthenticationServices(
        IServiceProvider services,
        AuthenticationService authentication,
        IAuthenticationSchemeProvider schemes,
        HttpContext context,
        Func<ClaimsPrincipal?> principalProvider
    )
    {
        Services = services;
        Handlers = new(schemes);
        AuthenticationService frameworkAuthentication = (AuthenticationService)ActivatorUtilities.CreateInstance(
            services,
            authentication.GetType(),
            authentication.Schemes,
            Handlers,
            authentication.Transform,
            Options.Create(authentication.Options));
        Authentication = new(frameworkAuthentication, context, principalProvider);
    }

    private ConnectionSubscriptionAuthenticationService Authentication { get; }

    private AuthenticationHandlerProvider Handlers { get; }

    private IServiceProvider Services { get; }

    /// <inheritdoc />
    public object? GetKeyedService(
        Type serviceType,
        object? serviceKey
    ) =>
        Services.GetKeyedService(serviceType, serviceKey);

    /// <inheritdoc />
    public object GetRequiredKeyedService(
        Type serviceType,
        object? serviceKey
    ) =>
        Services.GetRequiredKeyedService(serviceType, serviceKey);

    /// <inheritdoc />
    public object? GetService(
        Type serviceType
    )
    {
        if (serviceType == typeof(IAuthenticationService))
        {
            return Authentication;
        }

        return serviceType == typeof(IAuthenticationHandlerProvider) ? Handlers : Services.GetService(serviceType);
    }

    /// <summary>
    ///     Checks whether registered schemes can be replayed for the established caller.
    /// </summary>
    /// <param name="policy">The subscription policy.</param>
    /// <returns>Whether selected schemes have verified results for this caller.</returns>
    internal Task<bool> CanUseEstablishedAuthenticationAsync(
        AuthorizationPolicy policy
    ) =>
        Authentication.CanUseEstablishedAuthenticationAsync(policy);
}