using Microsoft.AspNetCore.Components.Web;

using Mississippi.Refraction.Client.Infrastructure.Theming;

using MississippiSamples.LightSpeed.Client.Features.Showcase;


namespace MississippiSamples.LightSpeed.Client.Pages.KitchenSink;

/// <summary>Connects the interactive gallery to Reservoir actions and selectors.</summary>
/// <remarks>Public so the Blazor router can instantiate the page.</remarks>
public sealed partial class KitchenSinkPage
{
    private ShowcaseView View => Select<ShowcaseState, ShowcaseView>(ShowcaseSelectors.GetView);

    private void ActivateEmitter(
        MouseEventArgs mouseEventArgs
    ) =>
        Dispatch(new ActivateEmitterAction());

    private void ChangeEmail(
        string email
    ) =>
        Dispatch(new ChangeEmailAction(email));

    private void ChangeEmitterDisabled(
        bool isDisabled
    ) =>
        Dispatch(new ChangeEmitterDisabledAction(isDisabled));

    private void ChangeProgress(
        int? percent
    ) =>
        Dispatch(new ChangeProgressAction(percent));

    private void ChangeTheme(
        RefractionThemeMode mode
    ) =>
        Dispatch(new ChangeThemeAction(mode));

    private void ResetProfile() => Dispatch(new ResetProfileAction());

    private void ValidateProfile() => Dispatch(new ValidateProfileAction());
}