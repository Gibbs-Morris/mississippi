using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Diagnostics;
using Mississippi.Sdk.Runtime;

using Moq;

using Orleans.Hosting;

using SiloBuilderExtensions = Mississippi.Sdk.Runtime.SiloBuilderExtensions;


namespace MississippiTests.Sdk.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="Mississippi.Sdk.Runtime.SiloBuilderExtensions" />.
/// </summary>
public sealed class SiloBuilderExtensionsTests
{
    private static ISiloBuilder CreateSiloBuilder(
        IServiceCollection services
    )
    {
        Mock<ISiloBuilder> mock = new();
        mock.SetupGet(b => b.Services).Returns(services);
        return mock.Object;
    }

    /// <summary>
    ///     Calling UseMississippi twice should throw with the runtime duplicate attach code.
    /// </summary>
    [Fact]
    public void DuplicateAttachThrowsWithDiagnosticCode()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = CreateSiloBuilder(services);
        siloBuilder.UseMississippi(_ => { });

        // Act & Assert
        MississippiBuilderException ex =
            Assert.Throws<MississippiBuilderException>(() => siloBuilder.UseMississippi(_ => { }));
        Assert.Equal(MississippiDiagnosticCodes.RuntimeDuplicateAttach, ex.DiagnosticCode);
    }

    /// <summary>
    ///     UseMississippi with null configure delegate should throw.
    /// </summary>
    [Fact]
    public void NullConfigureDelegateThrows()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = CreateSiloBuilder(services);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => siloBuilder.UseMississippi(null!));
    }

    /// <summary>
    ///     UseMississippi with null silo builder should throw.
    /// </summary>
    [Fact]
    public void NullSiloBuilderThrows()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SiloBuilderExtensions.UseMississippi(null!, _ => { }));
    }

    /// <summary>
    ///     UseMississippi should invoke the configuration callback.
    /// </summary>
    [Fact]
    public void UseMississippiInvokesCallback()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = CreateSiloBuilder(services);
        bool callbackInvoked = false;

        // Act
        siloBuilder.UseMississippi(_ => callbackInvoked = true);

        // Assert
        Assert.True(callbackInvoked);
    }

    /// <summary>
    ///     UseMississippi should register the attach marker service.
    /// </summary>
    [Fact]
    public void UseMississippiRegistersMarkerService()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = CreateSiloBuilder(services);

        // Act
        siloBuilder.UseMississippi(_ => { });

        // Assert — marker must be present so duplicate detection works.
        Assert.Contains(services, d => d.Lifetime == ServiceLifetime.Singleton);
    }

    /// <summary>
    ///     UseMississippi should return the same silo builder for chaining.
    /// </summary>
    [Fact]
    public void UseMississippiReturnsSameSiloBuilder()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = CreateSiloBuilder(services);

        // Act
        ISiloBuilder result = siloBuilder.UseMississippi(_ => { });

        // Assert
        Assert.Same(siloBuilder, result);
    }
}