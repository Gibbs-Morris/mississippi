using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Builders.Abstractions;
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

    /// <summary>
    ///     Validates gateway authorization requirements.
    /// </summary>
    /// <exception cref="BuilderValidationException">Thrown when gateway authorization is not configured.</exception>
    public void EnsureAuthorizationConfigured()
    {
        IReadOnlyList<BuilderDiagnostic> diagnostics = Validate();
        if (diagnostics.Count == 0)
        {
            return;
        }

        throw new BuilderValidationException(
            "Gateway authorization is required before terminal host attachment.",
            diagnostics);
    }

    /// <inheritdoc />
    public IReadOnlyList<BuilderDiagnostic> Validate()
    {
        if (AuthorizationConfigured || AnonymousExplicitlyAllowed)
        {
            return [];
        }

        return
        [
            new(
                "Gateway.AuthorizationNotConfigured",
                "Security",
                "Gateway authorization is not configured.",
                "Call ConfigureAuthorization() or AllowAnonymousExplicitly() before terminal host attachment."),
        ];
    }
}