using System.Reflection;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Atoms;


namespace Mississippi.Refraction.L0Tests.Components.Atoms;

/// <summary>
///     Tests for <see cref="Reticle" /> component.
/// </summary>
public sealed class ReticleTests : BunitContext
{
    /// <summary>
    ///     Reticle has AdditionalAttributes parameter.
    /// </summary>
    [Fact]
    public void ReticleHasAdditionalAttributesParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(Reticle).GetProperty("AdditionalAttributes");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr!.CaptureUnmatchedValues);
    }

    /// <summary>
    ///     Reticle has Mode parameter.
    /// </summary>
    [Fact]
    public void ReticleHasModeParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(Reticle).GetProperty("Mode");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(string), prop!.PropertyType);
    }

    /// <summary>
    ///     Reticle has State parameter.
    /// </summary>
    [Fact]
    public void ReticleHasStateParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(Reticle).GetProperty("State");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     Reticle inherits from ComponentBase.
    /// </summary>
    [Fact]
    public void ReticleInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(Reticle)));
    }

    /// <summary>
    ///     Reticle Mode defaults to Focus.
    /// </summary>
    [Fact]
    public void ReticleModeDefaultsToFocus()
    {
        // Arrange
        Reticle component = new();

        // Assert
        Assert.Equal(RefractionReticleModes.Focus, component.Mode);
    }

    /// <summary>
    ///     Reticle renders additional attributes.
    /// </summary>
    [Fact]
    public void ReticleRendersAdditionalAttributes()
    {
        // Act
        using IRenderedComponent<Reticle> cut = Render<Reticle>(p => p.AddUnmatched("data-testid", "reticle-1"));

        // Assert
        Assert.Equal("reticle-1", cut.Find(".rf-reticle").GetAttribute("data-testid"));
    }

    /// <summary>
    ///     Reticle renders custom mode.
    /// </summary>
    [Fact]
    public void ReticleRendersCustomMode()
    {
        // Act
        using IRenderedComponent<Reticle> cut = Render<Reticle>(p => p.Add(
            c => c.Mode,
            RefractionReticleModes.Command));

        // Assert
        string? dataMode = cut.Find(".rf-reticle").GetAttribute("data-mode");
        Assert.Equal("command", dataMode);
    }

    /// <summary>
    ///     Reticle renders default mode.
    /// </summary>
    [Fact]
    public void ReticleRendersDefaultMode()
    {
        // Act
        using IRenderedComponent<Reticle> cut = Render<Reticle>();

        // Assert
        string? dataMode = cut.Find(".rf-reticle").GetAttribute("data-mode");
        Assert.Equal("focus", dataMode);
    }

    /// <summary>
    ///     Reticle renders ring indicator.
    /// </summary>
    [Fact]
    public void ReticleRendersRingIndicator()
    {
        // Act
        using IRenderedComponent<Reticle> cut = Render<Reticle>();

        // Assert
        Assert.NotEmpty(cut.FindAll(".rf-reticle__ring"));
    }

    /// <summary>
    ///     Reticle renders with default state.
    /// </summary>
    [Fact]
    public void ReticleRendersWithDefaultState()
    {
        // Act
        using IRenderedComponent<Reticle> cut = Render<Reticle>();

        // Assert
        string? dataState = cut.Find(".rf-reticle").GetAttribute("data-state");
        Assert.Equal("idle", dataState);
    }

    /// <summary>
    ///     Reticle State defaults to Idle.
    /// </summary>
    [Fact]
    public void ReticleStateDefaultsToIdle()
    {
        // Arrange
        Reticle component = new();

        // Assert
        Assert.Equal(RefractionStates.Idle, component.State);
    }
}