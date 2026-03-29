using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

using Mississippi.Refraction.Abstractions.Theme;


namespace Mississippi.Refraction.Client.Infrastructure;

/// <summary>
///     Applies the Refraction token runtime to a host-owned content tree.
/// </summary>
public partial class RefractionRoot : ComponentBase
{
    private const string ThemeStylesheetPath = "_content/Mississippi.Refraction.Client/themes/refraction.css";

    /// <summary>
    ///     Gets or sets the child content to wrap with the Refraction token runtime.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the Refraction theme stylesheet should be emitted.
    /// </summary>
    [Parameter]
    public bool IncludeThemeAssets { get; set; } = true;

    /// <summary>
    ///     Gets or sets the deterministic preference snapshot used for runtime theme resolution.
    /// </summary>
    [Parameter]
    public RefractionPreferenceSnapshot Preferences { get; set; } = new();

    /// <summary>
    ///     Gets or sets the host-owned Refraction theme selection.
    /// </summary>
    [Parameter]
    public RefractionThemeSelection Selection { get; set; } = new();

    private string ContrastAttributeValue => RefractionThemeAttributeValueFormatter.Format(ResolvedSelection.Contrast);

    private string DensityAttributeValue => RefractionThemeAttributeValueFormatter.Format(ResolvedSelection.Density);

    private bool IsReducedMotion => ResolvedSelection.Motion == RefractionMotionMode.Reduced;

    [Inject]
    private ILogger<RefractionRoot> Logger { get; set; } = default!;

    private string MotionAttributeValue => RefractionThemeAttributeValueFormatter.Format(ResolvedSelection.Motion);

    private RefractionThemeSelection ResolvedSelection { get; set; } = new();

    private RefractionThemeDescriptor ResolvedTheme { get; set; } = new()
    {
        BrandId = new("horizon"),
        CssScopeName = "horizon",
        DisplayName = "Horizon",
        IsDefault = true,
    };

    [Inject]
    private IRefractionThemeCatalog ThemeCatalog { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ResolvedTheme = ResolveTheme();
        ResolvedSelection = ResolveSelection(ResolvedTheme);
    }

    private RefractionContrastMode ResolveContrast(
        RefractionContrastMode contrastMode
    )
    {
        if (contrastMode != RefractionContrastMode.System)
        {
            return contrastMode;
        }

        return Preferences.PrefersHighContrast ? RefractionContrastMode.High : RefractionContrastMode.Standard;
    }

    private RefractionMotionMode ResolveMotion(
        RefractionMotionMode motionMode
    )
    {
        if (motionMode != RefractionMotionMode.System)
        {
            return motionMode;
        }

        return Preferences.PrefersReducedMotion ? RefractionMotionMode.Reduced : RefractionMotionMode.Standard;
    }

    private RefractionThemeSelection ResolveSelection(
        RefractionThemeDescriptor theme
    ) =>
        new()
        {
            BrandId = theme.BrandId,
            Contrast = ResolveContrast(Selection.Contrast),
            Density = Selection.Density,
            Motion = ResolveMotion(Selection.Motion),
        };

    private RefractionThemeDescriptor ResolveTheme()
    {
        if (Selection.BrandId is not RefractionBrandId brandId || string.IsNullOrWhiteSpace(brandId.Value))
        {
            return ThemeCatalog.DefaultTheme;
        }

        RefractionThemeDescriptor? theme = ThemeCatalog.GetTheme(brandId);
        if (theme is not null)
        {
            return theme;
        }

        Logger.UnknownThemeFallback(brandId.Value, ThemeCatalog.DefaultTheme.BrandId.Value);
        return ThemeCatalog.DefaultTheme;
    }
}