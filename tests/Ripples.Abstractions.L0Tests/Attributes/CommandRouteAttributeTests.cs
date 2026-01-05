using System;
using System.Reflection;

using Allure.Xunit.Attributes;

using Mississippi.Ripples.Abstractions.Attributes;


namespace Mississippi.Ripples.Abstractions.L0Tests.Attributes;

/// <summary>
///     Tests for <see cref="CommandRouteAttribute" />.
/// </summary>
[AllureParentSuite("Ripples")]
[AllureSuite("Abstractions")]
[AllureSubSuite("CommandRouteAttribute")]
public sealed class CommandRouteAttributeTests
{
    /// <summary>
    ///     Verifies that CommandRouteAttribute throws when route is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenRouteIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CommandRouteAttribute(null!));
    }

    /// <summary>
    ///     Verifies that Route is read-only.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void RouteIsReadOnly()
    {
        // Arrange & Act
        PropertyInfo? property = typeof(CommandRouteAttribute).GetProperty(nameof(CommandRouteAttribute.Route));

        // Assert
        Assert.NotNull(property);
        Assert.True(property!.CanRead);
        Assert.False(property.CanWrite);
    }

    /// <summary>
    ///     Verifies that CommandRouteAttribute requires route in constructor.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void RouteIsRequiredInConstructor()
    {
        // Arrange & Act
        CommandRouteAttribute attribute = new("create");

        // Assert
        Assert.Equal("create", attribute.Route);
    }

    /// <summary>
    ///     Verifies that CommandRouteAttribute targets method only.
    /// </summary>
    [Fact]
    [AllureFeature("Attribute Usage")]
    public void TargetsMethodOnly()
    {
        // Arrange & Act
        AttributeUsageAttribute? attributeUsage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(
            typeof(CommandRouteAttribute),
            typeof(AttributeUsageAttribute));

        // Assert
        Assert.NotNull(attributeUsage);
        Assert.Equal(AttributeTargets.Method, attributeUsage!.ValidOn);
        Assert.False(attributeUsage.AllowMultiple);
    }
}