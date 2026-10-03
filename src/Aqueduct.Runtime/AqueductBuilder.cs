using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Hosting.Abstractions;

using Orleans.Hosting;


namespace Mississippi.Aqueduct.Runtime;

/// <summary>
///     Configures the Aqueduct backplane within one runtime composition scope.
/// </summary>
/// <remarks>Public for the AddAqueduct callback; captured scopes cannot be configured after they close.</remarks>
public sealed class AqueductBuilder
{
    /// <summary>Initializes a new instance of the <see cref="AqueductBuilder" /> class.</summary>
    internal AqueductBuilder()
    {
    }

    /// <summary>Gets or sets the positive multiplier applied to the shared gateway heartbeat interval.</summary>
    public int DeadServerTimeoutMultiplier
    {
        get => DeadServerTimeoutOverride ?? Options.DeadServerTimeoutMultiplier;
        set
        {
            ThrowIfClosed();
            DeadServerTimeoutOverride = value;
        }
    }

    /// <summary>Gets or sets the positive gateway heartbeat interval, in minutes, used for runtime cleanup.</summary>
    public int HeartbeatIntervalMinutes
    {
        get => HeartbeatIntervalOverride ?? Options.HeartbeatIntervalMinutes;
        set
        {
            ThrowIfClosed();
            HeartbeatIntervalOverride = value;
        }
    }

    /// <summary>Gets or sets the namespace used for server-targeted messages.</summary>
    public string ServerStreamNamespace
    {
        get => Options.ServerStreamNamespace;
        set
        {
            ThrowIfClosed();
            Options.ServerStreamNamespace = value;
        }
    }

    /// <summary>Gets or sets the Orleans stream provider used for SignalR delivery.</summary>
    public string StreamProviderName
    {
        get => Options.StreamProviderName;
        set
        {
            ThrowIfClosed();
            Options.StreamProviderName = value;
        }
    }

    private int? DeadServerTimeoutOverride { get; set; }

    private int? HeartbeatIntervalOverride { get; set; }

    private bool IsClosed { get; set; }

    private AqueductOptions Options { get; } = new();

    private bool ShouldUseMemoryStreams { get; set; }

    private static void ValidateName(
        string value,
        string name,
        string code,
        List<BuilderDiagnostic> diagnostics
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            diagnostics.Add(new(code, $"Aqueduct {name} cannot be empty.", $"Set {name} to a nonempty value."));
        }
    }

    /// <summary>
    ///     Configures development memory streams using the final selected provider and the Orleans PubSubStore.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    public AqueductBuilder UseMemoryStreams()
    {
        ThrowIfClosed();
        ShouldUseMemoryStreams = true;
        return this;
    }

    /// <summary>Chooses a provider name and enables development memory streams.</summary>
    /// <param name="streamProviderName">The provider name to select.</param>
    /// <returns>This builder for chaining.</returns>
    public AqueductBuilder UseMemoryStreams(
        string streamProviderName
    )
    {
        StreamProviderName = streamProviderName;
        return UseMemoryStreams();
    }

    /// <summary>Returns configuration diagnostics without registering services.</summary>
    /// <returns>The current validation diagnostics.</returns>
    public IReadOnlyList<BuilderDiagnostic> Validate()
    {
        if (IsClosed)
        {
            return
            [
                new(
                    AqueductBuilderDiagnosticCodes.ConfigurationScopeClosed,
                    "The Aqueduct configuration scope has closed.",
                    "Configure Aqueduct inside a fresh AddAqueduct(...) callback."),
            ];
        }

        List<BuilderDiagnostic> diagnostics = [];
        if (HeartbeatIntervalMinutes <= 0)
        {
            diagnostics.Add(
                new(
                    AqueductBuilderDiagnosticCodes.HeartbeatIntervalInvalid,
                    "Aqueduct HeartbeatIntervalMinutes must be positive.",
                    "Use the same positive heartbeat interval on gateway and runtime hosts."));
        }

        if ((DeadServerTimeoutMultiplier <= 0) ||
            (((double)HeartbeatIntervalMinutes * DeadServerTimeoutMultiplier) > TimeSpan.MaxValue.TotalMinutes))
        {
            diagnostics.Add(
                new(
                    AqueductBuilderDiagnosticCodes.DeadServerTimeoutInvalid,
                    "Aqueduct heartbeat timeout must be positive and fit within TimeSpan.",
                    "Choose a positive DeadServerTimeoutMultiplier and a supported heartbeat duration."));
        }

        ValidateName(
            StreamProviderName,
            nameof(StreamProviderName),
            AqueductBuilderDiagnosticCodes.StreamProviderRequired,
            diagnostics);
        ValidateName(
            ServerStreamNamespace,
            nameof(ServerStreamNamespace),
            AqueductBuilderDiagnosticCodes.ServerNamespaceRequired,
            diagnostics);
        return diagnostics;
    }

    /// <summary>Validates and applies the completed settings to staged native services.</summary>
    /// <param name="silo">The staged silo supplied by the owning runtime.</param>
    internal void Apply(
        ISiloBuilder silo
    )
    {
        IReadOnlyList<BuilderDiagnostic> diagnostics = Validate();
        if (diagnostics.Count > 0)
        {
            throw new BuilderValidationException(diagnostics);
        }

        AqueductOptions snapshot = new()
        {
            StreamProviderName = StreamProviderName,
            ServerStreamNamespace = ServerStreamNamespace,
        };
        if (ShouldUseMemoryStreams)
        {
            silo.AddMemoryStreams(snapshot.StreamProviderName);
            silo.AddMemoryGrainStorage("PubSubStore");
        }

        int? heartbeatInterval = HeartbeatIntervalOverride;
        int? timeoutMultiplier = DeadServerTimeoutOverride;
        silo.Services.AddOptions<AqueductOptions>()
            .Configure(options =>
            {
                options.StreamProviderName = snapshot.StreamProviderName;
                options.ServerStreamNamespace = snapshot.ServerStreamNamespace;
                if (heartbeatInterval.HasValue)
                {
                    options.HeartbeatIntervalMinutes = heartbeatInterval.Value;
                }

                if (timeoutMultiplier.HasValue)
                {
                    options.DeadServerTimeoutMultiplier = timeoutMultiplier.Value;
                }
            })
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.StreamProviderName) &&
                           !string.IsNullOrWhiteSpace(options.ServerStreamNamespace) &&
                           (options.HeartbeatIntervalMinutes > 0) &&
                           (options.DeadServerTimeoutMultiplier > 0) &&
                           (((double)options.HeartbeatIntervalMinutes * options.DeadServerTimeoutMultiplier) <=
                            TimeSpan.MaxValue.TotalMinutes),
                "Aqueduct requires nonempty stream names and positive heartbeat timing within TimeSpan range.")
            .ValidateOnStart();
        silo.Services.TryAddSingleton<IAqueductGrainFactory, AqueductGrainFactory>();
        silo.Services.TryAddSingleton<SignalRServerLivenessCache>();
    }

    /// <summary>Closes configuration after its callback completes or fails.</summary>
    internal void Close() => IsClosed = true;

    private void ThrowIfClosed()
    {
        if (IsClosed)
        {
            throw new BuilderValidationException(Validate());
        }
    }
}