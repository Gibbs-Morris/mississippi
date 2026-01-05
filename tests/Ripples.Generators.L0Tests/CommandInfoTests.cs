using Allure.Xunit.Attributes;

using FluentAssertions;

using Mississippi.Ripples.Generators.Models;


namespace Mississippi.Ripples.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="CommandInfo" />.
/// </summary>
[AllureParentSuite("Mississippi")]
[AllureSuite("Ripples.Generators")]
[AllureSubSuite("CommandInfo")]
public sealed class CommandInfoTests
{
    /// <summary>
    ///     Verifies that all properties can be set.
    /// </summary>
    [Fact]
    [AllureFeature("Property Assignment")]
    public void AllPropertiesCanBeSet()
    {
        // Act
        CommandInfo sut = new()
        {
            MethodName = "CreateAsync",
            Route = "create",
            ParameterType = "CreateOrderCommand",
            ParameterName = "command",
            ReturnType = "Task<OperationResult<OrderId>>",
            XmlDocSummary = "Creates a new order.",
        };

        // Assert
        sut.MethodName.Should().Be("CreateAsync");
        sut.Route.Should().Be("create");
        sut.ParameterType.Should().Be("CreateOrderCommand");
        sut.ParameterName.Should().Be("command");
        sut.ReturnType.Should().Be("Task<OperationResult<OrderId>>");
        sut.HasParameter.Should().BeTrue();
        sut.XmlDocSummary.Should().Be("Creates a new order.");
    }

    /// <summary>
    ///     Verifies that default values are set correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Default Values")]
    public void DefaultValuesAreSetCorrectly()
    {
        // Act
        CommandInfo sut = new();

        // Assert
        sut.MethodName.Should().BeEmpty();
        sut.Route.Should().BeEmpty();
        sut.ParameterType.Should().BeNull();
        sut.ParameterName.Should().BeNull();
        sut.ReturnType.Should().Be("Task<OperationResult>");
        sut.HasParameter.Should().BeFalse();
        sut.XmlDocSummary.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that HasParameter returns false when ParameterType is null.
    /// </summary>
    [Fact]
    [AllureFeature("Computed Properties")]
    public void HasParameterReturnsFalseWhenParameterTypeIsNull()
    {
        // Arrange
        CommandInfo sut = new()
        {
            MethodName = "ArchiveAsync",
            Route = "archive",
        };

        // Act & Assert
        sut.HasParameter.Should().BeFalse();
    }

    /// <summary>
    ///     Verifies that HasParameter returns true when ParameterType is set.
    /// </summary>
    [Fact]
    [AllureFeature("Computed Properties")]
    public void HasParameterReturnsTrueWhenParameterTypeIsSet()
    {
        // Arrange
        CommandInfo sut = new()
        {
            ParameterType = "CreateOrderCommand",
            ParameterName = "command",
        };

        // Act & Assert
        sut.HasParameter.Should().BeTrue();
    }
}