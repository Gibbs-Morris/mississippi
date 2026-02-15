using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisHelpText;

/// <summary>
///     Extension methods for registering the MisHelpText Kitchen Sink feature.
/// </summary>
internal static class MisHelpTextKitchenSinkFeatureRegistration
{
    /// <summary>
    ///     Adds reducers for the MisHelpText Kitchen Sink feature.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMisHelpTextKitchenSinkFeature(
        this IServiceCollection services
    )
    {
        services.AddReducer<SetMisHelpTextContentAction, MisHelpTextKitchenSinkState>(MisHelpTextKitchenSinkReducers.SetContent);
        services.AddReducer<SetMisHelpTextCssClassAction, MisHelpTextKitchenSinkState>(MisHelpTextKitchenSinkReducers.SetCssClass);
        services.AddReducer<SetMisHelpTextIdAction, MisHelpTextKitchenSinkState>(MisHelpTextKitchenSinkReducers.SetId);
        return services;
    }
}
