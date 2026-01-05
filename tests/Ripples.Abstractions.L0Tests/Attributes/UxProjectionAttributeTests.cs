using System;

using Allure.Xunit.Attributes;

using Mississippi.Ripples.Abstractions.Attributes;


namespace Mississippi.Ripples.Abstractions.L0Tests.Attributes;

/// <summary>
///     Tests for <see cref="UxProjectionAttribute" />.
/// </summary>
[AllureParentSuite("Ripples")]
[AllureSuite("Abstractions")]
[AllureSubSuite("UxProjectionAttribute")]
public sealed class UxProjectionAttributeTests
{
    /// <summary>
    ///     Verifies that Authorize defaults to null.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void AuthorizeDefaultsToNull()
    {
        // Arrange & Act
        UxProjectionAttribute attribute = new("/api/projections");

        // Assert
        Assert.Null(attribute.Authorize);
    }

    /// <summary>
    ///     Verifies that Authorize is settable.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void AuthorizeIsSettable()
    {
        // Arrange & Act
        UxProjectionAttribute attribute = new("/api/projections")
        {
            Authorize = "AdminPolicy",
        };

        // Assert
        Assert.Equal("AdminPolicy", attribute.Authorize);
    }

    /// <summary>
    ///     Verifies that BrookName defaults to null.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void BrookNameDefaultsToNull()
    {
        // Arrange & Act
        UxProjectionAttribute attribute = new("/api/projections");

        // Assert
        Assert.Null(attribute.BrookName);
    }

    /// <summary>
    ///     Verifies that BrookName is settable.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void BrookNameIsSettable()
    {
        // Arrange & Act
        UxProjectionAttribute attribute = new("/api/projections")
        {
            BrookName = "my-brook",
        };

        // Assert
        Assert.Equal("my-brook", attribute.BrookName);
    }

    /// <summary>
    ///     Verifies that UxProjectionAttribute throws when route is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenRouteIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UxProjectionAttribute(null!));
    }

    /// <summary>
    ///     Verifies that IsBatchEnabled defaults to true.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void IsBatchEnabledDefaultsToTrue()
    {
        // Arrange & Act
        UxProjectionAttribute attribute = new("/api/projections");

        // Assert
        Assert.True(attribute.IsBatchEnabled);
    }

    /// <summary>
    ///     Verifies that IsBatchEnabled is settable.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void IsBatchEnabledIsSettable()
    {
        // Arrange & Act
        UxProjectionAttribute attribute = new("/api/projections")
        {
            IsBatchEnabled = false,
        };

        // Assert
        Assert.False(attribute.IsBatchEnabled);
    }

    /// <summary>
    ///     Verifies that UxProjectionAttribute requires route in constructor.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void RouteIsRequiredInConstructor()
    {
        // Arrange & Act
        UxProjectionAttribute attribute = new("/api/projections");

        // Assert
        Assert.Equal("/api/projections", attribute.Route);
    }

    /// <summary>
    ///     Verifies that Tags defaults to null.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void TagsDefaultsToNull()
    {
        // Arrange & Act
        UxProjectionAttribute attribute = new("/api/projections");

        // Assert
        Assert.Null(attribute.Tags);
    }

    /// <summary>
    ///     Verifies that Tags is settable.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void TagsIsSettable()
    {
        // Arrange & Act
        UxProjectionAttribute attribute = new("/api/projections")
        {
            Tags = ["Orders", "Admin"],
        };

        // Assert
        Assert.NotNull(attribute.Tags);
        Assert.Equal(2, attribute.Tags!.Length);
        Assert.Contains("Orders", attribute.Tags);
        Assert.Contains("Admin", attribute.Tags);
    }

    /// <summary>
    ///     Verifies that UxProjectionAttribute targets class only.
    /// </summary>
    [Fact]
    [AllureFeature("Attribute Usage")]
    public void TargetsClassOnly()
    {
        // Arrange & Act
        AttributeUsageAttribute? attributeUsage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(
            typeof(UxProjectionAttribute),
            typeof(AttributeUsageAttribute));

        // Assert
        Assert.NotNull(attributeUsage);
        Assert.Equal(AttributeTargets.Class, attributeUsage!.ValidOn);
        Assert.False(attributeUsage.AllowMultiple);
    }
}