using System.Reflection;

using Allure.Xunit.Attributes;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Organisms;


namespace Mississippi.Refraction.L0Tests.Components.Organisms;

/// <summary>
///     Tests for <see cref="SchematicViewer" /> component.
/// </summary>
[AllureSuite("Refraction")]
[AllureSubSuite("Organisms")]
public sealed class SchematicViewerTests
{
    /// <summary>
    ///     SchematicViewer has AdditionalAttributes parameter.
    /// </summary>
    [Fact]
    [AllureFeature("SchematicViewer")]
    public void SchematicViewerHasAdditionalAttributesParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(SchematicViewer).GetProperty("AdditionalAttributes");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr!.CaptureUnmatchedValues);
    }

    /// <summary>
    ///     SchematicViewer has Caption parameter.
    /// </summary>
    [Fact]
    [AllureFeature("SchematicViewer")]
    public void SchematicViewerHasCaptionParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(SchematicViewer).GetProperty("Caption");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(string), prop!.PropertyType);
    }

    /// <summary>
    ///     SchematicViewer has ChildContent parameter.
    /// </summary>
    [Fact]
    [AllureFeature("SchematicViewer")]
    public void SchematicViewerHasChildContentParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(SchematicViewer).GetProperty("ChildContent");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(RenderFragment), prop!.PropertyType);
    }

    /// <summary>
    ///     SchematicViewer has State parameter.
    /// </summary>
    [Fact]
    [AllureFeature("SchematicViewer")]
    public void SchematicViewerHasStateParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(SchematicViewer).GetProperty("State");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     SchematicViewer inherits from ComponentBase.
    /// </summary>
    [Fact]
    [AllureFeature("SchematicViewer")]
    public void SchematicViewerInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(SchematicViewer)));
    }

    /// <summary>
    ///     SchematicViewer State defaults to Idle.
    /// </summary>
    [Fact]
    [AllureFeature("SchematicViewer")]
    public void SchematicViewerStateDefaultsToIdle()
    {
        // Arrange
        SchematicViewer component = new();

        // Assert
        Assert.Equal(RefractionStates.Idle, component.State);
    }
}