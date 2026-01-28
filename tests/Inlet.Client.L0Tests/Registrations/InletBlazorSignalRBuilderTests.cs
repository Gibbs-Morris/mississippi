using System;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Client.L0Tests.Helpers;


namespace Mississippi.Inlet.Client.L0Tests.Registrations;

/// <summary>
///     Tests for <see cref="InletBlazorSignalRBuilder" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet")]
[AllureSuite("Registrations")]
[AllureSubSuite("InletBlazorSignalRBuilder")]
public sealed class InletBlazorSignalRBuilderTests
{
    /// <summary>
    ///     AddProjectionFetcher returns builder for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("ProjectionFetcher")]
    public void AddProjectionFetcherReturnsBuilderForChaining()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act
        InletBlazorSignalRBuilder result = builder.AddProjectionFetcher<TestProjectionFetcher>();

        // Assert
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     Build registers IHubConnectionProvider as scoped.
    /// </summary>
    [Fact]
    [AllureFeature("Build")]
    public void BuildRegistersHubConnectionProviderAsScoped()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

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
    [AllureFeature("Build")]
    public void BuildRegistersLazyInletStoreAsScoped()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

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
    [AllureFeature("Build")]
    public void BuildRegistersOptionsAsSingleton()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);
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
    [AllureFeature("Build")]
    public void BuildRegistersProjectionDtoRegistryAsSingleton()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

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
    [AllureFeature("Build")]
    public void BuildWithCustomFetcherRegistersItAsScoped()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);
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
    [AllureFeature("Build")]
    public void BuildWithScanProjectionDtosRegistersAutoProjectionFetcher()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);
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
    [AllureFeature("Constructor")]
    public void ConstructorAcceptsNonNullServices()
    {
        // Arrange
        ServiceCollection services = new();

        // Act (should not throw)
        InletBlazorSignalRBuilder builder = new(services);

        // Assert
        Assert.NotNull(builder);
    }

    /// <summary>
    ///     Constructor throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenServicesIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new InletBlazorSignalRBuilder(null!));
    }

    /// <summary>
    ///     ScanProjectionDtos returns builder for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("Scanning")]
    public void ScanProjectionDtosReturnsBuilderForChaining()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act
        InletBlazorSignalRBuilder result = builder.ScanProjectionDtos(typeof(InletBlazorSignalRBuilderTests).Assembly);

        // Assert
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     ScanProjectionDtos throws ArgumentNullException when assemblies is null.
    /// </summary>
    [Fact]
    [AllureFeature("Scanning")]
    public void ScanProjectionDtosThrowsWhenAssembliesIsNull()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.ScanProjectionDtos(null!));
    }

    /// <summary>
    ///     WithHubPath returns builder for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    public void WithHubPathReturnsBuilderForChaining()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act
        InletBlazorSignalRBuilder result = builder.WithHubPath("/hubs/inlet");

        // Assert
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     WithHubPath throws ArgumentException when hubPath is null.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    public void WithHubPathThrowsWhenHubPathIsNull()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithHubPath(null!));
    }

    /// <summary>
    ///     WithHubPath throws ArgumentException when hubPath is whitespace.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    public void WithHubPathThrowsWhenHubPathIsWhitespace()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithHubPath("   "));
    }

    /// <summary>
    ///     WithRoutePrefix returns builder for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    public void WithRoutePrefixReturnsBuilderForChaining()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act
        InletBlazorSignalRBuilder result = builder.WithRoutePrefix("/api/projections");

        // Assert
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     WithRoutePrefix throws ArgumentException when prefix is null.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    public void WithRoutePrefixThrowsWhenPrefixIsNull()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithRoutePrefix(null!));
    }

    /// <summary>
    ///     WithRoutePrefix throws ArgumentException when prefix is whitespace.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    public void WithRoutePrefixThrowsWhenPrefixIsWhitespace()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithRoutePrefix(string.Empty));
    }
}