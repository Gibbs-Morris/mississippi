using System;
using System.Collections.Generic;
using System.ComponentModel;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Client;


namespace Mississippi.Hosting.Client;

/// <summary>
///     Provides the top-level client-role builder for Mississippi applications.
/// </summary>
public sealed class MississippiClientBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MississippiClientBuilder" /> class.
    /// </summary>
    /// <param name="builder">The host builder used for client startup.</param>
    public MississippiClientBuilder(
        WebAssemblyHostBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        Builder = builder;
        Services = builder.Services;
    }

    /// <summary>
    ///     Gets the underlying service collection for advanced composition scenarios.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public IServiceCollection Services { get; }

    private WebAssemblyHostBuilder Builder { get; }

    private HashSet<string> RegisteredDomains { get; } = new(StringComparer.Ordinal);

    private IReservoirBuilder? ReservoirBuilder { get; set; }

    /// <summary>
    ///     Composes Reservoir registrations within the Mississippi client builder flow.
    /// </summary>
    /// <param name="configure">The Reservoir composition callback.</param>
    /// <returns>The Mississippi client builder for chaining.</returns>
    public MississippiClientBuilder Reservoir(
        Action<IReservoirBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(GetOrCreateReservoirBuilder());
        return this;
    }

    /// <summary>
    ///     Registers generated client features for a single domain exactly once per builder.
    /// </summary>
    /// <param name="domainName">The normalized domain name being attached.</param>
    /// <param name="registrationMethodName">The generated registration method name.</param>
    /// <param name="configure">The client feature registration callback.</param>
    /// <returns>The Mississippi client builder for chaining.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public MississippiClientBuilder RegisterDomainFeatures(
        string domainName,
        string registrationMethodName,
        Action<IReservoirBuilder> configure
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domainName);
        ArgumentException.ThrowIfNullOrWhiteSpace(registrationMethodName);
        ArgumentNullException.ThrowIfNull(configure);
        if (!RegisteredDomains.Add(domainName))
        {
            throw new InvalidOperationException(
                $"Mississippi client domain composition for '{domainName}' can only be attached once per builder. Remove the duplicate {registrationMethodName}(...) call and keep each domain on a single client builder path.");
        }

        configure(GetOrCreateReservoirBuilder());
        return this;
    }

    private IReservoirBuilder GetOrCreateReservoirBuilder()
    {
        ReservoirBuilder ??= Builder.AddReservoir();
        return ReservoirBuilder;
    }
}