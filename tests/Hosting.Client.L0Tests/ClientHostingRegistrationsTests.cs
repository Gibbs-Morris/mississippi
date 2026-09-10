using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Hosting.Abstractions;
using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Hosting.Client.L0Tests;

/// <summary>
///     Verifies terminal attachment against a host's service collection without a browser runtime.
/// </summary>
public sealed class ClientHostingRegistrationsTests
{
    private static WebAssemblyHostBuilder CreateHost(
        IServiceCollection services
    )
    {
        WebAssemblyHostBuilder builder =
            (WebAssemblyHostBuilder)RuntimeHelpers.GetUninitializedObject(typeof(WebAssemblyHostBuilder));
        FieldInfo? servicesField = typeof(WebAssemblyHostBuilder).GetField(
            "<Services>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(servicesField);
        servicesField.SetValue(builder, services);
        return builder;
    }

    /// <summary>
    ///     The canonical flow commits registrations and keeps host-provided defaults.
    /// </summary>
    [Fact]
    public void AttachCommitsCompositionAndPreservesExistingDescriptors()
    {
        ServiceCollection services = [];
        services.AddSingleton(TimeProvider.System);
        ServiceDescriptor existing = Assert.Single(services);
        WebAssemblyHostBuilder host = CreateHost(services);
        ClientBuilder? captured = null;
        WebAssemblyHostBuilder result = host.UseMississippi(client =>
        {
            captured = client;
            Assert.NotSame(services, client.Services);
            client.Reservoir(_ => { });
            Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IStore));
        });
        Assert.Same(host, result);
        Assert.Same(services, host.Services);
        Assert.Same(existing, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(TimeProvider)));
        Assert.NotNull(captured);
        Assert.True(captured.Services.IsReadOnly);
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IStore>());
    }

    /// <summary>
    ///     A failed callback leaves the host unchanged and permits a corrected attempt.
    /// </summary>
    [Fact]
    public void CallbackFailureDoesNotPartiallyAttach()
    {
        ServiceCollection services = [];
        services.AddSingleton(TimeProvider.System);
        ServiceDescriptor[] original = services.ToArray();
        WebAssemblyHostBuilder host = CreateHost(services);
        InvalidOperationException expected = new("Configuration failed.");
        Assert.Same(
            expected,
            Assert.Throws<InvalidOperationException>(() => host.UseMississippi(client =>
            {
                client.Reservoir(_ => { });
                throw expected;
            })));
        Assert.Equal(original, services);
        host.UseMississippi(client => client.Reservoir(_ => { }));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IStore));
    }

    /// <summary>
    ///     A captured Reservoir builder rejects callbacks before application code can run after attachment.
    /// </summary>
    [Fact]
    public void CapturedReservoirRejectsFeatureCallbackAfterAttachment()
    {
        ServiceCollection services = [];
        WebAssemblyHostBuilder host = CreateHost(services);
        IReservoirBuilder? captured = null;
        host.UseMississippi(client => client.Reservoir(reservoir => captured = reservoir));
        Assert.NotNull(captured);
        ServiceDescriptor[] original = services.ToArray();
        bool invoked = false;
        Assert.Throws<InvalidOperationException>(() =>
            captured.AddFeatureState<ClientLifecycleState>(_ => invoked = true));
        Assert.False(invoked);
        Assert.Equal(original, services);
    }

    /// <summary>
    ///     Even an otherwise idempotent feature registration rejects a completed builder.
    /// </summary>
    [Fact]
    public void CapturedReservoirRejectsRepeatedFeatureAfterAttachment()
    {
        WebAssemblyHostBuilder host = CreateHost(new ServiceCollection());
        IReservoirBuilder? captured = null;
        host.UseMississippi(client => client.Reservoir(reservoir =>
        {
            captured = reservoir;
            reservoir.AddFeatureState<ClientLifecycleState>();
        }));
        Assert.NotNull(captured);
        Assert.Throws<InvalidOperationException>(() => captured.AddFeatureState<ClientLifecycleState>());
    }

    /// <summary>
    ///     Duplicate attachment is rejected before invoking user code or altering the host.
    /// </summary>
    [Fact]
    public void DuplicateAttachReportsStableDiagnosticBeforeCallback()
    {
        ServiceCollection services = [];
        WebAssemblyHostBuilder host = CreateHost(services);
        host.UseMississippi(_ => { });
        ServiceDescriptor[] original = services.ToArray();
        bool invoked = false;
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            host.UseMississippi(_ => invoked = true));
        Assert.False(invoked);
        Assert.Equal(original, services);
        BuilderDiagnostic diagnostic = Assert.Single(exception.Diagnostics);
        Assert.Equal("MSB001", diagnostic.Code);
        Assert.Contains("one UseMississippi", diagnostic.Remediation, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Failed client configuration closes all escaped scopes while permitting a fresh retry.
    /// </summary>
    [Fact]
    public void FailedCallbackClosesCapturedBuildersAndServices()
    {
        ServiceCollection services = [];
        WebAssemblyHostBuilder host = CreateHost(services);
        ClientBuilder? capturedClient = null;
        IReservoirBuilder? capturedReservoir = null;
        IServiceCollection? capturedServices = null;
        Assert.Throws<InvalidOperationException>(() => host.UseMississippi(client =>
        {
            capturedClient = client;
            capturedServices = client.Services;
            client.Reservoir(reservoir => capturedReservoir = reservoir);
            throw new InvalidOperationException("Configuration failed.");
        }));
        Assert.NotNull(capturedClient);
        Assert.NotNull(capturedReservoir);
        Assert.NotNull(capturedServices);
        Assert.True(capturedServices.IsReadOnly);
        Assert.Empty(services);
        bool invoked = false;
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            capturedClient.Reservoir(_ => invoked = true));
        Assert.Equal("MSB003", Assert.Single(exception.Diagnostics).Code);
        Assert.False(invoked);
        Assert.Throws<InvalidOperationException>(() =>
            capturedReservoir.AddFeatureState<ClientLifecycleState>(_ => invoked = true));
        Assert.False(invoked);
        Assert.Throws<InvalidOperationException>(() => capturedServices.AddSingleton(TimeProvider.System));
        host.UseMississippi(client => client.Reservoir(reservoir => reservoir.AddFeatureState<ClientLifecycleState>()));
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IStore>());
    }

    /// <summary>
    ///     Feature-scoped builders cannot silently accept registrations after their callback has returned.
    /// </summary>
    [Fact]
    public void FeatureScopeBecomesReadOnlyWhenItsCallbackReturns()
    {
        WebAssemblyHostBuilder host = CreateHost(new ServiceCollection());
        IReservoirFeatureBuilder<ClientLifecycleState>? captured = null;
        host.UseMississippi(client => client.Reservoir(reservoir =>
        {
            reservoir.AddFeatureState<ClientLifecycleState>(feature => captured = feature);
            Assert.NotNull(captured);
            Assert.True(captured.Services.IsReadOnly);
        }));
        Assert.NotNull(captured);
        Assert.Throws<InvalidOperationException>(() => captured.Services.AddSingleton(TimeProvider.System));
    }

    /// <summary>
    ///     Invalid composition never reaches the host.
    /// </summary>
    [Fact]
    public void InvalidBuilderDoesNotCommitStagedRegistrations()
    {
        ServiceCollection services = [];
        WebAssemblyHostBuilder host = CreateHost(services);
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            host.UseMississippi(client =>
            {
                client.Reservoir(_ => { });
                client.Complete();
            }));
        Assert.Equal("MSB002", Assert.Single(exception.Diagnostics).Code);
        Assert.Empty(services);
        Assert.Same(host, host.UseMississippi(_ => { }));
    }

    /// <summary>
    ///     Recursive terminal attachment is also a duplicate and rolls back the reservation.
    /// </summary>
    [Fact]
    public void ReentrantAttachIsRejectedAndCanBeRetried()
    {
        ServiceCollection services = [];
        WebAssemblyHostBuilder host = CreateHost(services);
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            host.UseMississippi(_ => host.UseMississippi(_ => { })));
        Assert.Equal("MSB001", Assert.Single(exception.Diagnostics).Code);
        Assert.Empty(services);
        Assert.Same(host, host.UseMississippi(_ => { }));
    }

    /// <summary>
    ///     Attachment is tracked per host rather than across independent hosts.
    /// </summary>
    [Fact]
    public void SeparateHostsCanEachAttach()
    {
        WebAssemblyHostBuilder first = CreateHost(new ServiceCollection());
        WebAssemblyHostBuilder second = CreateHost(new ServiceCollection());
        Assert.Same(first, first.UseMississippi(_ => { }));
        Assert.Same(second, second.UseMississippi(_ => { }));
    }

    /// <summary>
    ///     Invalid arguments do not reserve attachment.
    /// </summary>
    [Fact]
    public void UseMississippiRejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => ClientHostingRegistrations.UseMississippi(null!, _ => { }));
        ServiceCollection services = [];
        WebAssemblyHostBuilder host = CreateHost(services);
        Assert.Throws<ArgumentNullException>(() => host.UseMississippi(null!));
        Assert.Empty(services);
    }
}