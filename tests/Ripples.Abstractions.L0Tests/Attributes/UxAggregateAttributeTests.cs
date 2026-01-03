namespace Mississippi.Ripples.Abstractions.L0Tests.Attributes;

using System;

using Allure.Xunit.Attributes;

using Xunit;

/// <summary>
/// Tests for <see cref="UxAggregateAttribute"/>.
/// </summary>
[AllureParentSuite("Ripples")]
[AllureSuite("Abstractions")]
[AllureSubSuite("UxAggregateAttribute")]
public sealed class UxAggregateAttributeTests
{
    /// <summary>
    /// Verifies that UxAggregateAttribute targets interface only.
    /// </summary>
    [Fact]
    [AllureFeature("Attribute Usage")]
    public void TargetsInterfaceOnly()
    {
        // Arrange & Act
        var attributeUsage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(
            typeof(UxAggregateAttribute),
            typeof(AttributeUsageAttribute));

        // Assert
        Assert.NotNull(attributeUsage);
        Assert.Equal(AttributeTargets.Interface, attributeUsage!.ValidOn);
        Assert.False(attributeUsage.AllowMultiple);
    }

    /// <summary>
    /// Verifies that UxAggregateAttribute requires route in constructor.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void RouteIsRequiredInConstructor()
    {
        // Arrange & Act
        var attribute = new UxAggregateAttribute("/api/orders");

        // Assert
        Assert.Equal("/api/orders", attribute.Route);
    }

    /// <summary>
    /// Verifies that Authorize defaults to null.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void AuthorizeDefaultsToNull()
    {
        // Arrange & Act
        var attribute = new UxAggregateAttribute("/api/orders");

        // Assert
        Assert.Null(attribute.Authorize);
    }

    /// <summary>
    /// Verifies that Authorize is settable.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void AuthorizeIsSettable()
    {
        // Arrange & Act
        var attribute = new UxAggregateAttribute("/api/orders")
        {
            Authorize = "AdminPolicy",
        };

        // Assert
        Assert.Equal("AdminPolicy", attribute.Authorize);
    }
}
