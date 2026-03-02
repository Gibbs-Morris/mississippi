using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Builders.Gateway.Abstractions;


namespace Mississippi.Common.Builders.Gateway;

/// <summary>
///     Concrete gateway-host builder implementation.
/// </summary>
public sealed class GatewayBuilder : IGatewayBuilder
{
    private GatewayBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    /// <summary>
    ///     Gets a value indicating whether anonymous gateway mode was explicitly enabled.
    /// </summary>
    public bool AnonymousExplicitlyAllowed { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether gateway authorization was configured.
    /// </summary>
    public bool AuthorizationConfigured { get; private set; }

    /// <inheritdoc />
    public IServiceCollection Services { get; }

    /// <summary>
    ///     Creates a new gateway builder instance.
    /// </summary>
    /// <returns>A new <see cref="GatewayBuilder" /> instance.</returns>
    public static GatewayBuilder Create() => new(new ServiceCollection());

    /// <inheritdoc />
    public IGatewayBuilder AllowAnonymousExplicitly()
    {
        AnonymousExplicitlyAllowed = true;
        return this;
    }

    /// <inheritdoc />
    public IGatewayBuilder ConfigureAuthorization()
    {
        AuthorizationConfigured = true;
        return this;
    }
}