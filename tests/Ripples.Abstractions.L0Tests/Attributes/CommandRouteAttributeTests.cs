namespace Mississippi.Ripples.Abstractions.L0Tests.Attributes;

using System;

using Allure.Xunit.Attributes;

using Xunit;

/// <summary>
/// Tests for <see cref="CommandRouteAttribute"/>.
/// </summary>
[AllureParentSuite("Ripples")]
[AllureSuite("Abstractions")]
[AllureSubSuite("CommandRouteAttribute")]
public sealed class CommandRouteAttributeTests
{
    /// <summary>
    /// Verifies that CommandRouteAttribute targets method only.
    /// </summary>
    [Fact]
    [AllureFeature("Attribute Usage")]
    public void TargetsMethodOnly()
    {
        // Arrange & Act
        var attributeUsage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(
            typeof(CommandRouteAttribute),
            typeof(AttributeUsageAttribute));

        // Assert
        Assert.NotNull(attributeUsage);
        Assert.Equal(AttributeTargets.Method, attributeUsage!.ValidOn);
        Assert.False(attributeUsage.AllowMultiple);
    }

    /// <summary>
    /// Verifies that CommandRouteAttribute requires route in constructor.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void RouteIsRequiredInConstructor()
    {
        // Arrange & Act
        var attribute = new CommandRouteAttribute("create");

        // Assert
        Assert.Equal("create", attribute.Route);
    }

    /// <summary>
    /// Verifies that Route is read-only.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void RouteIsReadOnly()
    {
        // Arrange & Act
        var property = typeof(CommandRouteAttribute).GetProperty(nameof(CommandRouteAttribute.Route));

        // Assert
        Assert.NotNull(property);
        Assert.True(property!.CanRead);
        Assert.False(property.CanWrite);
    }
}
