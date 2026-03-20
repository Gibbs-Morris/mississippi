using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Diagnostics;

using Orleans.Hosting;


namespace Mississippi.Sdk.Runtime;

/// <summary>
///     Extension methods for attaching Mississippi runtime composition to an Orleans silo.
/// </summary>
public static class SiloBuilderExtensions
{
    /// <summary>
    ///     Attaches Mississippi runtime composition to the Orleans silo builder.
    /// </summary>
    /// <param name="siloBuilder">The Orleans silo builder.</param>
    /// <param name="configure">The runtime configuration callback.</param>
    /// <returns>The same silo builder for chaining.</returns>
    /// <exception cref="MississippiBuilderException">
    ///     Thrown with <see cref="MississippiDiagnosticCodes.RuntimeDuplicateAttach" />
    ///     if <c>UseMississippi</c> has already been called on this silo builder.
    /// </exception>
    public static ISiloBuilder UseMississippi(
        this ISiloBuilder siloBuilder,
        Action<MississippiRuntimeBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(siloBuilder);
        ArgumentNullException.ThrowIfNull(configure);
        if (siloBuilder.Services.Any(d => d.ServiceType == typeof(MississippiRuntimeAttachMarker)))
        {
            throw new MississippiBuilderException(
                MississippiDiagnosticCodes.RuntimeDuplicateAttach,
                "UseMississippi was called more than once on the same ISiloBuilder.");
        }

        MississippiRuntimeBuilder runtimeBuilder = new(siloBuilder);
        configure(runtimeBuilder);
        runtimeBuilder.Validate();
        siloBuilder.Services.AddSingleton(MississippiRuntimeAttachMarker.Instance);
        return siloBuilder;
    }

    private sealed class MississippiRuntimeAttachMarker
    {
        public static MississippiRuntimeAttachMarker Instance { get; } = new();
    }
}