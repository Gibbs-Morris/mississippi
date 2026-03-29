using System;
using System.Reflection;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Client.Components.Molecules;


namespace Mississippi.Refraction.Client.L0Tests.Components.Molecules;

/// <summary>
///     Tests for <see cref="EmptyState" /> component.
/// </summary>
public sealed class EmptyStateTests : BunitContext
{
    /// <summary>
    ///     EmptyState does not render a title when one is not provided.
    /// </summary>
    [Fact]
    public void EmptyStateDoesNotRenderTitleWhenEmpty()
    {
        // Act
        using IRenderedComponent<EmptyState> cut = Render<EmptyState>(parameters => parameters.Add(
            component => component.Message,
            "No work items match the current search and stage filter."));

        // Assert
        Assert.Empty(cut.FindAll(".rf-empty-state__title"));
    }

    /// <summary>
    ///     EmptyState has AdditionalAttributes parameter with CaptureUnmatchedValues.
    /// </summary>
    [Fact]
    public void EmptyStateHasAdditionalAttributesParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(EmptyState).GetProperty("AdditionalAttributes");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr!.CaptureUnmatchedValues);
    }

    /// <summary>
    ///     EmptyState has Message parameter.
    /// </summary>
    [Fact]
    public void EmptyStateHasMessageParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(EmptyState).GetProperty("Message");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(string), prop.PropertyType);
    }

    /// <summary>
    ///     EmptyState has State parameter.
    /// </summary>
    [Fact]
    public void EmptyStateHasStateParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(EmptyState).GetProperty("State");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     EmptyState has Title parameter.
    /// </summary>
    [Fact]
    public void EmptyStateHasTitleParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(EmptyState).GetProperty("Title");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(string), prop.PropertyType);
    }

    /// <summary>
    ///     EmptyState inherits from ComponentBase.
    /// </summary>
    [Fact]
    public void EmptyStateInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(EmptyState)));
    }

    /// <summary>
    ///     EmptyState merges an additional CSS class with the component root class.
    /// </summary>
    [Fact]
    public void EmptyStateMergesAdditionalCssClassWithRootClass()
    {
        // Act
        using IRenderedComponent<EmptyState> cut = Render<EmptyState>(parameters => parameters
            .Add(component => component.Message, "No work items match the current search and stage filter.")
            .AddUnmatched("class", "empty-state-host"));

        // Assert
        Assert.NotNull(cut.Find(".rf-empty-state.empty-state-host"));
    }

    /// <summary>
    ///     EmptyState falls back to the default root class when the host class attribute is null.
    /// </summary>
    [Fact]
    public void EmptyStateFallsBackToDefaultRootClassWhenAdditionalCssClassIsNull()
    {
        // Act
        using IRenderedComponent<EmptyState> cut = Render<EmptyState>(parameters => parameters
            .Add(component => component.Message, "No work items match the current search and stage filter.")
            .AddUnmatched("class", (object?)null));

        // Assert
        Assert.Equal("rf-empty-state", cut.Find(".rf-empty-state").GetAttribute("class"));
    }

    /// <summary>
    ///     EmptyState renders additional attributes.
    /// </summary>
    [Fact]
    public void EmptyStateRendersAdditionalAttributes()
    {
        // Act
        using IRenderedComponent<EmptyState> cut = Render<EmptyState>(parameters => parameters
            .Add(component => component.Message, "Select a queue item to inspect the current response plan.")
            .AddUnmatched("data-testid", "empty-state-1"));

        // Assert
        Assert.Equal("empty-state-1", cut.Find(".rf-empty-state").GetAttribute("data-testid"));
    }

    /// <summary>
    ///     EmptyState renders custom state.
    /// </summary>
    [Fact]
    public void EmptyStateRendersCustomState()
    {
        // Act
        using IRenderedComponent<EmptyState> cut = Render<EmptyState>(parameters => parameters
            .Add(component => component.Message, "No work items match the current search and stage filter.")
            .Add(component => component.State, RefractionStates.Active));

        // Assert
        Assert.Equal("active", cut.Find(".rf-empty-state").GetAttribute("data-state"));
    }

    /// <summary>
    ///     EmptyState renders the message.
    /// </summary>
    [Fact]
    public void EmptyStateRendersMessage()
    {
        // Act
        using IRenderedComponent<EmptyState> cut = Render<EmptyState>(parameters => parameters.Add(
            component => component.Message,
            "No work items match the current search and stage filter."));

        // Assert
        Assert.Contains(
            "No work items match the current search and stage filter.",
            cut.Find(".rf-empty-state__message").TextContent,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     EmptyState renders the title when provided.
    /// </summary>
    [Fact]
    public void EmptyStateRendersTitleWhenProvided()
    {
        // Act
        using IRenderedComponent<EmptyState> cut = Render<EmptyState>(parameters => parameters
            .Add(component => component.Message, "Select a queue item to inspect the current response plan.")
            .Add(component => component.Title, "No work item selected"));

        // Assert
        Assert.Contains(
            "No work item selected",
            cut.Find(".rf-empty-state__title").TextContent,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     EmptyState renders with default state.
    /// </summary>
    [Fact]
    public void EmptyStateRendersWithDefaultState()
    {
        // Act
        using IRenderedComponent<EmptyState> cut = Render<EmptyState>(parameters => parameters.Add(
            component => component.Message,
            "No work items match the current search and stage filter."));

        // Assert
        Assert.Equal("quiet", cut.Find(".rf-empty-state").GetAttribute("data-state"));
    }

    /// <summary>
    ///     EmptyState State defaults to Quiet.
    /// </summary>
    [Fact]
    public void EmptyStateStateDefaultsToQuiet()
    {
        // Arrange
        EmptyState component = new();

        // Assert
        Assert.Equal(RefractionStates.Quiet, component.State);
    }
}