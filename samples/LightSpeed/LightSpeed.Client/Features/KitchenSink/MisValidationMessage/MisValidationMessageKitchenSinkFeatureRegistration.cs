using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisValidationMessage;

/// <summary>
///     Extension methods for registering the MisValidationMessage Kitchen Sink feature.
/// </summary>
internal static class MisValidationMessageKitchenSinkFeatureRegistration
{
    /// <summary>
    ///     Adds reducers for the MisValidationMessage Kitchen Sink feature.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMisValidationMessageKitchenSinkFeature(
        this IServiceCollection services
    )
    {
        services.AddReducer<SetMisValidationMessageTextAction, MisValidationMessageKitchenSinkState>(
            MisValidationMessageKitchenSinkReducers.SetText);
        services.AddReducer<SetMisValidationMessageForAction, MisValidationMessageKitchenSinkState>(
            MisValidationMessageKitchenSinkReducers.SetFor);
        services.AddReducer<SetMisValidationMessageSeverityAction, MisValidationMessageKitchenSinkState>(
            MisValidationMessageKitchenSinkReducers.SetSeverity);
        services.AddReducer<SetMisValidationMessageCssClassAction, MisValidationMessageKitchenSinkState>(
            MisValidationMessageKitchenSinkReducers.SetCssClass);
        return services;
    }
}