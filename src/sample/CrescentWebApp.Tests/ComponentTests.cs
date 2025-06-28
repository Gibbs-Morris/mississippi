using System.Diagnostics;

using Bunit;

using Mississippi.CrescentWebApp.Components.Pages;


namespace Mississippi.CrescentWebApp.Tests;

/// <summary>
///     Unit tests covering the sample Crescent WebApp Razor components.
/// </summary>
public sealed class ComponentTests
{
    /// <summary>
    ///     Verifies that the Home component renders the default greeting message.
    /// </summary>
    [Fact]
    public void HomeComponentRendersGreeting()
    {
        using TestContext ctx = new();

        // Act
        using IRenderedComponent<Home> cut = ctx.RenderComponent<Home>();

        // Assert
        Assert.Contains("Hello, world!", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Ensures the Error component outputs the current <see cref="Activity" /> identifier when available.
    /// </summary>
    [Fact]
    public void ErrorComponentRendersRequestIdWhenPresent()
    {
        using TestContext ctx = new();
        using Activity activity = new("test");
        activity.Start();
        using IRenderedComponent<Error> cut = ctx.RenderComponent<Error>();
        Assert.Contains(activity.Id!, cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Confirms the Error component omits the request identifier when none is available.
    /// </summary>
    [Fact]
    public void ErrorComponentDoesNotRenderRequestIdWhenAbsent()
    {
        using TestContext ctx = new();

        // Act
        using IRenderedComponent<Error> cut = ctx.RenderComponent<Error>();

        // Assert
        Assert.DoesNotContain("<code>", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }
}