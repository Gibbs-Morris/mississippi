using Allure.Net.Commons;
using Allure.Xunit.Attributes;

using FluentAssertions;

using Mississippi.Ripples.Generators.Models;

using Xunit;


namespace Mississippi.Ripples.Generators.L0Tests;

/// <summary>
/// Tests for <see cref="AggregateInfo"/>.
/// </summary>
[AllureParentSuite("Mississippi")]
[AllureSuite("Ripples.Generators")]
[AllureSubSuite("AggregateInfo")]
public sealed class AggregateInfoTests
{
    /// <summary>
    /// Verifies that default values are set correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Default Values")]
    public void DefaultValuesAreSetCorrectly()
    {
        // Act
        AggregateInfo sut = new();

        // Assert
        sut.FullTypeName.Should().BeEmpty();
        sut.InterfaceName.Should().BeEmpty();
        sut.AggregateName.Should().BeEmpty();
        sut.Namespace.Should().BeEmpty();
        sut.Route.Should().BeEmpty();
        sut.Authorize.Should().BeNull();
        sut.Commands.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that all properties can be set.
    /// </summary>
    [Fact]
    [AllureFeature("Property Assignment")]
    public void AllPropertiesCanBeSet()
    {
        // Arrange
        CommandInfo[] commands = [new CommandInfo { MethodName = "CreateAsync", Route = "create" }];

        // Act
        AggregateInfo sut = new()
        {
            FullTypeName = "MyNamespace.IMyAggregateGrain",
            InterfaceName = "IMyAggregateGrain",
            AggregateName = "MyAggregate",
            Namespace = "MyNamespace",
            Route = "my-aggregate",
            Authorize = "AdminPolicy",
            Commands = commands,
        };

        // Assert
        sut.FullTypeName.Should().Be("MyNamespace.IMyAggregateGrain");
        sut.InterfaceName.Should().Be("IMyAggregateGrain");
        sut.AggregateName.Should().Be("MyAggregate");
        sut.Namespace.Should().Be("MyNamespace");
        sut.Route.Should().Be("my-aggregate");
        sut.Authorize.Should().Be("AdminPolicy");
        sut.Commands.Should().HaveCount(1);
        sut.Commands[0].MethodName.Should().Be("CreateAsync");
    }
}
