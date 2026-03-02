using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Mississippi.Common.Builders.Abstractions;
using Mississippi.Common.Builders.Client;
using Mississippi.Common.Builders.Gateway;
using Mississippi.Common.Builders.Runtime;


namespace Mississippi.Common.Builders.Core.L0Tests;

/// <summary>
///     L0 tests for <see cref="MississippiBuilderHostExtensions" />.
/// </summary>
public sealed class MississippiBuilderHostExtensionsTests
{
    private sealed class TestClientService;

    [Fact]
    public void UseMississippiShouldMergeClientBuilderServices()
    {
        HostApplicationBuilder hostBuilder = Host.CreateApplicationBuilder();
        ClientBuilder builder = ClientBuilder.Create();
        builder.Services.AddSingleton<TestClientService>();
        hostBuilder.UseMississippi(builder);
        bool containsService =
            hostBuilder.Services.Any(descriptor => descriptor.ServiceType == typeof(TestClientService));
        Assert.True(containsService);
    }

    [Fact]
    public void UseMississippiShouldSucceedForRuntimeBuilder()
    {
        HostApplicationBuilder hostBuilder = Host.CreateApplicationBuilder();
        RuntimeBuilder builder = RuntimeBuilder.Create();
        HostApplicationBuilder result = hostBuilder.UseMississippi(builder);
        Assert.Same(hostBuilder, result);
    }

    [Fact]
    public void UseMississippiShouldSucceedWhenGatewayAuthorizationConfigured()
    {
        HostApplicationBuilder hostBuilder = Host.CreateApplicationBuilder();
        GatewayBuilder builder = GatewayBuilder.Create();
        builder.ConfigureAuthorization();
        HostApplicationBuilder result = hostBuilder.UseMississippi(builder);
        Assert.Same(hostBuilder, result);
    }

    [Fact]
    public void UseMississippiShouldThrowOnSecondAttachment()
    {
        HostApplicationBuilder hostBuilder = Host.CreateApplicationBuilder();
        ClientBuilder builder = ClientBuilder.Create();
        hostBuilder.UseMississippi(builder);
        Assert.Throws<InvalidOperationException>(() => hostBuilder.UseMississippi(builder));
    }

    [Fact]
    public void UseMississippiShouldThrowWhenGatewayAuthorizationNotConfigured()
    {
        HostApplicationBuilder hostBuilder = Host.CreateApplicationBuilder();
        GatewayBuilder builder = GatewayBuilder.Create();
        BuilderValidationException exception =
            Assert.Throws<BuilderValidationException>(() => hostBuilder.UseMississippi(builder));
        BuilderDiagnostic diagnostic = Assert.Single(exception.Diagnostics);
        Assert.Equal("Gateway.AuthorizationNotConfigured", diagnostic.ErrorCode);
    }

    [Fact]
    public void UseMississippiShouldThrowWhenHostBuilderIsNull()
    {
        HostApplicationBuilder? hostBuilder = null;
        ClientBuilder builder = ClientBuilder.Create();
        Assert.Throws<ArgumentNullException>(() => hostBuilder!.UseMississippi(builder));
    }
}