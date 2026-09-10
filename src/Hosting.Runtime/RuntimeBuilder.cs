using System;
using System.Collections.Generic;
using System.ComponentModel;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Hosting.Abstractions;
using Mississippi.Hosting.Runtime.Abstractions;

using Orleans.Hosting;


namespace Mississippi.Hosting.Runtime;

/// <summary>
///     Composes Mississippi runtime services and native Orleans configuration before attachment.
/// </summary>
/// <remarks>Public so applications and generated extensions share one runtime composition root.</remarks>
public sealed class RuntimeBuilder : IRuntimeBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RuntimeBuilder" /> class.
    /// </summary>
    /// <param name="siloBuilder">The owning silo builder.</param>
    /// <param name="services">The staged service collection.</param>
    internal RuntimeBuilder(
        ISiloBuilder siloBuilder,
        ServiceCollection services
    )
    {
        TargetSiloBuilder = siloBuilder;
        StagedServices = services;
    }

    /// <inheritdoc />
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public IServiceCollection Services => StagedServices;

    /// <summary>
    ///     Gets a value indicating whether native application has begun.
    /// </summary>
    internal bool IsSiloApplied { get; private set; }

    private bool HasSiloApplicationFailed { get; set; }

    private bool IsAttached { get; set; }

    private List<Action<ISiloBuilder>> SiloConfigurations { get; } = [];

    private ServiceCollection StagedServices { get; }

    private ISiloBuilder TargetSiloBuilder { get; }

    /// <inheritdoc />
    public IRuntimeBuilder ApplyToSilo(
        ISiloBuilder siloBuilder
    )
    {
        ArgumentNullException.ThrowIfNull(siloBuilder);
        ThrowIfInvalid();
        if (!ReferenceEquals(siloBuilder.Services, TargetSiloBuilder.Services))
        {
            throw new BuilderValidationException(
            [
                new(
                    RuntimeBuilderDiagnosticCodes.SiloHostMismatch,
                    "The runtime configuration belongs to a different silo host.",
                    "Pass the silo receiving this UseMississippi(...) call to ApplyToSilo(...)."),
            ]);
        }

        if (IsSiloApplied)
        {
            throw new BuilderValidationException(
            [
                new(
                    RuntimeBuilderDiagnosticCodes.DuplicateSiloApplication,
                    "Runtime configuration has already been applied to the silo.",
                    "Call ApplyToSilo(...) once after configuration, or let UseMississippi(...) apply it automatically."),
            ]);
        }

        IsSiloApplied = true;
        StagedSiloBuilder stagedSilo = new(StagedServices, TargetSiloBuilder.Configuration);
        bool appliedSuccessfully = false;
        try
        {
            foreach (Action<ISiloBuilder> configure in SiloConfigurations)
            {
                configure(stagedSilo);
            }

            appliedSuccessfully = true;
        }
        finally
        {
            HasSiloApplicationFailed = !appliedSuccessfully;
            SiloConfigurations.Clear();
        }

        return this;
    }

    /// <inheritdoc />
    public IRuntimeBuilder ConfigureSilo(
        Action<ISiloBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        ThrowIfInvalid();
        if (IsSiloApplied)
        {
            throw new BuilderValidationException(
            [
                new(
                    RuntimeBuilderDiagnosticCodes.SiloConfigurationAlreadyApplied,
                    "Native silo configuration has already been applied.",
                    "Queue all ConfigureSilo(...) callbacks before calling ApplyToSilo(...)."),
            ]);
        }

        SiloConfigurations.Add(configure);
        return this;
    }

    /// <inheritdoc />
    public IReadOnlyList<BuilderDiagnostic> Validate()
    {
        if (StagedServices.IsReadOnly)
        {
            return IsAttached
                ?
                [
                    new(
                        BuilderDiagnosticCodes.BuilderAlreadyAttached,
                        "The runtime builder has already been attached.",
                        "Configure runtime features inside UseMississippi(...)."),
                ]
                :
                [
                    new(
                        BuilderDiagnosticCodes.ConfigurationScopeClosed,
                        "The runtime composition scope closed without attachment.",
                        "Retry with a new UseMississippi(...) callback."),
                ];
        }

        return HasSiloApplicationFailed
            ?
            [
                new(
                    RuntimeBuilderDiagnosticCodes.SiloConfigurationFailed,
                    "A native silo configuration callback did not complete.",
                    "Correct the callback and retry with a new UseMississippi(...) scope."),
            ]
            : [];
    }

    /// <summary>
    ///     Closes an unsuccessful runtime composition.
    /// </summary>
    internal void Abort()
    {
        IsAttached = false;
        SiloConfigurations.Clear();
        StagedServices.MakeReadOnly();
    }

    /// <summary>
    ///     Closes a successfully applied runtime composition.
    /// </summary>
    internal void Complete()
    {
        IsAttached = true;
        StagedServices.MakeReadOnly();
    }

    private void ThrowIfInvalid()
    {
        IReadOnlyList<BuilderDiagnostic> diagnostics = Validate();
        if (diagnostics.Count > 0)
        {
            throw new BuilderValidationException(diagnostics);
        }
    }
}