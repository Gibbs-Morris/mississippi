using System;
using System.Collections.Generic;
using System.ComponentModel;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Hosting.Abstractions;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Core;


namespace Mississippi.Hosting.Client;

/// <summary>
///     Composes Mississippi client features before terminal host attachment.
/// </summary>
/// <remarks>Public so applications and generated extensions can compose client features fluently.</remarks>
public sealed class ClientBuilder : IMississippiBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ClientBuilder" /> class over staged registrations.
    /// </summary>
    /// <param name="services">The staged service collection.</param>
    internal ClientBuilder(
        ServiceCollection services
    ) =>
        StagedServices = services;

    /// <inheritdoc />
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public IServiceCollection Services => StagedServices;

    private bool IsAttached { get; set; }

    private IReservoirBuilder? ReservoirBuilder { get; set; }

    private ServiceCollection StagedServices { get; }

    /// <summary>
    ///     Composes Reservoir features within the Mississippi client flow.
    /// </summary>
    /// <param name="configure">The Reservoir composition callback.</param>
    /// <returns>This builder for chaining.</returns>
    public ClientBuilder Reservoir(
        Action<IReservoirBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        IReadOnlyList<BuilderDiagnostic> diagnostics = Validate();
        if (diagnostics.Count > 0)
        {
            throw new BuilderValidationException(diagnostics);
        }

        ReservoirBuilder ??= Services.AddReservoir();
        configure(ReservoirBuilder);
        return this;
    }

    /// <inheritdoc />
    public IReadOnlyList<BuilderDiagnostic> Validate()
    {
        if (!StagedServices.IsReadOnly)
        {
            return [];
        }

        return IsAttached
            ?
            [
                new(
                    BuilderDiagnosticCodes.BuilderAlreadyAttached,
                    "The client builder has already been attached.",
                    "Configure all client features inside UseMississippi(...)."),
            ]
            :
            [
                new(
                    BuilderDiagnosticCodes.ConfigurationScopeClosed,
                    "The client composition scope closed without attachment.",
                    "Start a new UseMississippi(...) callback instead of reusing captured builders."),
            ];
    }

    /// <summary>
    ///     Closes an unsuccessful composition without marking it attached.
    /// </summary>
    internal void Abort()
    {
        IsAttached = false;
        StagedServices.MakeReadOnly();
    }

    /// <summary>
    ///     Prevents further changes after successful composition.
    /// </summary>
    internal void Complete()
    {
        IsAttached = true;
        StagedServices.MakeReadOnly();
    }
}