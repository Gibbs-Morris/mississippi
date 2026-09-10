using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Brooks.Abstractions.Factory;
using Mississippi.Brooks.Abstractions.Streaming;
using Mississippi.Brooks.Runtime.Factory;
using Mississippi.Brooks.Runtime.Reader;
using Mississippi.Hosting.Abstractions;
using Mississippi.Hosting.Runtime.Abstractions;


namespace Mississippi.Brooks.Runtime;

/// <summary>
///     Composes Brooks runtime services and options through the runtime builder.
/// </summary>
/// <remarks>Public so consumers register Brooks through the canonical runtime composition flow.</remarks>
public static class BrooksRuntimeRegistrations
{
    /// <summary>
    ///     Adds Brooks factories, stream identity support, and runtime options together.
    /// </summary>
    /// <param name="builder">The runtime composition builder.</param>
    /// <param name="configureOptions">Optional configuration of the host-owned stream provider name.</param>
    /// <returns>The runtime builder for chaining.</returns>
    /// <remarks>The host supplies Orleans streams and storage; this method does not create external infrastructure.</remarks>
    public static IRuntimeBuilder AddEventSourcing(
        this IRuntimeBuilder builder,
        Action<BrookProviderOptions>? configureOptions = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        IReadOnlyList<BuilderDiagnostic> diagnostics = builder.Validate();
        if (diagnostics.Count > 0)
        {
            throw new BuilderValidationException(diagnostics);
        }

        IServiceCollection services = builder.Services;
        services.TryAddSingleton<BrookGrainFactory>();
        services.TryAddSingleton<IBrookGrainFactory>(sp => sp.GetRequiredService<BrookGrainFactory>());
        services.TryAddSingleton<IInternalBrookGrainFactory>(sp => sp.GetRequiredService<BrookGrainFactory>());
        services.TryAddSingleton<IStreamIdFactory, StreamIdFactory>();
        services.AddOptions<BrookReaderOptions>();
        services.AddOptions<BrookProviderOptions>();
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        return builder;
    }
}