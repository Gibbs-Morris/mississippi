using System.Reflection;

using Allure.Xunit.Attributes;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Mississippi.Refraction.Components.Atoms;


namespace Mississippi.Refraction.L0Tests.Components.Atoms;

/// <summary>
///     Tests for <see cref="NotificationPulse" /> component.
/// </summary>
[AllureSuite("Refraction")]
[AllureSubSuite("Atoms")]
public sealed class NotificationPulseTests : BunitContext
{
    /// <summary>
    ///     NotificationPulse has AdditionalAttributes parameter.
    /// </summary>
    [Fact]
    [AllureFeature("NotificationPulse")]
    public void NotificationPulseHasAdditionalAttributesParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(NotificationPulse).GetProperty("AdditionalAttributes");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr!.CaptureUnmatchedValues);
    }

    /// <summary>
    ///     NotificationPulse has ChildContent parameter.
    /// </summary>
    [Fact]
    [AllureFeature("NotificationPulse")]
    public void NotificationPulseHasChildContentParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(NotificationPulse).GetProperty("ChildContent");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(RenderFragment), prop!.PropertyType);
    }

    /// <summary>
    ///     NotificationPulse has OnDismiss EventCallback.
    /// </summary>
    [Fact]
    [AllureFeature("NotificationPulse")]
    public void NotificationPulseHasOnDismissEventCallback()
    {
        // Arrange
        PropertyInfo? prop = typeof(NotificationPulse).GetProperty("OnDismiss");

        // Assert
        Assert.NotNull(prop);
        Assert.Equal(typeof(EventCallback), prop!.PropertyType);
    }

    /// <summary>
    ///     NotificationPulse has OnExpand EventCallback.
    /// </summary>
    [Fact]
    [AllureFeature("NotificationPulse")]
    public void NotificationPulseHasOnExpandEventCallback()
    {
        // Arrange
        PropertyInfo? prop = typeof(NotificationPulse).GetProperty("OnExpand");

        // Assert
        Assert.NotNull(prop);
        Assert.True(prop!.PropertyType.IsGenericType);
        Assert.Equal(typeof(EventCallback<MouseEventArgs>), prop.PropertyType);
    }

    /// <summary>
    ///     NotificationPulse has State parameter.
    /// </summary>
    [Fact]
    [AllureFeature("NotificationPulse")]
    public void NotificationPulseHasStateParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(NotificationPulse).GetProperty("State");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     NotificationPulse inherits from ComponentBase.
    /// </summary>
    [Fact]
    [AllureFeature("NotificationPulse")]
    public void NotificationPulseInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(NotificationPulse)));
    }

    /// <summary>
    ///     NotificationPulse State defaults to New.
    /// </summary>
    [Fact]
    [AllureFeature("NotificationPulse")]
    public void NotificationPulseStateDefaultsToNew()
    {
        // Arrange
        NotificationPulse component = new();

        // Assert
        Assert.Equal(RefractionStates.New, component.State);
    }

    /// <summary>
    ///     NotificationPulse renders with default state.
    /// </summary>
    [Fact]
    [AllureFeature("NotificationPulse")]
    public void NotificationPulseRendersWithDefaultState()
    {
        // Act
        using var cut = Render<NotificationPulse>();

        // Assert
        string? dataState = cut.Find(".rf-notification-pulse").GetAttribute("data-state");
        Assert.Equal("new", dataState);
    }

    /// <summary>
    ///     NotificationPulse renders custom state.
    /// </summary>
    [Fact]
    [AllureFeature("NotificationPulse")]
    public void NotificationPulseRendersCustomState()
    {
        // Act
        using var cut = Render<NotificationPulse>(p => p
            .Add(c => c.State, RefractionStates.Active));

        // Assert
        string? dataState = cut.Find(".rf-notification-pulse").GetAttribute("data-state");
        Assert.Equal("active", dataState);
    }

    /// <summary>
    ///     NotificationPulse renders additional attributes.
    /// </summary>
    [Fact]
    [AllureFeature("NotificationPulse")]
    public void NotificationPulseRendersAdditionalAttributes()
    {
        // Act
        using var cut = Render<NotificationPulse>(p => p
            .AddUnmatched("data-testid", "pulse-1"));

        // Assert
        Assert.Equal("pulse-1", cut.Find(".rf-notification-pulse").GetAttribute("data-testid"));
    }

    /// <summary>
    ///     NotificationPulse renders dot indicator.
    /// </summary>
    [Fact]
    [AllureFeature("NotificationPulse")]
    public void NotificationPulseRendersDotIndicator()
    {
        // Act
        using var cut = Render<NotificationPulse>();

        // Assert
        Assert.NotEmpty(cut.FindAll(".rf-notification-pulse__dot"));
    }
}