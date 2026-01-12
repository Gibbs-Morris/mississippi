using System;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.UxProjections.Abstractions.Attributes;


namespace Mississippi.EventSourcing.UxProjections.Abstractions.L0Tests.Attributes;

/// <summary>
///     Tests for <see cref="UxProjectionAttribute" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("UX Projections Abstractions")]
[AllureSubSuite("UxProjectionAttribute")]
public sealed class UxProjectionAttributeTests
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
            typeof(UxProjectionAttribute),
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
        UxProjectionAttribute attribute = new();

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
        UxProjectionAttribute attribute = new()
        {
            Authorize = "AdminPolicy",
        };

        // Assert
        Assert.Equal("AdminPolicy", attribute.Authorize);
    }

    /// <summary>
    ///     Verifies that parameterless constructor creates valid instance.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ParameterlessConstructorCreatesValidInstance()
    {
        // Arrange & Act
        UxProjectionAttribute attribute = new();

        // Assert
        Assert.NotNull(attribute);
        Assert.Null(attribute.Authorize);
        Assert.True(attribute.IsBatchEnabled);
        Assert.Null(attribute.Tags);
    }

    /// <summary>
    ///     Verifies that IsBatchEnabled defaults to true.
    /// </summary>
    [Fact]
    [AllureFeature("Properties")]
    public void IsBatchEnabledDefaultsToTrue()
    {
        // Arrange & Act
        UxProjectionAttribute attribute = new();

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
        UxProjectionAttribute attribute = new()
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
        UxProjectionAttribute attribute = new();

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
        UxProjectionAttribute attribute = new()
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