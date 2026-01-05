using System;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.UxProjections.Abstractions.Attributes;


namespace Mississippi.EventSourcing.UxProjections.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="GenerateProjectionApiAttribute" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("UX Projections Abstractions")]
[AllureSubSuite("GenerateProjectionApiAttribute")]
public sealed class GenerateProjectionApiAttributeTests
{
    /// <summary>
    ///     Verifies that attribute targets class only.
    /// </summary>
    [Fact]
    [AllureFeature("Attribute Targeting")]
    public void AttributeTargetsClassOnly()
    {
        // Arrange & Act
        AttributeUsageAttribute? usage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(
            typeof(GenerateProjectionApiAttribute),
            typeof(AttributeUsageAttribute));

        // Assert
        Assert.NotNull(usage);
        Assert.Equal(AttributeTargets.Class, usage.ValidOn);
        Assert.False(usage.Inherited);
    }

    /// <summary>
    ///     Verifies that Authorize defaults to null.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void AuthorizeDefaultsToNull()
    {
        // Arrange & Act
        GenerateProjectionApiAttribute attribute = new("users");

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
        GenerateProjectionApiAttribute attribute = new("users")
        {
            Authorize = "AdminPolicy",
        };

        // Assert
        Assert.Equal("AdminPolicy", attribute.Authorize);
    }

    /// <summary>
    ///     Verifies that constructor sets Route property.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorSetsRoute()
    {
        // Arrange & Act
        GenerateProjectionApiAttribute attribute = new("users");

        // Assert
        Assert.Equal("users", attribute.Route);
    }

    /// <summary>
    ///     Verifies that constructor throws when route is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenRouteIsNull()
    {
        // Arrange & Act
        ArgumentNullException ex =
            Assert.Throws<ArgumentNullException>(() => _ = new GenerateProjectionApiAttribute(null!));

        // Assert
        Assert.Equal("route", ex.ParamName);
    }

    /// <summary>
    ///     Verifies that IsBatchEnabled defaults to true.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void IsBatchEnabledDefaultsToTrue()
    {
        // Arrange & Act
        GenerateProjectionApiAttribute attribute = new("users");

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
        GenerateProjectionApiAttribute attribute = new("users")
        {
            IsBatchEnabled = false,
        };

        // Assert
        Assert.False(attribute.IsBatchEnabled);
    }

    /// <summary>
    ///     Verifies that Tags defaults to null.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void TagsDefaultsToNull()
    {
        // Arrange & Act
        GenerateProjectionApiAttribute attribute = new("users");

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
        GenerateProjectionApiAttribute attribute = new("users")
        {
            Tags = ["Users", "Projections"],
        };

        // Assert
        Assert.NotNull(attribute.Tags);
        Assert.Equal(2, attribute.Tags.Length);
        Assert.Contains("Users", attribute.Tags);
        Assert.Contains("Projections", attribute.Tags);
    }
}