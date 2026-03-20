using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Common.Abstractions.Diagnostics;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Inlet.Gateway;
using Mississippi.Inlet.Runtime.Abstractions;
using Mississippi.Sdk.Gateway;


namespace MississippiTests.Sdk.Gateway.L0Tests;

/// <summary>
///     Tests for <see cref="MississippiGatewayBuilder" /> and
///     <see cref="WebApplicationBuilderExtensions" />.
/// </summary>
public sealed class MississippiGatewayBuilderTests
{
    private sealed class TestHub : Hub;

    /// <summary>
    ///     AddAqueduct should be idempotent — second call is a no-op.
    /// </summary>
    [Fact]
    public void AddAqueductIsIdempotent()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Act — call twice with different options; first call wins.
        builder.UseMississippi(gateway =>
        {
            gateway.AddAqueduct<TestHub>(options => options.StreamProviderName = "First");
            gateway.AddAqueduct<TestHub>(options => options.StreamProviderName = "Second");
        });

        // Assert — first call's options survive.
        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        AqueductOptions options = provider.GetRequiredService<IOptions<AqueductOptions>>().Value;
        Assert.Equal("First", options.StreamProviderName);
    }

    /// <summary>
    ///     AddAqueduct should register Aqueduct services and configure options.
    /// </summary>
    [Fact]
    public void AddAqueductRegistersServicesAndOptions()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Act
        builder.UseMississippi(gateway =>
            gateway.AddAqueduct<TestHub>(options => options.StreamProviderName = "TestStream"));

        // Assert — AqueductOptions should be configured.
        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        AqueductOptions options = provider.GetRequiredService<IOptions<AqueductOptions>>().Value;
        Assert.Equal("TestStream", options.StreamProviderName);
    }

    /// <summary>
    ///     AddAqueduct with null configure should throw.
    /// </summary>
    [Fact]
    public void AddAqueductWithNullConfigureThrows()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Act & Assert
        builder.UseMississippi(gateway =>
            Assert.Throws<ArgumentNullException>(() => gateway.AddAqueduct<TestHub>(null!)));
    }

    /// <summary>
    ///     AddInletGateway with options should configure InletServerOptions.
    /// </summary>
    [Fact]
    public void AddInletGatewayConfiguresOptions()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Act
        builder.UseMississippi(gateway => gateway.AddInletGateway(options =>
            options.GeneratedApiAuthorization.DefaultPolicy = "test-policy"));

        // Assert — InletServerOptions should be configured.
        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        InletServerOptions options = provider.GetRequiredService<IOptions<InletServerOptions>>().Value;
        Assert.Equal("test-policy", options.GeneratedApiAuthorization.DefaultPolicy);
    }

    /// <summary>
    ///     AddInletGateway with default options should not throw.
    /// </summary>
    [Fact]
    public void AddInletGatewayDefaultDoesNotThrow()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Act
        builder.UseMississippi(gateway => gateway.AddInletGateway());

        // Assert — no exception expected; verify builder completes.
        Assert.NotNull(builder);
    }

    /// <summary>
    ///     AddInletGateway should be idempotent — second call is a no-op.
    /// </summary>
    [Fact]
    public void AddInletGatewayIsIdempotent()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Act — call twice; should not throw.
        builder.UseMississippi(gateway =>
        {
            gateway.AddInletGateway();
            gateway.AddInletGateway();
        });

        // Assert — completed without exception.
        Assert.NotNull(builder);
    }

    /// <summary>
    ///     AddInletGateway should register aggregate and UX projection infrastructure.
    /// </summary>
    [Fact]
    public void AddInletGatewayRegistersInfrastructure()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Act
        builder.UseMississippi(gateway => gateway.AddInletGateway());

        // Assert — key infrastructure service descriptors should be registered.
        Assert.Contains(builder.Services, d => d.ServiceType == typeof(IAggregateGrainFactory));
        Assert.Contains(builder.Services, d => d.ServiceType == typeof(IUxProjectionGrainFactory));
        Assert.Contains(builder.Services, d => d.ServiceType == typeof(IProjectionBrookRegistry));
    }

    /// <summary>
    ///     AddInletGateway should register MVC authorization setup.
    /// </summary>
    [Fact]
    public void AddInletGatewayRegistersMvcAuthSetup()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Act
        builder.UseMississippi(gateway => gateway.AddInletGateway());

        // Assert — MvcOptions configure action should be registered.
        Assert.Contains(
            builder.Services,
            d => (d.ServiceType == typeof(IConfigureOptions<MvcOptions>)) && (d.Lifetime == ServiceLifetime.Transient));
    }

    /// <summary>
    ///     AddInletGateway with null options should throw.
    /// </summary>
    [Fact]
    public void AddInletGatewayWithNullOptionsThrows()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Act & Assert
        builder.UseMississippi(gateway => Assert.Throws<ArgumentNullException>(() => gateway.AddInletGateway(null!)));
    }

    /// <summary>
    ///     AddInletGateway with options callback should not throw.
    /// </summary>
    [Fact]
    public void AddInletGatewayWithOptionsDoesNotThrow()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Act
        builder.UseMississippi(gateway => gateway.AddInletGateway(_ => { }));

        // Assert — no exception expected; verify builder completes.
        Assert.NotNull(builder);
    }

    /// <summary>
    ///     AddJsonSerialization should be idempotent.
    /// </summary>
    [Fact]
    public void AddJsonSerializationIsIdempotent()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Act — call twice; should not throw.
        builder.UseMississippi(gateway =>
        {
            gateway.AddJsonSerialization();
            gateway.AddJsonSerialization();
        });

        // Assert — completed without exception.
        Assert.NotNull(builder);
    }

    /// <summary>
    ///     Calling UseMississippi twice should throw with duplicate attach code.
    /// </summary>
    [Fact]
    public void DuplicateAttachThrowsWithDiagnosticCode()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.UseMississippi(_ => { });

        // Act & Assert
        MississippiBuilderException ex =
            Assert.Throws<MississippiBuilderException>(() => builder.UseMississippi(_ => { }));
        Assert.Equal(MississippiDiagnosticCodes.DuplicateAttach, ex.DiagnosticCode);
    }

    /// <summary>
    ///     Empty builder with no registrations should validate successfully.
    /// </summary>
    [Fact]
    public void EmptyBuilderValidatesSuccessfully()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Act — empty gateway should not throw.
        builder.UseMississippi(_ => { });

        // Assert — completed without exception.
        Assert.NotNull(builder);
    }

    /// <summary>
    ///     UseMississippi with null builder should throw.
    /// </summary>
    [Fact]
    public void NullBuilderThrows()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => WebApplicationBuilderExtensions.UseMississippi(null!, _ => { }));
    }

    /// <summary>
    ///     UseMississippi with null configure delegate should throw.
    /// </summary>
    [Fact]
    public void NullConfigureDelegateThrows()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.UseMississippi(null!));
    }

    /// <summary>
    ///     RegisterDomainMappers should execute the callback with the service collection.
    /// </summary>
    [Fact]
    public void RegisterDomainMappersExecutesCallback()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        bool callbackExecuted = false;

        // Act
        builder.UseMississippi(gateway => gateway.RegisterDomainMappers(_ => callbackExecuted = true));

        // Assert
        Assert.True(callbackExecuted);
    }

    /// <summary>
    ///     RegisterDomainMappers with null callback should throw.
    /// </summary>
    [Fact]
    public void RegisterDomainMappersWithNullCallbackThrows()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Act & Assert
        builder.UseMississippi(gateway =>
            Assert.Throws<ArgumentNullException>(() => gateway.RegisterDomainMappers(null!)));
    }

    /// <summary>
    ///     UseMississippi should invoke the configuration callback.
    /// </summary>
    [Fact]
    public void UseMississippiInvokesCallback()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        bool callbackInvoked = false;

        // Act
        builder.UseMississippi(_ => callbackInvoked = true);

        // Assert
        Assert.True(callbackInvoked);
    }

    /// <summary>
    ///     UseMississippi should return the same builder for chaining.
    /// </summary>
    [Fact]
    public void UseMississippiReturnsSameBuilder()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Act
        WebApplicationBuilder result = builder.UseMississippi(_ => { });

        // Assert
        Assert.Same(builder, result);
    }
}