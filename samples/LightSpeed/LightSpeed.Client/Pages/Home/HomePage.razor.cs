using Mississippi.Refraction.Client.Infrastructure.Theming;
using Mississippi.Reservoir.Client;

using MississippiSamples.LightSpeed.Client.Features.Showcase;


namespace MississippiSamples.LightSpeed.Client.Pages.Home;

/// <summary>Connects the showcase entry page to Reservoir.</summary>
/// <remarks>Public so the Blazor router can instantiate the page.</remarks>
public sealed partial class HomePage : StoreComponent
{
    private ShowcaseView View => Select<ShowcaseState, ShowcaseView>(ShowcaseSelectors.GetView);

    private void ChangeTheme(
        RefractionThemeMode mode
    ) =>
        Dispatch(new ChangeThemeAction(mode));
}