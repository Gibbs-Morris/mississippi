using System;
using System.Collections.Generic;

using Mississippi.Refraction.Abstractions.Theme;


namespace Mississippi.Refraction.Client.L0Tests.Infrastructure;

/// <summary>
///     Test theme catalog for Refraction runtime foundation tests.
/// </summary>
internal sealed class TestRefractionThemeCatalog : IRefractionThemeCatalog
{
    private static readonly RefractionThemeDescriptor DefaultDescriptor = new()
    {
        BrandId = new("horizon"),
        CssScopeName = "horizon",
        DisplayName = "Horizon",
        IsDefault = true,
    };

    private static readonly RefractionThemeDescriptor SignalDescriptor = new()
    {
        BrandId = new("signal"),
        CssScopeName = "signal",
        DisplayName = "Signal",
    };

    private static readonly Dictionary<string, RefractionThemeDescriptor> ThemeDescriptors =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [DefaultDescriptor.BrandId.Value] = DefaultDescriptor,
            [SignalDescriptor.BrandId.Value] = SignalDescriptor,
        };

    /// <inheritdoc />
    public RefractionThemeDescriptor DefaultTheme => DefaultDescriptor;

    /// <inheritdoc />
    public IReadOnlyList<RefractionThemeDescriptor> Themes { get; } = [DefaultDescriptor, SignalDescriptor];

    /// <inheritdoc />
    public RefractionThemeDescriptor? GetTheme(
        RefractionBrandId brandId
    ) =>
        ThemeDescriptors.TryGetValue(brandId.Value, out RefractionThemeDescriptor? descriptor) ? descriptor : null;
}