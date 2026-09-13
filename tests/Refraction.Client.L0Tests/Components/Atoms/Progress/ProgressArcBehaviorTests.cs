using System;
using System.Globalization;

using Bunit;

using Mississippi.Refraction.Client.Components.Atoms.Progress;
using Mississippi.Refraction.Client.Infrastructure.Theming;


namespace Mississippi.Refraction.Client.L0Tests.Components.Atoms.Progress;

/// <summary>Verifies completion geometry, range contracts and accessible transitions.</summary>
public sealed class ProgressArcBehaviorTests : BunitContext
{
    /// <summary>Clamped completion drives both the arc and accessible value.</summary>
    /// <param name="value">Requested completion.</param>
    /// <param name="expected">Clamped completion.</param>
    /// <param name="offset">The unfilled portion.</param>
    [Theory]
    [InlineData(-50, "0", "100")]
    [InlineData(0, "0", "100")]
    [InlineData(25, "25", "75")]
    [InlineData(50, "50", "50")]
    [InlineData(75, "75", "25")]
    [InlineData(100, "100", "0")]
    [InlineData(150, "100", "0")]
    public void CompletionMatchesGeometry(
        double value,
        string expected,
        string offset
    )
    {
        using IRenderedComponent<ProgressArc> cut = Render<ProgressArc>(p => p.Add(c => c.Value, value));
        Assert.Equal(expected, cut.Find("[role=progressbar]").GetAttribute("aria-valuenow"));
        Assert.Equal(offset, cut.Find(".rf-progress-arc__fill").GetAttribute("stroke-dashoffset"));
        Assert.Equal("100", cut.Find(".rf-progress-arc__fill").GetAttribute("pathLength"));
        Assert.Equal(expected == "0" ? "0" : "1", cut.Find(".rf-progress-arc__fill").GetAttribute("opacity"));
        Assert.Equal(value, cut.Instance.Value);
    }

    /// <summary>Names and branding survive while conflicting state attributes cannot misreport progress.</summary>
    [Fact]
    public void ExplicitSemanticsPreserveCallerNameAndBranding()
    {
        using IRenderedComponent<ProgressArc> cut = Render<ProgressArc>(p => p
            .Add(c => c.Class, "brand-progress")
            .Add(c => c.Value, 25)
            .AddUnmatched("aria-label", "Import records")
            .AddUnmatched("aria-describedby", "import-help")
            .AddUnmatched("role", "button")
            .AddUnmatched("aria-valuenow", "99")
            .AddUnmatched("style", "--rf-progress-size:64px"));
        Assert.Equal("Import records", cut.Find("[role=progressbar]").GetAttribute("aria-label"));
        Assert.Equal("import-help", cut.Find("[role=progressbar]").GetAttribute("aria-describedby"));
        Assert.Equal("25", cut.Find("[role=progressbar]").GetAttribute("aria-valuenow"));
        Assert.Equal("--rf-progress-size:64px", cut.Find(".brand-progress").GetAttribute("style"));
        Assert.Equal("true", cut.Find("svg").GetAttribute("aria-hidden"));
        Assert.Equal("false", cut.Find("svg").GetAttribute("focusable"));
        cut.Render(p => p.Add(c => c.Class, " "));
        Assert.Equal("rf-progress-arc", cut.Find("[role=progressbar]").ClassName);
    }

    /// <summary>Invalid ranges and determinate values fail before rendering misleading progress.</summary>
    /// <param name="min">Minimum.</param>
    /// <param name="max">Maximum.</param>
    /// <param name="value">Completion.</param>
    /// <param name="parameter">The rejected parameter.</param>
    [Theory]
    [InlineData(double.NaN, 100, 0, "min")]
    [InlineData(double.NegativeInfinity, 100, 0, "min")]
    [InlineData(0, double.PositiveInfinity, 0, "max")]
    [InlineData(0, double.NaN, 0, "max")]
    [InlineData(10, 10, 10, "max")]
    [InlineData(10, 0, 5, "max")]
    [InlineData(double.MinValue, double.MaxValue, 0, "max")]
    [InlineData(0, 100, double.NaN, "value")]
    [InlineData(0, 100, double.PositiveInfinity, "value")]
    [InlineData(0, 100, double.NegativeInfinity, "value")]
    public void InvalidNumbersAreRejected(
        double min,
        double max,
        double value,
        string parameter
    )
    {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            using IRenderedComponent<ProgressArc> cut = Render<ProgressArc>(p =>
                p.Add(c => c.Min, min).Add(c => c.Max, max).Add(c => c.Value, value));
        });
        Assert.Equal(parameter, error.ParamName);
    }

    /// <summary>Nonzero ranges and fractional values retain machine-readable invariant numbers.</summary>
    [Fact]
    public void RangeChangesUseInvariantNumbers()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            using IRenderedComponent<ProgressArc> cut = Render<ProgressArc>(p => p
                .Add(c => c.Min, -10.5)
                .Add(c => c.Max, 9.5)
                .Add(c => c.Value, -5.5));
            Assert.Equal("-5.5", cut.Find("[role=progressbar]").GetAttribute("aria-valuenow"));
            Assert.Equal("-10.5", cut.Find("[role=progressbar]").GetAttribute("aria-valuemin"));
            Assert.Equal("9.5", cut.Find("[role=progressbar]").GetAttribute("aria-valuemax"));
            Assert.Equal("75", cut.Find(".rf-progress-arc__fill").GetAttribute("stroke-dashoffset"));
            cut.Render(p => p.Add(c => c.Min, -5.5).Add(c => c.Max, 4.5).Add(c => c.Value, 2));
            Assert.Equal("25", cut.Find(".rf-progress-arc__fill").GetAttribute("stroke-dashoffset"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>The provider's live motion preference reaches the atom.</summary>
    [Fact]
    public void ReducedMotionTracksThemeScope()
    {
        using IRenderedComponent<CascadingRefractionProvider> cut = Render<CascadingRefractionProvider>(p =>
            p.AddChildContent<ProgressArc>(arc => arc.Add(c => c.State, RefractionStates.Indeterminate)));
        Assert.Equal("false", cut.Find("[role=progressbar]").GetAttribute("data-reduced-motion"));
        cut.Render(p => p.Add(c => c.IsReducedMotion, true));
        Assert.Equal("true", cut.Find("[role=progressbar]").GetAttribute("data-reduced-motion"));
        cut.Render(p => p.Add(c => c.IsReducedMotion, false));
        Assert.Equal("false", cut.Find("[role=progressbar]").GetAttribute("data-reduced-motion"));
    }

    /// <summary>Unknown duration ignores Value and cannot acquire a false numeric value from attributes.</summary>
    [Fact]
    public void UnknownDurationTransitionsBackToCompletion()
    {
        using IRenderedComponent<ProgressArc> cut = Render<ProgressArc>(p => p
            .Add(c => c.Value, 50)
            .AddUnmatched("aria-valuenow", "99")
            .AddUnmatched("aria-labelledby", "task-title"));
        cut.Render(p => p.Add(c => c.State, RefractionStates.Indeterminate).Add(c => c.Value, double.NaN));
        Assert.False(cut.Find("[role=progressbar]").HasAttribute("aria-valuenow"));
        Assert.Equal("task-title", cut.Find("[role=progressbar]").GetAttribute("aria-labelledby"));
        Assert.Equal("75", cut.Find(".rf-progress-arc__fill").GetAttribute("stroke-dashoffset"));
        cut.Render(p => p.Add(c => c.State, RefractionStates.Determinate).Add(c => c.Value, 100));
        Assert.Equal("100", cut.Find("[role=progressbar]").GetAttribute("aria-valuenow"));
        Assert.Equal("0", cut.Find(".rf-progress-arc__fill").GetAttribute("stroke-dashoffset"));
    }

    /// <summary>Unsupported states cannot silently imply determinate completion.</summary>
    [Fact]
    public void UnsupportedStateIsRejected()
    {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            using IRenderedComponent<ProgressArc> cut = Render<ProgressArc>(p => p.Add(c => c.State, "unrecognized"));
        });
        Assert.Equal("state", error.ParamName);
    }
}