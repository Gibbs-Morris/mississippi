using System;
using System.Reflection;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Mississippi.Refraction.Client.Components.Molecules.Notifications;


namespace Mississippi.Refraction.Client.L0Tests.Components.Molecules.Notifications;

/// <summary>
///     Tests for <see cref="NotificationPulse" /> component.
/// </summary>
public sealed class NotificationPulseTests : BunitContext
{
    /// <summary>
    ///     NotificationPulse has AdditionalAttributes parameter.
    /// </summary>
    [Fact]
    public void NotificationPulseHasAdditionalAttributesParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(NotificationPulse).GetProperty("AdditionalAttributes");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr.CaptureUnmatchedValues);
    }

    /// <summary>
    ///     NotificationPulse has ChildContent parameter.
    /// </summary>
    [Fact]
    public void NotificationPulseHasChildContentParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(NotificationPulse).GetProperty("ChildContent");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(RenderFragment), prop.PropertyType);
    }

    /// <summary>
    ///     NotificationPulse has OnDismiss EventCallback.
    /// </summary>
    [Fact]
    public void NotificationPulseHasOnDismissEventCallback()
    {
        // Arrange
        PropertyInfo? prop = typeof(NotificationPulse).GetProperty("OnDismiss");

        // Assert
        Assert.NotNull(prop);
        Assert.Equal(typeof(EventCallback), prop.PropertyType);
    }

    /// <summary>
    ///     NotificationPulse has OnExpand EventCallback.
    /// </summary>
    [Fact]
    public void NotificationPulseHasOnExpandEventCallback()
    {
        // Arrange
        PropertyInfo? prop = typeof(NotificationPulse).GetProperty("OnExpand");

        // Assert
        Assert.NotNull(prop);
        Assert.True(prop.PropertyType.IsGenericType);
        Assert.Equal(typeof(EventCallback<MouseEventArgs>), prop.PropertyType);
    }

    /// <summary>
    ///     NotificationPulse has State parameter.
    /// </summary>
    [Fact]
    public void NotificationPulseHasStateParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(NotificationPulse).GetProperty("State");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     NotificationPulse inherits from ComponentBase.
    /// </summary>
    [Fact]
    public void NotificationPulseInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(NotificationPulse)));
        Assert.True(typeof(NotificationPulse).IsSealed);
    }

    /// <summary>
    ///     NotificationPulse invokes OnExpand when its expand action is clicked.
    /// </summary>
    [Fact]
    public void NotificationPulseInvokesOnExpandWhenClicked()
    {
        // Arrange
        bool wasExpanded = false;
        MouseEventArgs? receivedArgs = null;
        using IRenderedComponent<NotificationPulse> cut = Render<NotificationPulse>(p => p.Add(
            c => c.OnExpand,
            args =>
            {
                wasExpanded = true;
                receivedArgs = args;
            }));

        // Act
        cut.Find(".rf-notification-pulse__expand").Click();

        // Assert
        Assert.True(wasExpanded);
        Assert.NotNull(receivedArgs);
    }

    /// <summary>
    ///     NotificationPulse renders additional attributes.
    /// </summary>
    [Fact]
    public void NotificationPulseRendersAdditionalAttributes()
    {
        // Act
        using IRenderedComponent<NotificationPulse> cut =
            Render<NotificationPulse>(p => p.AddUnmatched("data-testid", "pulse-1"));

        // Assert
        Assert.Equal("pulse-1", cut.Find(".rf-notification-pulse").GetAttribute("data-testid"));
    }

    /// <summary>
    ///     NotificationPulse renders child content.
    /// </summary>
    [Fact]
    public void NotificationPulseRendersChildContent()
    {
        // Act
        using IRenderedComponent<NotificationPulse> cut = Render<NotificationPulse>(p => p.AddChildContent(
            "<span>Test Content</span>"));

        // Assert
        string textContent = cut.Find(".rf-notification-pulse__content").TextContent;
        Assert.Contains("Test Content", textContent, StringComparison.Ordinal);
    }

    /// <summary>
    ///     NotificationPulse renders custom state.
    /// </summary>
    [Fact]
    public void NotificationPulseRendersCustomState()
    {
        // Act
        using IRenderedComponent<NotificationPulse> cut = Render<NotificationPulse>(p => p.Add(
            c => c.State,
            RefractionStates.Active));

        // Assert
        string? dataState = cut.Find(".rf-notification-pulse").GetAttribute("data-state");
        Assert.Equal("active", dataState);
    }

    /// <summary>
    ///     NotificationPulse renders dot indicator.
    /// </summary>
    [Fact]
    public void NotificationPulseRendersDotIndicator()
    {
        // Act
        using IRenderedComponent<NotificationPulse> cut = Render<NotificationPulse>();

        // Assert
        Assert.NotEmpty(cut.FindAll(".rf-notification-pulse__dot"));
        Assert.Equal("true", cut.Find(".rf-notification-pulse__dot").GetAttribute("aria-hidden"));
    }

    /// <summary>
    ///     NotificationPulse renders with default state.
    /// </summary>
    [Fact]
    public void NotificationPulseRendersWithDefaultState()
    {
        // Act
        using IRenderedComponent<NotificationPulse> cut = Render<NotificationPulse>();

        // Assert
        string? dataState = cut.Find(".rf-notification-pulse").GetAttribute("data-state");
        Assert.Equal("new", dataState);
    }

    /// <summary>
    ///     NotificationPulse renders with a stable atomic status region for accessibility.
    /// </summary>
    [Fact]
    public void NotificationPulseRendersWithStatusRoleForAccessibility()
    {
        // Act
        using IRenderedComponent<NotificationPulse> cut = Render<NotificationPulse>();

        // Assert
        string? role = cut.Find(".rf-notification-pulse__status").GetAttribute("role");
        Assert.Equal("status", role);
        Assert.Equal("true", cut.Find(".rf-notification-pulse__status").GetAttribute("aria-atomic"));
    }

    /// <summary>
    ///     NotificationPulse does not add a root tabindex for keyboard accessibility.
    /// </summary>
    [Fact]
    public void NotificationPulseRendersWithoutRootTabindex()
    {
        // Act
        using IRenderedComponent<NotificationPulse> cut = Render<NotificationPulse>();

        // Assert
        Assert.False(cut.Find(".rf-notification-pulse").HasAttribute("tabindex"));
    }

    /// <summary>
    ///     NotificationPulse State defaults to New.
    /// </summary>
    [Fact]
    public void NotificationPulseStateDefaultsToNew()
    {
        // Arrange
        NotificationPulse component = new();

        // Assert
        Assert.Equal(RefractionStates.New, component.State);
    }
}