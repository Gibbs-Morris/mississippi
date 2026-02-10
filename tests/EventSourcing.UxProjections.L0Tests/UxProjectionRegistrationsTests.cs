using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Builders;
using Mississippi.EventSourcing.UxProjections.Abstractions;


namespace Mississippi.EventSourcing.UxProjections.L0Tests;

/// <summary>
///     Tests for <see cref="UxProjectionRegistrations" />.
/// </summary>
public sealed class UxProjectionRegistrationsTests
{
    private sealed class TestMississippiSiloBuilder : IMississippiSiloBuilder
    {
        private readonly IServiceCollection services;

        public TestMississippiSiloBuilder(
            IServiceCollection services
        )
        {
            ArgumentNullException.ThrowIfNull(services);
            this.services = services;
        }

        public IMississippiSiloBuilder ConfigureOptions<TOptions>(
            Action<TOptions> configure
        )
            where TOptions : class
        {
            services.Configure(configure);
            return this;
        }

        public IMississippiSiloBuilder ConfigureServices(
            Action<IServiceCollection> configure
        )
        {
            configure(services);
            return this;
        }
    }

    /// <summary>
    ///     AddUxProjections should not register duplicate when called multiple times.
    /// </summary>
    [Fact]
    public void AddUxProjectionsDoesNotDuplicateRegistration()
    {
        // Arrange
        ServiceCollection services = [];
        TestMississippiSiloBuilder builder = new(services);

        // Act
        builder.AddUxProjections();
        builder.AddUxProjections();

        // Assert
        int factoryCount = services.Count(sd => sd.ServiceType == typeof(IUxProjectionGrainFactory));
        Assert.Equal(1, factoryCount);
    }

    /// <summary>
    ///     AddUxProjections should register IUxProjectionGrainFactory.
    /// </summary>
    [Fact]
    public void AddUxProjectionsRegistersFactory()
    {
        // Arrange
        ServiceCollection services = [];
        TestMississippiSiloBuilder builder = new(services);

        // Act
        builder.AddUxProjections();

        // Assert
        ServiceDescriptor? factoryDescriptor = Assert.Single(
            services,
            sd => sd.ServiceType == typeof(IUxProjectionGrainFactory));
        Assert.Equal(ServiceLifetime.Singleton, factoryDescriptor.Lifetime);
        Assert.Equal(typeof(UxProjectionGrainFactory), factoryDescriptor.ImplementationType);
    }

    /// <summary>
    ///     AddUxProjections should return service collection for chaining.
    /// </summary>
    [Fact]
    public void AddUxProjectionsReturnsServicesForChaining()
    {
        // Arrange
        TestMississippiSiloBuilder builder = new(new ServiceCollection());

        // Act
        IMississippiSiloBuilder result = builder.AddUxProjections();

        // Assert
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     AddUxProjections should throw when services is null.
    /// </summary>
    [Fact]
    public void AddUxProjectionsThrowsWhenServicesIsNull()
    {
        // Arrange
        IMississippiSiloBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddUxProjections());
    }
}