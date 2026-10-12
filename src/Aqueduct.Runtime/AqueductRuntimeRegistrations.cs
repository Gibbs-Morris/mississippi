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
            int? heartbeatInterval = ReadTimingValue(
                configuration,
                nameof(AqueductOptions.HeartbeatIntervalMinutes),
                AqueductBuilderDiagnosticCodes.HeartbeatIntervalInvalid);
            int? timeoutMultiplier = ReadTimingValue(
                configuration,
                nameof(AqueductOptions.DeadServerTimeoutMultiplier),
                AqueductBuilderDiagnosticCodes.DeadServerTimeoutInvalid);
            if (heartbeatInterval.HasValue)
            {
                aqueduct.HeartbeatIntervalMinutes = heartbeatInterval.Value;
            }

            if (timeoutMultiplier.HasValue)
            {
                aqueduct.DeadServerTimeoutMultiplier = timeoutMultiplier.Value;
            }

            aqueduct.StreamProviderName = configuration[nameof(AqueductOptions.StreamProviderName)] ??
                                          aqueduct.StreamProviderName;
            aqueduct.ServerStreamNamespace = configuration[nameof(AqueductOptions.ServerStreamNamespace)] ??
                                             aqueduct.ServerStreamNamespace;
        });
    }

    /// <summary>Configures Aqueduct with explicit stream and server namespace settings.</summary>
    /// <param name="builder">The owning runtime builder.</param>
    /// <param name="streamProviderName">The existing stream provider.</param>
    /// <param name="serverStreamNamespace">The namespace for server-targeted messages.</param>
    /// <returns>The runtime builder for chaining.</returns>
    public static IRuntimeBuilder AddAqueduct(
        this IRuntimeBuilder builder,
        string streamProviderName,
        string serverStreamNamespace = AqueductStreamDefaults.ServerStreamNamespace
    ) =>
        builder.AddAqueduct(aqueduct =>
        {
            aqueduct.StreamProviderName = streamProviderName;
            aqueduct.ServerStreamNamespace = serverStreamNamespace;
        });

    /// <summary>Reads an optional integer timing setting without introducing a configuration binder dependency.</summary>
    /// <param name="configuration">The shared Aqueduct configuration section.</param>
    /// <param name="name">The option-property name.</param>
    /// <param name="diagnosticCode">The stable diagnostic for an invalid configured value.</param>
    /// <returns>The configured integer, or null when the key is omitted.</returns>
    private static int? ReadTimingValue(
        IConfiguration configuration,
        string name,
        string diagnosticCode
    )
    {
        string? text = configuration[name];
        if (text is null)
        {
            return null;
        }

        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            throw new BuilderValidationException(
            [
                new(diagnosticCode, $"Aqueduct {name} must be an integer.", $"Set {name} to a positive integer."),
            ]);
        }

        return value;
    }
}