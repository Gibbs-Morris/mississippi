using LightSpeed.Client.Features.KitchenSinkFeatures.MisHelpText;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Refraction.Components.Molecules.MisTextInputActions;
using Mississippi.Reservoir.Blazor;


namespace LightSpeed.Client.Compoments;

/// <summary>
///     Displays and manages the MisHelpText demo section for the kitchen sink page.
/// </summary>
public sealed partial class KitchenSinkMisHelpTextSection : StoreComponent
{
    private string HelpTextContent =>
        Select<MisHelpTextKitchenSinkState, string>(MisHelpTextKitchenSinkSelectors.GetHelpTextContent);

    private MisHelpTextViewModel HelpTextModel =>
        Select<MisHelpTextKitchenSinkState, MisHelpTextViewModel>(MisHelpTextKitchenSinkSelectors.GetViewModel);

    private void HandlePropertyTextInputAction(
        IMisTextInputAction action
    )
    {
        if (action is MisTextInputInputAction inputAction)
        {
            string? value = string.IsNullOrWhiteSpace(inputAction.Value) ? null : inputAction.Value.Trim();
            switch (inputAction.IntentId)
            {
                case "prop-content":
                    Dispatch(new SetMisHelpTextContentAction(value));
                    break;
                case "prop-id":
                    Dispatch(new SetMisHelpTextIdAction(value));
                    break;
                case "prop-cssclass":
                    Dispatch(new SetMisHelpTextCssClassAction(value));
                    break;
            }
        }
    }
}