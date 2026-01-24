using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Blazor.WebAssembly.ActionEffects;
using Mississippi.Inlet.Projection.Abstractions;
using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Inlet.Blazor.WebAssembly.L0Tests;

/// <summary>
///     Tests for <see cref="InletBlazorSignalRBuilder" /> behavior.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Blazor.WebAssembly")]
[AllureSuite("Core")]
[AllureSubSuite("InletBlazorSignalRBuilder")]
public sealed class InletBlazorSignalRBuilderTests
{
    /// <summary>
    ///     Sample projection DTO for testing.
    /// </summary>
    [ProjectionPath("/test/sample")]
    private sealed record SampleProjection(string Value);

    /// <summary>
    ///     Test projection fetcher for custom fetcher registration tests.
    /// </summary>
    private sealed class TestProjectionFetcher : IProjectionFetcher
    {
        /// <inheritdoc />
        public Task<ProjectionFetchResult?> FetchAsync(
            Type projectionType,
            string entityId,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult<ProjectionFetchResult?>(null);

        /// <inheritdoc />
        public Task<ProjectionFetchResult?> FetchAtVersionAsync(
            Type projectionType,
            string entityId,
            long version,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult<ProjectionFetchResult?>(null);
    }

    /// <summary>
    ///     AddProjectionFetcher should override ScanProjectionDtos auto fetcher behavior.
    /// </summary>
    [Fact]
    [AllureFeature("Builder Pattern")]
    public void AddProjectionFetcherOverridesScanProjectionDtosAutoFetcher()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act - Call ScanProjectionDtos first, then AddProjectionFetcher
        builder.ScanProjectionDtos(typeof(InletBlazorSignalRBuilderTests).Assembly);
        builder.AddProjectionFetcher<TestProjectionFetcher>();
        typeof(InletBlazorSignalRBuilder).GetMethod("Build", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(
            builder,
            null);

        // Assert - Custom fetcher should be registered, not auto fetcher
        ServiceDescriptor? fetcherDescriptor =
            services.FirstOrDefault(sd => sd.ServiceType == typeof(IProjectionFetcher));
        Assert.NotNull(fetcherDescriptor);
        Assert.Equal(typeof(TestProjectionFetcher), fetcherDescriptor.ImplementationType);
    }

    /// <summary>
    ///     AddProjectionFetcher should return the builder for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("Builder Pattern")]
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
    ///     Build should register auto projection fetcher when ScanProjectionDtos is called.
    /// </summary>
    [Fact]
    [AllureFeature("Build")]
    public void BuildRegistersAutoProjectionFetcherWhenScanProjectionDtosCalled()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);
        builder.ScanProjectionDtos(typeof(InletBlazorSignalRBuilderTests).Assembly);

        // Act
        typeof(InletBlazorSignalRBuilder).GetMethod("Build", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(
            builder,
            null);

        // Assert
        ServiceDescriptor? fetcherDescriptor =
            services.FirstOrDefault(sd => sd.ServiceType == typeof(IProjectionFetcher));
        Assert.NotNull(fetcherDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, fetcherDescriptor.Lifetime);

        // Factory function should be registered for AutoProjectionFetcher
        Assert.NotNull(fetcherDescriptor.ImplementationFactory);
    }

    /// <summary>
    ///     Build should register custom projection fetcher when AddProjectionFetcher is called.
    /// </summary>
    [Fact]
    [AllureFeature("Build")]
    public void BuildRegistersCustomProjectionFetcherWhenAddProjectionFetcherCalled()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);
        builder.AddProjectionFetcher<TestProjectionFetcher>();

        // Act
        typeof(InletBlazorSignalRBuilder).GetMethod("Build", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(
            builder,
            null);

        // Assert
        ServiceDescriptor? fetcherDescriptor =
            services.FirstOrDefault(sd => sd.ServiceType == typeof(IProjectionFetcher));
        Assert.NotNull(fetcherDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, fetcherDescriptor.Lifetime);
        Assert.Equal(typeof(TestProjectionFetcher), fetcherDescriptor.ImplementationType);
    }

    /// <summary>
    ///     Build should register InletSignalRActionEffect.
    /// </summary>
    [Fact]
    [AllureFeature("Build")]
    public void BuildRegistersInletSignalRActionEffect()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act
        typeof(InletBlazorSignalRBuilder).GetMethod("Build", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(
            builder,
            null);

        // Assert - Check that IActionEffect is registered for InletSignalRActionEffect
        ServiceDescriptor? effectDescriptor = services.FirstOrDefault(sd =>
            (sd.ServiceType == typeof(IActionEffect)) && (sd.ImplementationType == typeof(InletSignalRActionEffect)));
        Assert.NotNull(effectDescriptor);
    }

    /// <summary>
    ///     Build should register options singleton.
    /// </summary>
    [Fact]
    [AllureFeature("Build")]
    public void BuildRegistersOptionsSingleton()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);
        builder.WithHubPath("/custom/hub");

        // Act - Use reflection to invoke internal Build()
        typeof(InletBlazorSignalRBuilder).GetMethod("Build", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(
            builder,
            null);

        // Assert
        ServiceDescriptor? optionsDescriptor =
            services.FirstOrDefault(sd => sd.ServiceType == typeof(InletSignalRActionEffectOptions));
        Assert.NotNull(optionsDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, optionsDescriptor.Lifetime);
    }

    /// <summary>
    ///     Build should register projection DTO registry singleton.
    /// </summary>
    [Fact]
    [AllureFeature("Build")]
    public void BuildRegistersProjectionDtoRegistrySingleton()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act
        typeof(InletBlazorSignalRBuilder).GetMethod("Build", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(
            builder,
            null);

        // Assert
        ServiceDescriptor? registryDescriptor =
            services.FirstOrDefault(sd => sd.ServiceType == typeof(IProjectionDtoRegistry));
        Assert.NotNull(registryDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, registryDescriptor.Lifetime);
    }

    /// <summary>
    ///     Build should use route prefix when creating auto fetcher.
    /// </summary>
    [Fact]
    [AllureFeature("Build")]
    public void BuildUsesRoutePrefixWhenCreatingAutoFetcher()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);
        builder.ScanProjectionDtos(typeof(InletBlazorSignalRBuilderTests).Assembly);
        builder.WithRoutePrefix("/custom/prefix");

        // Act
        typeof(InletBlazorSignalRBuilder).GetMethod("Build", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(
            builder,
            null);

        // Assert - Should have a factory that uses the route prefix
        ServiceDescriptor? fetcherDescriptor =
            services.FirstOrDefault(sd => sd.ServiceType == typeof(IProjectionFetcher));
        Assert.NotNull(fetcherDescriptor);
        Assert.NotNull(fetcherDescriptor.ImplementationFactory);
    }

    /// <summary>
    ///     Build with scanned assemblies should scan all registered assemblies.
    /// </summary>
    [Fact]
    [AllureFeature("Build")]
    public void BuildWithScannedAssembliesScansAllRegisteredAssemblies()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddSingleton<HttpClient>(_ =>
#pragma warning disable IDISP014 // Test creates HttpClient directly for DI container - not using IHttpClientFactory
            new()
            {
                BaseAddress = new("http://localhost"),
            });
#pragma warning restore IDISP014
        InletBlazorSignalRBuilder builder = new(services);
        builder.ScanProjectionDtos(typeof(InletBlazorSignalRBuilderTests).Assembly);

        // Act
        typeof(InletBlazorSignalRBuilder).GetMethod("Build", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(
            builder,
            null);

        // Build and resolve the registry to verify scanning works
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionDtoRegistry registry = provider.GetRequiredService<IProjectionDtoRegistry>();

        // Assert - SampleProjection should be registered from assembly scan
        Type? dtoType = registry.GetDtoType("/test/sample");
        Assert.NotNull(dtoType);
        Assert.Equal(typeof(SampleProjection), dtoType);
    }

    /// <summary>
    ///     Build without fetcher configuration should not register projection fetcher.
    /// </summary>
    [Fact]
    [AllureFeature("Build")]
    public void BuildWithoutFetcherConfigurationDoesNotRegisterProjectionFetcher()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act
        typeof(InletBlazorSignalRBuilder).GetMethod("Build", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(
            builder,
            null);

        // Assert - No projection fetcher should be registered
        ServiceDescriptor? fetcherDescriptor =
            services.FirstOrDefault(sd => sd.ServiceType == typeof(IProjectionFetcher));
        Assert.Null(fetcherDescriptor);
    }

    /// <summary>
    ///     Constructor should succeed with valid services collection.
    /// </summary>
    [Fact]
    [AllureFeature("Construction")]
    public void ConstructorSucceedsWithValidServices()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        InletBlazorSignalRBuilder builder = new(services);

        // Assert
        Assert.NotNull(builder);
    }

    /// <summary>
    ///     Constructor should throw when services is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void ConstructorThrowsWhenServicesIsNull()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new InletBlazorSignalRBuilder(null!));
    }

    /// <summary>
    ///     Multiple ScanProjectionDtos calls should accumulate assemblies.
    /// </summary>
    [Fact]
    [AllureFeature("Builder Pattern")]
    public void MultipleScanProjectionDtosCallsAccumulateAssemblies()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);
        Assembly assembly1 = typeof(InletBlazorSignalRBuilderTests).Assembly;
        Assembly assembly2 = typeof(ServiceCollection).Assembly;

        // Act
        InletBlazorSignalRBuilder result = builder.ScanProjectionDtos(assembly1).ScanProjectionDtos(assembly2);

        // Assert - Both calls should chain correctly
        Assert.Same(builder, result);

        // Trigger build and verify no exceptions
        typeof(InletBlazorSignalRBuilder).GetMethod("Build", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(
            builder,
            null);
    }

    /// <summary>
    ///     ScanProjectionDtos should return the builder for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("Builder Pattern")]
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
    ///     ScanProjectionDtos should throw when assemblies is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void ScanProjectionDtosThrowsWhenAssembliesIsNull()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.ScanProjectionDtos(null!));
    }

    /// <summary>
    ///     WithHubPath should return the builder for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("Builder Pattern")]
    public void WithHubPathReturnsBuilderForChaining()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act
        InletBlazorSignalRBuilder result = builder.WithHubPath("/hubs/test");

        // Assert
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     WithHubPath should throw when hubPath is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void WithHubPathThrowsWhenHubPathIsNull()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => builder.WithHubPath(null!));
    }

    /// <summary>
    ///     WithHubPath should throw when hubPath is whitespace.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void WithHubPathThrowsWhenHubPathIsWhitespace()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithHubPath("   "));
    }

    /// <summary>
    ///     WithRoutePrefix should return the builder for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("Builder Pattern")]
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
    ///     WithRoutePrefix should throw when prefix is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void WithRoutePrefixThrowsWhenPrefixIsNull()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => builder.WithRoutePrefix(null!));
    }

    /// <summary>
    ///     WithRoutePrefix should throw when prefix is whitespace.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void WithRoutePrefixThrowsWhenPrefixIsWhitespace()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithRoutePrefix("   "));
    }
}