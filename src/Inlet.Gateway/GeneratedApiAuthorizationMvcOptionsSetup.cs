using System;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;


namespace Mississippi.Inlet.Gateway;

/// <summary>
///     Configures MVC options to apply generated API authorization conventions.
/// </summary>
internal sealed class GeneratedApiAuthorizationMvcOptionsSetup : IConfigureOptions<MvcOptions>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="GeneratedApiAuthorizationMvcOptionsSetup" /> class.
    /// </summary>
    /// <param name="inletServerOptions">The resolved Inlet server options.</param>
    public GeneratedApiAuthorizationMvcOptionsSetup(
        IOptions<InletServerOptions> inletServerOptions
    )
    {
        ArgumentNullException.ThrowIfNull(inletServerOptions);
        AuthorizationOptions = inletServerOptions.Value.GeneratedApiAuthorization;
    }

    private GeneratedApiAuthorizationOptions AuthorizationOptions { get; }

    /// <inheritdoc />
    public void Configure(
        MvcOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Conventions.Add(new GeneratedApiAuthorizationConvention(AuthorizationOptions));
    }
}