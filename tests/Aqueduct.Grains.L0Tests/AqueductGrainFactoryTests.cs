using System;


using Microsoft.Extensions.Logging;

using Mississippi.Aqueduct.Abstractions.Grains;
using Mississippi.Aqueduct.Abstractions.Keys;

using NSubstitute;

using Orleans;


namespace Mississippi.Aqueduct.Grains.L0Tests;

/// <summary>
///     Tests for <see cref="AqueductGrainFactory" /> in the Grains assembly.
/// </summary>
public sealed class AqueductGrainFactoryTests
{
    /// <summary>
    ///     Constructor should throw when grainFactory is null.
    /// </summary>
    [Fact]
        public void ConstructorThrowsWhenGrainFactoryIsNull()
    {
        // Arrange
        ILogger<AqueductGrainFactory> logger = Substitute.For<ILogger<AqueductGrainFactory>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AqueductGrainFactory(null!, logger));
    }

    /// <summary>
    ///     Constructor should throw when logger is null.
    /// </summary>
    [Fact]
        public void ConstructorThrowsWhenLoggerIsNull()
    {
        // Arrange
        IGrainFactory grainFactory = Substitute.For<IGrainFactory>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AqueductGrainFactory(grainFactory, null!));
    }

    /// <summary>
    ///     GetClientGrain with key should return grain from factory.
    /// </summary>
    [Fact]
        public void GetClientGrainWithKeyReturnsGrain()
    {
        // Arrange
        IGrainFactory grainFactory = Substitute.For<IGrainFactory>();
        ILogger<AqueductGrainFactory> logger = Substitute.For<ILogger<AqueductGrainFactory>>();
        ISignalRClientGrain expectedGrain = Substitute.For<ISignalRClientGrain>();
        SignalRClientKey clientKey = new("TestHub", "conn-123");
        grainFactory.GetGrain<ISignalRClientGrain>(clientKey).Returns(expectedGrain);
        AqueductGrainFactory factory = new(grainFactory, logger);

        // Act
        ISignalRClientGrain result = factory.GetClientGrain(clientKey);

        // Assert
        Assert.Same(expectedGrain, result);
        grainFactory.Received(1).GetGrain<ISignalRClientGrain>(clientKey);
    }

    /// <summary>
    ///     GetClientGrain with strings should return grain from factory.
    /// </summary>
    [Fact]
        public void GetClientGrainWithStringsReturnsGrain()
    {
        // Arrange
        IGrainFactory grainFactory = Substitute.For<IGrainFactory>();
        ILogger<AqueductGrainFactory> logger = Substitute.For<ILogger<AqueductGrainFactory>>();
        ISignalRClientGrain expectedGrain = Substitute.For<ISignalRClientGrain>();
        SignalRClientKey expectedKey = new("TestHub", "conn-456");
        grainFactory.GetGrain<ISignalRClientGrain>(expectedKey).Returns(expectedGrain);
        AqueductGrainFactory factory = new(grainFactory, logger);

        // Act
        ISignalRClientGrain result = factory.GetClientGrain("TestHub", "conn-456");

        // Assert
        Assert.Same(expectedGrain, result);
    }

    /// <summary>
    ///     GetGroupGrain with key should return grain from factory.
    /// </summary>
    [Fact]
        public void GetGroupGrainWithKeyReturnsGrain()
    {
        // Arrange
        IGrainFactory grainFactory = Substitute.For<IGrainFactory>();
        ILogger<AqueductGrainFactory> logger = Substitute.For<ILogger<AqueductGrainFactory>>();
        ISignalRGroupGrain expectedGrain = Substitute.For<ISignalRGroupGrain>();
        SignalRGroupKey groupKey = new("TestHub", "Group1");
        grainFactory.GetGrain<ISignalRGroupGrain>(groupKey).Returns(expectedGrain);
        AqueductGrainFactory factory = new(grainFactory, logger);

        // Act
        ISignalRGroupGrain result = factory.GetGroupGrain(groupKey);

        // Assert
        Assert.Same(expectedGrain, result);
        grainFactory.Received(1).GetGrain<ISignalRGroupGrain>(groupKey);
    }

    /// <summary>
    ///     GetGroupGrain with strings should return grain from factory.
    /// </summary>
    [Fact]
        public void GetGroupGrainWithStringsReturnsGrain()
    {
        // Arrange
        IGrainFactory grainFactory = Substitute.For<IGrainFactory>();
        ILogger<AqueductGrainFactory> logger = Substitute.For<ILogger<AqueductGrainFactory>>();
        ISignalRGroupGrain expectedGrain = Substitute.For<ISignalRGroupGrain>();
        SignalRGroupKey expectedKey = new("TestHub", "Group2");
        grainFactory.GetGrain<ISignalRGroupGrain>(expectedKey).Returns(expectedGrain);
        AqueductGrainFactory factory = new(grainFactory, logger);

        // Act
        ISignalRGroupGrain result = factory.GetGroupGrain("TestHub", "Group2");

        // Assert
        Assert.Same(expectedGrain, result);
    }

    /// <summary>
    ///     GetServerDirectoryGrain with default should return grain from factory.
    /// </summary>
    [Fact]
        public void GetServerDirectoryGrainDefaultReturnsGrain()
    {
        // Arrange
        IGrainFactory grainFactory = Substitute.For<IGrainFactory>();
        ILogger<AqueductGrainFactory> logger = Substitute.For<ILogger<AqueductGrainFactory>>();
        ISignalRServerDirectoryGrain expectedGrain = Substitute.For<ISignalRServerDirectoryGrain>();
        grainFactory.GetGrain<ISignalRServerDirectoryGrain>(SignalRServerDirectoryKey.Default).Returns(expectedGrain);
        AqueductGrainFactory factory = new(grainFactory, logger);

        // Act
        ISignalRServerDirectoryGrain result = factory.GetServerDirectoryGrain();

        // Assert
        Assert.Same(expectedGrain, result);
    }

    /// <summary>
    ///     GetServerDirectoryGrain with key should return grain from factory.
    /// </summary>
    [Fact]
        public void GetServerDirectoryGrainWithKeyReturnsGrain()
    {
        // Arrange
        IGrainFactory grainFactory = Substitute.For<IGrainFactory>();
        ILogger<AqueductGrainFactory> logger = Substitute.For<ILogger<AqueductGrainFactory>>();
        ISignalRServerDirectoryGrain expectedGrain = Substitute.For<ISignalRServerDirectoryGrain>();
        SignalRServerDirectoryKey directoryKey = new("custom-directory");
        grainFactory.GetGrain<ISignalRServerDirectoryGrain>(directoryKey).Returns(expectedGrain);
        AqueductGrainFactory factory = new(grainFactory, logger);

        // Act
        ISignalRServerDirectoryGrain result = factory.GetServerDirectoryGrain(directoryKey);

        // Assert
        Assert.Same(expectedGrain, result);
        grainFactory.Received(1).GetGrain<ISignalRServerDirectoryGrain>(directoryKey);
    }
}