using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Configuration;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Hosting.Abstractions;
using Mississippi.Hosting.Runtime.Abstractions;


namespace Mississippi.Aqueduct.Runtime;

/// <summary>
///     Composes the Aqueduct backplane through the canonical runtime builder.
/// </summary>
/// <remarks>Public so applications discover Aqueduct from their runtime composition callback.</remarks>
public static class AqueductRuntimeRegistrations
{
    private static ConditionalWeakTable<IRuntimeBuilder, object> Registrations { get; } = new();

    /// <summary>Queues one validated Aqueduct configuration for this runtime.</summary>
    /// <param name="builder">The owning runtime builder.</param>
    /// <param name="configure">Optional synchronous configuration of the nested Aqueduct scope.</param>
    /// <returns>The runtime builder for chaining.</returns>
    public static IRuntimeBuilder AddAqueduct(
        this IRuntimeBuilder builder,
        Action<AqueductBuilder>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        IReadOnlyList<BuilderDiagnostic> diagnostics = builder.Validate();
        if (diagnostics.Count > 0)
        {
            throw new BuilderValidationException(diagnostics);
        }

        if (!Registrations.TryAdd(builder, new()))
        {
            throw new BuilderValidationException(
            [
                new(
                    AqueductBuilderDiagnosticCodes.DuplicateComposition,
                    "Aqueduct has already been configured for this runtime.",
                    "Combine Aqueduct settings in one AddAqueduct(...) callback."),
            ]);
        }

        try
        {
            builder.ConfigureSilo(silo =>
            {
                AqueductBuilder aqueduct = new();
                try
                {
                    configure?.Invoke(aqueduct);
                    aqueduct.Apply(silo);
                }
                finally
                {
                    aqueduct.Close();
                }
            });
            return builder;
        }
        catch
        {
            Registrations.Remove(builder);
            throw;
        }
    }

    /// <summary>Configures Aqueduct from a configuration section using the option property names.</summary>
    /// <param name="builder">The owning runtime builder.</param>
    /// <param name="configuration">The Aqueduct configuration section.</param>
    /// <returns>The runtime builder for chaining.</returns>
    public static IRuntimeBuilder AddAqueduct(
        this IRuntimeBuilder builder,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return builder.AddAqueduct(aqueduct =>
        {
            aqueduct.StreamProviderName = configuration[nameof(AqueductOptions.StreamProviderName)] ??
                                          aqueduct.StreamProviderName;
            aqueduct.ServerStreamNamespace = configuration[nameof(AqueductOptions.ServerStreamNamespace)] ??
                                             aqueduct.ServerStreamNamespace;
            aqueduct.AllClientsStreamNamespace = configuration[nameof(AqueductOptions.AllClientsStreamNamespace)] ??
                                                 aqueduct.AllClientsStreamNamespace;
            aqueduct.HeartbeatIntervalMinutes = ReadInteger(
                configuration,
                nameof(AqueductOptions.HeartbeatIntervalMinutes),
                aqueduct.HeartbeatIntervalMinutes);
            aqueduct.DeadServerTimeoutMultiplier = ReadInteger(
                configuration,
                nameof(AqueductOptions.DeadServerTimeoutMultiplier),
                aqueduct.DeadServerTimeoutMultiplier);
        });
    }

    /// <summary>Configures Aqueduct with explicit stream and heartbeat settings.</summary>
    /// <param name="builder">The owning runtime builder.</param>
    /// <param name="streamProviderName">The existing stream provider.</param>
    /// <param name="serverStreamNamespace">The namespace for server-targeted messages.</param>
    /// <param name="allClientsStreamNamespace">The namespace for broadcasts.</param>
    /// <param name="heartbeatIntervalMinutes">The positive heartbeat interval in minutes.</param>
    /// <param name="deadServerTimeoutMultiplier">The positive dead-server timeout multiplier.</param>
    /// <returns>The runtime builder for chaining.</returns>
    public static IRuntimeBuilder AddAqueduct(
        this IRuntimeBuilder builder,
        string streamProviderName,
        string serverStreamNamespace = AqueductStreamDefaults.ServerStreamNamespace,
        string allClientsStreamNamespace = AqueductStreamDefaults.AllClientsStreamNamespace,
        int heartbeatIntervalMinutes = 1,
        int deadServerTimeoutMultiplier = 3
    ) =>
        builder.AddAqueduct(aqueduct =>
        {
            aqueduct.StreamProviderName = streamProviderName;
            aqueduct.ServerStreamNamespace = serverStreamNamespace;
            aqueduct.AllClientsStreamNamespace = allClientsStreamNamespace;
            aqueduct.HeartbeatIntervalMinutes = heartbeatIntervalMinutes;
            aqueduct.DeadServerTimeoutMultiplier = deadServerTimeoutMultiplier;
        });

    private static int ReadInteger(
        IConfiguration configuration,
        string key,
        int fallback
    )
    {
        string? value = configuration[key];
        if (value is null)
        {
            return fallback;
        }

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : 0;
    }
}