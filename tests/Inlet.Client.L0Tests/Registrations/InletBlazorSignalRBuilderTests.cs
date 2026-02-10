using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Client.L0Tests.Helpers;
using Mississippi.Reservoir;
using Mississippi.Reservoir.Abstractions.Builders;


namespace Mississippi.Inlet.Client.L0Tests.Registrations;

/// <summary>
///     Tests for <see cref="InletBlazorSignalRBuilder" />.
/// </summary>
public sealed class InletBlazorSignalRBuilderTests
{
    private static InletBlazorSignalRBuilder CreateBuilder(
        out ServiceCollection services
    )
    {
        services = new();
        TestMississippiClientBuilder mississippiBuilder = new(services);
        IReservoirBuilder reservoirBuilder = mississippiBuilder.AddReservoir();
        return new(reservoirBuilder);
    }

    /// <summary>
    ///     AddProjectionFetcher returns builder for chaining.
    /// </summary>
    [Fact]
    public void AddProjectionFetcherReturnsBuilderForChaining()
    {
        // Arrange
        InletBlazorSignalRBuilder builder = CreateBuilder(out ServiceCollection _);

        // Act
        InletBlazorSignalRBuilder result = builder.AddProjectionFetcher<TestProjectionFetcher>();

        // Assert
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     Build registers IHubConnectionProvider as scoped.
    /// </summary>
    [Fact]
    public void BuildRegistersHubConnectionProviderAsScoped()
    {
        // Arrange
        InletBlazorSignalRBuilder builder = CreateBuilder(out ServiceCollection services);

        // Act
        builder.Build();

        // Assert
        Assert.Contains(
            services,
            sd => (sd.ServiceType == typeof(IHubConnectionProvider)) && (sd.Lifetime == ServiceLifetime.Scoped));
    }

    /// <summary>
    ///     Build registers Lazy IInletStore as scoped.
    /// </summary>
    [Fact]
    public void BuildRegistersLazyInletStoreAsScoped()
    {
        // Arrange
        InletBlazorSignalRBuilder builder = CreateBuilder(out ServiceCollection services);

        // Act
        builder.Build();

        // Assert
        Assert.Contains(
            services,
            sd => (sd.ServiceType == typeof(Lazy<IInletStore>)) && (sd.Lifetime == ServiceLifetime.Scoped));
    }

    /// <summary>
    ///     Build registers options as singleton.
    /// </summary>
    [Fact]
    public void BuildRegistersOptionsAsSingleton()
    {
        // Arrange
        InletBlazorSignalRBuilder builder = CreateBuilder(out ServiceCollection services);
        builder.WithHubPath("/hubs/test");

        // Act
        builder.Build();

        // Assert
        Assert.Contains(
            services,
            sd => (sd.ServiceType == typeof(InletSignalRActionEffectOptions)) &&
                  (sd.Lifetime == ServiceLifetime.Singleton));
    }

    /// <summary>
    ///     Build registers IProjectionDtoRegistry as singleton.
    /// </summary>
    [Fact]
    public void BuildRegistersProjectionDtoRegistryAsSingleton()
    {
        // Arrange
        InletBlazorSignalRBuilder builder = CreateBuilder(out ServiceCollection services);

        // Act
        builder.Build();

        // Assert
        Assert.Contains(
            services,
            sd => (sd.ServiceType == typeof(IProjectionDtoRegistry)) && (sd.Lifetime == ServiceLifetime.Singleton));
    }

    /// <summary>
    ///     Build with custom fetcher registers it as scoped.
    /// </summary>
    [Fact]
    public void BuildWithCustomFetcherRegistersItAsScoped()
    {
        // Arrange
        InletBlazorSignalRBuilder builder = CreateBuilder(out ServiceCollection services);
        builder.AddProjectionFetcher<TestProjectionFetcher>();

        // Act
        builder.Build();

        // Assert
        Assert.Contains(
            services,
            sd => (sd.ServiceType == typeof(IProjectionFetcher)) &&
                  (sd.ImplementationType == typeof(TestProjectionFetcher)) &&
                  (sd.Lifetime == ServiceLifetime.Scoped));
    }

    /// <summary>
    ///     Build with ScanProjectionDtos registers AutoProjectionFetcher.
    /// </summary>
    [Fact]
    public void BuildWithScanProjectionDtosRegistersAutoProjectionFetcher()
    {
        // Arrange
        InletBlazorSignalRBuilder builder = CreateBuilder(out ServiceCollection services);
        builder.ScanProjectionDtos(typeof(InletBlazorSignalRBuilderTests).Assembly);

        // Act
        builder.Build();

        // Assert
        Assert.Contains(
            services,
            sd => (sd.ServiceType == typeof(IProjectionFetcher)) && (sd.Lifetime == ServiceLifetime.Scoped));
    }

    /// <summary>
    ///     Constructor accepts non-null services.
    /// </summary>
    [Fact]
    public void ConstructorAcceptsNonNullServices()
    {
        // Arrange
        InletBlazorSignalRBuilder builder = CreateBuilder(out ServiceCollection _);

        // Assert
        Assert.NotNull(builder);
    }

    /// <summary>
    ///     Constructor throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenServicesIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new InletBlazorSignalRBuilder(null!));
    }

    /// <summary>
    ///     ScanProjectionDtos returns builder for chaining.
    /// </summary>
    [Fact]
    public void ScanProjectionDtosReturnsBuilderForChaining()
    {
        // Arrange
        InletBlazorSignalRBuilder builder = CreateBuilder(out ServiceCollection _);

        // Act
        InletBlazorSignalRBuilder result = builder.ScanProjectionDtos(typeof(InletBlazorSignalRBuilderTests).Assembly);

        // Assert
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     ScanProjectionDtos throws ArgumentNullException when assemblies is null.
    /// </summary>
    [Fact]
    public void ScanProjectionDtosThrowsWhenAssembliesIsNull()
    {
        // Arrange
        InletBlazorSignalRBuilder builder = CreateBuilder(out ServiceCollection _);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.ScanProjectionDtos(null!));
    }

    /// <summary>
    ///     WithHubPath returns builder for chaining.
    /// </summary>
    [Fact]
    public void WithHubPathReturnsBuilderForChaining()
    {
        // Arrange
        InletBlazorSignalRBuilder builder = CreateBuilder(out ServiceCollection _);

        // Act
        InletBlazorSignalRBuilder result = builder.WithHubPath("/hubs/inlet");

        // Assert
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     WithHubPath throws ArgumentException when hubPath is null.
    /// </summary>
    [Fact]
    public void WithHubPathThrowsWhenHubPathIsNull()
    {
        // Arrange
        InletBlazorSignalRBuilder builder = CreateBuilder(out ServiceCollection _);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithHubPath(null!));
    }

    /// <summary>
    ///     WithHubPath throws ArgumentException when hubPath is whitespace.
    /// </summary>
    [Fact]
    public void WithHubPathThrowsWhenHubPathIsWhitespace()
    {
        // Arrange
        InletBlazorSignalRBuilder builder = CreateBuilder(out ServiceCollection _);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithHubPath("   "));
    }

    /// <summary>
    ///     WithRoutePrefix returns builder for chaining.
    /// </summary>
    [Fact]
    public void WithRoutePrefixReturnsBuilderForChaining()
    {
        // Arrange
        InletBlazorSignalRBuilder builder = CreateBuilder(out ServiceCollection _);

        // Act
        InletBlazorSignalRBuilder result = builder.WithRoutePrefix("/api/projections");

        // Assert
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     WithRoutePrefix throws ArgumentException when prefix is null.
    /// </summary>
    [Fact]
    public void WithRoutePrefixThrowsWhenPrefixIsNull()
    {
        // Arrange
        InletBlazorSignalRBuilder builder = CreateBuilder(out ServiceCollection _);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithRoutePrefix(null!));
    }

    /// <summary>
    ///     WithRoutePrefix throws ArgumentException when prefix is whitespace.
    /// </summary>
    [Fact]
    public void WithRoutePrefixThrowsWhenPrefixIsWhitespace()
    {
        // Arrange
        InletBlazorSignalRBuilder builder = CreateBuilder(out ServiceCollection _);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithRoutePrefix(string.Empty));
    }
}