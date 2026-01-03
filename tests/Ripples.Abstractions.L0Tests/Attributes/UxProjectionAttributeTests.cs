namespace Mississippi.Ripples.Abstractions.L0Tests.Attributes;

using System;

using Allure.Xunit.Attributes;

using Xunit;

/// <summary>
/// Tests for <see cref="UxProjectionAttribute"/>.
/// </summary>
[AllureParentSuite("Ripples")]
[AllureSuite("Abstractions")]
[AllureSubSuite("UxProjectionAttribute")]
public sealed class UxProjectionAttributeTests
{
    /// <summary>
    /// Verifies that UxProjectionAttribute targets class only.
    /// </summary>
    [Fact]
    [AllureFeature("Attribute Usage")]
    public void TargetsClassOnly()
    {
        // Arrange & Act
        var attributeUsage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(
            typeof(UxProjectionAttribute),
            typeof(AttributeUsageAttribute));

        // Assert
        Assert.NotNull(attributeUsage);
        Assert.Equal(AttributeTargets.Class, attributeUsage!.ValidOn);
        Assert.False(attributeUsage.AllowMultiple);
    }

    /// <summary>
    /// Verifies that UxProjectionAttribute requires route in constructor.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void RouteIsRequiredInConstructor()
    {
        // Arrange & Act
        var attribute = new UxProjectionAttribute("/api/projections");

        // Assert
        Assert.Equal("/api/projections", attribute.Route);
    }

    /// <summary>
    /// Verifies that BrookName defaults to null.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void BrookNameDefaultsToNull()
    {
        // Arrange & Act
        var attribute = new UxProjectionAttribute("/api/projections");

        // Assert
        Assert.Null(attribute.BrookName);
    }

    /// <summary>
    /// Verifies that BrookName is settable.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void BrookNameIsSettable()
    {
        // Arrange & Act
        var attribute = new UxProjectionAttribute("/api/projections")
        {
            BrookName = "my-brook",
        };

        // Assert
        Assert.Equal("my-brook", attribute.BrookName);
    }

    /// <summary>
    /// Verifies that EnableBatch defaults to true.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void EnableBatchDefaultsToTrue()
    {
        // Arrange & Act
        var attribute = new UxProjectionAttribute("/api/projections");

        // Assert
        Assert.True(attribute.EnableBatch);
    }

    /// <summary>
    /// Verifies that EnableBatch is settable.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void EnableBatchIsSettable()
    {
        // Arrange & Act
        var attribute = new UxProjectionAttribute("/api/projections")
        {
            EnableBatch = false,
        };

        // Assert
        Assert.False(attribute.EnableBatch);
    }

    /// <summary>
    /// Verifies that Authorize defaults to null.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void AuthorizeDefaultsToNull()
    {
        // Arrange & Act
        var attribute = new UxProjectionAttribute("/api/projections");

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
        var attribute = new UxProjectionAttribute("/api/projections")
        {
            Authorize = "AdminPolicy",
        };

        // Assert
        Assert.Equal("AdminPolicy", attribute.Authorize);
    }

    /// <summary>
    /// Verifies that Tags defaults to null.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void TagsDefaultsToNull()
    {
        // Arrange & Act
        var attribute = new UxProjectionAttribute("/api/projections");

        // Assert
        Assert.Null(attribute.Tags);
    }

    /// <summary>
    /// Verifies that Tags is settable.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void TagsIsSettable()
    {
        // Arrange & Act
        var attribute = new UxProjectionAttribute("/api/projections")
        {
            Tags = ["Orders", "Admin"],
        };

        // Assert
        Assert.NotNull(attribute.Tags);
        Assert.Equal(2, attribute.Tags!.Length);
        Assert.Contains("Orders", attribute.Tags);
        Assert.Contains("Admin", attribute.Tags);
    }
}
