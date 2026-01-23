using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir;


namespace Spring.Client.Features.EntitySelection;

/// <summary>
///     Extension methods for registering the EntitySelection feature.
/// </summary>
/// <remarks>
///     <para>
///         This feature provides application-specific state for tracking which entity
///         is currently selected in the UI. Commands read the EntityId from this state
///         when being dispatched.
///     </para>
///     <para>
///         This is separate from aggregate command state because entity selection is a
///         UI/navigation concern, not part of the command execution lifecycle.
///     </para>
/// </remarks>
internal static class EntitySelectionFeatureRegistration
{
    /// <summary>
    ///     Adds the EntitySelection feature to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEntitySelectionFeature(
        this IServiceCollection services
    )
    {
        services.AddReducer<SetEntityIdAction, EntitySelectionState>(EntitySelectionReducers.SetEntityId);
        return services;
    }
}