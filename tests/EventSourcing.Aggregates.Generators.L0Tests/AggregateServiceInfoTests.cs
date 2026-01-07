using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Aggregates.Generators.Models;


namespace Mississippi.EventSourcing.Aggregates.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="AggregateServiceInfo" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Aggregates Generators")]
[AllureSubSuite("AggregateServiceInfo")]
public sealed class AggregateServiceInfoTests
{
    /// <summary>
    ///     Verifies that the model can be instantiated with default values.
    /// </summary>
    [Fact]
    [AllureFeature("Model Instantiation")]
    public void CanInstantiateWithDefaults()
    {
        // Act
        AggregateServiceInfo sut = new();

        // Assert
        Assert.NotNull(sut);
        Assert.Equal(string.Empty, sut.FullTypeName);
        Assert.Equal(string.Empty, sut.TypeName);
        Assert.Equal(string.Empty, sut.ServiceName);
        Assert.Equal(string.Empty, sut.Namespace);
        Assert.Equal(string.Empty, sut.Route);
        Assert.False(sut.GenerateApi);
        Assert.False(sut.IsInternal);
        Assert.Null(sut.Authorize);
        Assert.NotNull(sut.Commands);
        Assert.Empty(sut.Commands);
    }

    /// <summary>
    ///     Verifies that properties can be set.
    /// </summary>
    [Fact]
    [AllureFeature("Model Properties")]
    public void CanSetProperties()
    {
        // Arrange
        AggregateServiceInfo sut = new();

        // Act
        sut.FullTypeName = "Sample.UserAggregate";
        sut.TypeName = "UserAggregate";
        sut.ServiceName = "User";
        sut.Namespace = "Sample";
        sut.Route = "users";
        sut.GenerateApi = true;
        sut.IsInternal = true;
        sut.Authorize = "Admin";

        // Assert
        Assert.Equal("Sample.UserAggregate", sut.FullTypeName);
        Assert.Equal("UserAggregate", sut.TypeName);
        Assert.Equal("User", sut.ServiceName);
        Assert.Equal("Sample", sut.Namespace);
        Assert.Equal("users", sut.Route);
        Assert.True(sut.GenerateApi);
        Assert.True(sut.IsInternal);
        Assert.Equal("Admin", sut.Authorize);
    }

    /// <summary>
    ///     Verifies that commands list is mutable.
    /// </summary>
    [Fact]
    [AllureFeature("Model Commands")]
    public void CommandsListIsMutable()
    {
        // Arrange
        AggregateServiceInfo sut = new();
        CommandInfo command = new()
        {
            FullTypeName = "Sample.CreateUser",
            TypeName = "CreateUser",
            Namespace = "Sample",
            MethodName = "CreateUser",
        };

        // Act
        sut.Commands.Add(command);

        // Assert
        Assert.Single(sut.Commands);
        Assert.Same(command, sut.Commands[0]);
    }
}