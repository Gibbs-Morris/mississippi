using System;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Moq;

using Orleans;


namespace Mississippi.EventSourcing.Aggregates.L0Tests;

/// <summary>
///     Tests for <see cref="AggregateGrainFactory" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Aggregates")]
[AllureSubSuite("Grain Factory")]
public class AggregateGrainFactoryTests
{
    /// <summary>
    ///     Constructor should throw when grain factory is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenGrainFactoryIsNull()
    {
        Mock<ILogger<AggregateGrainFactory>> loggerMock = new();
        Assert.Throws<ArgumentNullException>(() => new AggregateGrainFactory(null!, loggerMock.Object));
    }

    /// <summary>
    ///     Constructor should throw when logger is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenLoggerIsNull()
    {
        Mock<IGrainFactory> grainFactoryMock = new();
        Assert.Throws<ArgumentNullException>(() => new AggregateGrainFactory(grainFactoryMock.Object, null!));
    }

    /// <summary>
    ///     GetAggregate should resolve grain with entity ID.
    /// </summary>
    [Fact]
    public void GetAggregateResolvesGrainWithEntityId()
    {
        Mock<IGrainFactory> grainFactoryMock = new();
        Mock<ILogger<AggregateGrainFactory>> loggerMock = new();
        Mock<ITestAggregateGrain> grainMock = new();
        string entityId = "entity-123";
        string? capturedKey = null;
        grainFactoryMock.Setup(f => f.GetGrain<ITestAggregateGrain>(It.IsAny<string>(), It.IsAny<string?>()))
            .Callback<string, string?>((
                key,
                _
            ) => capturedKey = key)
            .Returns(grainMock.Object);
        AggregateGrainFactory factory = new(grainFactoryMock.Object, loggerMock.Object);
        ITestAggregateGrain result = factory.GetAggregate<ITestAggregateGrain>(entityId);
        Assert.Same(grainMock.Object, result);
        Assert.Equal(entityId, capturedKey);
    }

    /// <summary>
    ///     GetAggregate should throw when entity ID is null.
    /// </summary>
    [Fact]
    public void GetAggregateThrowsWhenEntityIdIsNull()
    {
        Mock<IGrainFactory> grainFactoryMock = new();
        Mock<ILogger<AggregateGrainFactory>> loggerMock = new();
        AggregateGrainFactory factory = new(grainFactoryMock.Object, loggerMock.Object);
        Assert.Throws<ArgumentNullException>(() => factory.GetAggregate<ITestAggregateGrain>(null!));
    }

    /// <summary>
    ///     GetAggregate should throw when entity ID is whitespace.
    /// </summary>
    [Fact]
    public void GetAggregateThrowsWhenEntityIdIsWhitespace()
    {
        Mock<IGrainFactory> grainFactoryMock = new();
        Mock<ILogger<AggregateGrainFactory>> loggerMock = new();
        AggregateGrainFactory factory = new(grainFactoryMock.Object, loggerMock.Object);
        Assert.Throws<ArgumentException>(() => factory.GetAggregate<ITestAggregateGrain>("   "));
    }

    /// <summary>
    ///     GetGenericAggregate should resolve grain with entity ID only.
    /// </summary>
    [Fact]
    public void GetGenericAggregateResolvesGrainWithEntityId()
    {
        Mock<IGrainFactory> grainFactoryMock = new();
        Mock<ILogger<AggregateGrainFactory>> loggerMock = new();
        Mock<IGenericAggregateGrain<AggregateGrainTestAggregate>> grainMock = new();
        string entityId = "entity-789";
        string? capturedKey = null;
        grainFactoryMock
            .Setup(f => f.GetGrain<IGenericAggregateGrain<AggregateGrainTestAggregate>>(
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Callback<string, string?>((
                key,
                _
            ) => capturedKey = key)
            .Returns(grainMock.Object);
        AggregateGrainFactory factory = new(grainFactoryMock.Object, loggerMock.Object);
        IGenericAggregateGrain<AggregateGrainTestAggregate> result =
            factory.GetGenericAggregate<AggregateGrainTestAggregate>(entityId);
        Assert.Same(grainMock.Object, result);
        Assert.Equal(entityId, capturedKey);
    }

    /// <summary>
    ///     GetGenericAggregate should throw when entity ID is null.
    /// </summary>
    [Fact]
    public void GetGenericAggregateThrowsWhenEntityIdIsNull()
    {
        Mock<IGrainFactory> grainFactoryMock = new();
        Mock<ILogger<AggregateGrainFactory>> loggerMock = new();
        AggregateGrainFactory factory = new(grainFactoryMock.Object, loggerMock.Object);
        Assert.Throws<ArgumentNullException>(() =>
            factory.GetGenericAggregate<AggregateGrainTestAggregate>((string)null!));
    }

    /// <summary>
    ///     GetGenericAggregate should throw when entity ID is whitespace.
    /// </summary>
    [Fact]
    public void GetGenericAggregateThrowsWhenEntityIdIsWhitespace()
    {
        Mock<IGrainFactory> grainFactoryMock = new();
        Mock<ILogger<AggregateGrainFactory>> loggerMock = new();
        AggregateGrainFactory factory = new(grainFactoryMock.Object, loggerMock.Object);
        Assert.Throws<ArgumentException>(() => factory.GetGenericAggregate<AggregateGrainTestAggregate>("   "));
    }

    /// <summary>
    ///     GetGenericAggregate with AggregateKey should resolve grain correctly.
    /// </summary>
    [Fact]
    public void GetGenericAggregateWithAggregateKeyResolvesGrain()
    {
        Mock<IGrainFactory> grainFactoryMock = new();
        Mock<ILogger<AggregateGrainFactory>> loggerMock = new();
        Mock<IGenericAggregateGrain<AggregateGrainTestAggregate>> grainMock = new();
        AggregateKey aggregateKey = new("entity-789");
        string? capturedKey = null;
        grainFactoryMock
            .Setup(f => f.GetGrain<IGenericAggregateGrain<AggregateGrainTestAggregate>>(
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Callback<string, string?>((
                key,
                _
            ) => capturedKey = key)
            .Returns(grainMock.Object);
        AggregateGrainFactory factory = new(grainFactoryMock.Object, loggerMock.Object);
        IGenericAggregateGrain<AggregateGrainTestAggregate> result =
            factory.GetGenericAggregate<AggregateGrainTestAggregate>(aggregateKey);
        Assert.Same(grainMock.Object, result);
        Assert.Equal("entity-789", capturedKey);
    }
}

/// <summary>
///     Test aggregate grain interface for testing.
/// </summary>
[Alias("Mississippi.EventSourcing.Aggregates.Tests.ITestAggregateGrain")]
internal interface ITestAggregateGrain : IGrainWithStringKey;