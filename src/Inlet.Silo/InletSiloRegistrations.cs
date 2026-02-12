using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Common.Abstractions.Builders;
using Mississippi.Inlet.Silo.Abstractions;


namespace Mississippi.Inlet.Silo;

/// <summary>
///     Extension methods for registering Inlet Silo services.
/// </summary>
/// <remarks>
///     <para>
///         Use these extensions on Orleans silo hosts. For ASP.NET Core hosts
///         that serve SignalR hubs, use the extensions from <c>Inlet.Server</c>.
///     </para>
/// </remarks>
public static class InletSiloRegistrations
{
    /// <summary>
    ///     Adds Inlet Orleans services to the Mississippi silo builder.
    /// </summary>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method registers the <see cref="IProjectionBrookRegistry" /> as a singleton.
    ///         The registry is populated automatically by source-generated domain registration code
    ///         via <see cref="RegisterProjectionBrookMappings(IMississippiSiloBuilder, Action{IProjectionBrookRegistry}, bool)" />.
    ///     </para>
    /// </remarks>
    public static IMississippiSiloBuilder AddInletSilo(
        this IMississippiSiloBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services =>
            services.TryAddSingleton<IProjectionBrookRegistry, ProjectionBrookRegistry>());
        return builder;
    }

    /// <summary>
    ///     Adds Inlet Orleans services to the Mississippi server builder.
    /// </summary>
    /// <param name="builder">The Mississippi server builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiServerBuilder AddInletSilo(
        this IMississippiServerBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services =>
            services.TryAddSingleton<IProjectionBrookRegistry, ProjectionBrookRegistry>());
        return builder;
    }

    /// <summary>
    ///     Registers projection-to-brook mappings in the <see cref="IProjectionBrookRegistry" />.
    /// </summary>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <param name="configure">
    ///     An action that registers path-to-brook-name mappings on the registry.
    /// </param>
    /// <param name="requireExplicitPaths">
    ///     When <c>true</c>, validates that all projection paths were explicitly defined
    ///     via <c>[GenerateProjectionEndpoints(Path = "...")]</c> and throws if any were auto-derived.
    /// </param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method is called by source-generated domain registration code.
    ///         It replaces the empty registry registered by <see cref="AddInletSilo(IMississippiSiloBuilder)" />
    ///         with a populated instance containing all projection-to-brook mappings.
    ///     </para>
    /// </remarks>
    public static IMississippiSiloBuilder RegisterProjectionBrookMappings(
        this IMississippiSiloBuilder builder,
        Action<IProjectionBrookRegistry> configure,
        bool requireExplicitPaths = false
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        ProjectionBrookRegistry registry = new();
        configure(registry);
        if (requireExplicitPaths)
        {
            registry.ValidateExplicitPaths();
        }

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IProjectionBrookRegistry>();
            services.AddSingleton<IProjectionBrookRegistry>(registry);
        });
        return builder;
    }

    /// <summary>
    ///     Registers projection-to-brook mappings in the <see cref="IProjectionBrookRegistry" />.
    /// </summary>
    /// <param name="builder">The Mississippi server builder.</param>
    /// <param name="configure">
    ///     An action that registers path-to-brook-name mappings on the registry.
    /// </param>
    /// <param name="requireExplicitPaths">
    ///     When <c>true</c>, validates that all projection paths were explicitly defined
    ///     via <c>[GenerateProjectionEndpoints(Path = "...")]</c> and throws if any were auto-derived.
    /// </param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiServerBuilder RegisterProjectionBrookMappings(
        this IMississippiServerBuilder builder,
        Action<IProjectionBrookRegistry> configure,
        bool requireExplicitPaths = false
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        ProjectionBrookRegistry registry = new();
        configure(registry);
        if (requireExplicitPaths)
        {
            registry.ValidateExplicitPaths();
        }

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IProjectionBrookRegistry>();
            services.AddSingleton<IProjectionBrookRegistry>(registry);
        });
        return builder;
    }
}