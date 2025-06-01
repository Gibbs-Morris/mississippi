using System;
using System.Reflection;
using FluentAssertions;
using Mississippi.EventSourcing.Abstractions.Attributes;
using Xunit;

namespace Mississippi.EventSourcing.Abstractions.Tests.Attributes;

public class EventNameAttributeTests
{
    [Fact]
    public void Constructor_WithValidParameters_SetsProperties()
    {
        // Arrange
        const string appName = "APP";
        const string moduleName = "MODULE";
        const string name = "EVENT";
        const int version = 2;

        // Act
        var attribute = new EventNameAttribute(appName, moduleName, name, version);

        // Assert
        attribute.AppName.Should().Be(appName);
        attribute.ModuleName.Should().Be(moduleName);
        attribute.Name.Should().Be(name);
        attribute.Version.Should().Be(version);
    }

    [Fact]
    public void Constructor_WithDefaultVersion_SetsVersionToOne()
    {
        // Arrange
        const string appName = "APP";
        const string moduleName = "MODULE";
        const string name = "EVENT";

        // Act
        var attribute = new EventNameAttribute(appName, moduleName, name);

        // Assert
        attribute.Version.Should().Be(1);
    }

    [Theory]
    [InlineData(null, "MODULE", "EVENT", "appName")]
    [InlineData("", "MODULE", "EVENT", "appName")]
    [InlineData("  ", "MODULE", "EVENT", "appName")]
    [InlineData("APP", null, "EVENT", "moduleName")]
    [InlineData("APP", "", "EVENT", "moduleName")]
    [InlineData("APP", "  ", "EVENT", "moduleName")]
    [InlineData("APP", "MODULE", null, "name")]
    [InlineData("APP", "MODULE", "", "name")]
    [InlineData("APP", "MODULE", "  ", "name")]
    public void Constructor_WithNullOrWhiteSpaceParameter_ThrowsArgumentException(string appName, string moduleName, string name, string paramName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new EventNameAttribute(appName, moduleName, name));
        exception.ParamName.Should().Be(paramName);
        exception.Message.Should().Contain("Value cannot be null or whitespace");
    }

    [Theory]
    [InlineData("app", "MODULE", "EVENT", "appName")]
    [InlineData("APP123!", "MODULE", "EVENT", "appName")]
    [InlineData("APP", "module", "EVENT", "moduleName")]
    [InlineData("APP", "MODULE123!", "EVENT", "moduleName")]
    [InlineData("APP", "MODULE", "event", "name")]
    [InlineData("APP", "MODULE", "EVENT123!", "name")]
    public void Constructor_WithInvalidParameterFormat_ThrowsArgumentException(string appName, string moduleName, string name, string paramName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new EventNameAttribute(appName, moduleName, name));
        exception.ParamName.Should().Be(paramName);
        exception.Message.Should().Contain("Value must contain only uppercase alphanumeric characters");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithNonPositiveVersion_ThrowsArgumentException(int version)
    {
        // Arrange
        const string appName = "APP";
        const string moduleName = "MODULE";
        const string name = "EVENT";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new EventNameAttribute(appName, moduleName, name, version));
        exception.ParamName.Should().Be("version");
        exception.Message.Should().Contain("Version must be a positive integer");
    }

    [Fact]
    public void GetEventName_ReturnsCorrectlyFormattedName()
    {
        // Arrange
        const string appName = "APP";
        const string moduleName = "MODULE";
        const string name = "EVENT";
        const int version = 2;
        var attribute = new EventNameAttribute(appName, moduleName, name, version);

        // Act
        var eventName = attribute.GetEventName();

        // Assert
        eventName.Should().Be("APP.MODULE.EVENTV2");
    }

    [Fact]
    public void AttributeUsage_ShouldBeAppliedToClassesOnly()
    {
        // Arrange
        var attributeType = typeof(EventNameAttribute);

        // Act
        var attributeUsage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage.ValidOn.Should().Be(AttributeTargets.Class);
        attributeUsage.Inherited.Should().BeFalse();
    }
} 