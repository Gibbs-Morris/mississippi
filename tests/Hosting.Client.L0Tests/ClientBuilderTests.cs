using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Hosting.Abstractions;
using Mississippi.Hosting.Client;
using Mississippi.Reservoir.Abstractions;


namespace MississippiTests.Hosting.Client.L0Tests;

/// <summary>
///     Verifies client composition and its terminal lifecycle.
/// </summary>
public sealed class ClientBuilderTests
{
    /// <summary>
    ///     Configuration cannot continue after terminal attachment.
    /// </summary>
    [Fact]
    public void CompletedBuilderRejectsFurtherComposition()
    {
        ClientBuilder client = new([]);
        client.Complete();
        bool invoked = false;
        BuilderValidationException exception = Assert.Throws<BuilderValidationException>(() =>
            client.Reservoir(_ => invoked = true));
        Assert.False(invoked);
        Assert.Equal("MSB002", Assert.Single(exception.Diagnostics).Code);
        Assert.True(client.Services.IsReadOnly);
        Assert.Throws<InvalidOperationException>(() => client.Services.AddSingleton(TimeProvider.System));
    }

    /// <summary>
    ///     An empty client composition implements the common contract and is ready to attach.
    /// </summary>
    [Fact]
    public void EmptyBuilderIsValidWithoutChangingServices()
    {
        ServiceCollection services = [];
        ClientBuilder client = new(services);
        Assert.IsType<IMississippiBuilder>(client, false);
        Assert.Same(services, client.Services);
        Assert.Empty(client.Validate());
        Assert.Empty(client.Validate());
        Assert.Empty(services);
    }

    /// <summary>
    ///     Nested composition reuses its builder and produces a resolvable store.
    /// </summary>
    [Fact]
    public void ReservoirComposesAndReusesTheNestedBuilder()
    {
        ClientBuilder client = new([]);
        IReservoirBuilder? first = null;
        IReservoirBuilder? second = null;
        Assert.Same(client, client.Reservoir(reservoir => first = reservoir));
        Assert.Same(client, client.Reservoir(reservoir => second = reservoir));
        Assert.NotNull(first);
        Assert.Same(first, second);
        Assert.Empty(client.Validate());
        using ServiceProvider provider = client.Services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IStore>());
    }

    /// <summary>
    ///     Null callbacks fail before any registrations are staged.
    /// </summary>
    [Fact]
    public void ReservoirRejectsNullCallback()
    {
        ClientBuilder client = new([]);
        Assert.Throws<ArgumentNullException>(() => client.Reservoir(null!));
        Assert.Empty(client.Services);
    }

    /// <summary>
    ///     Validation snapshots distinguish a consumed builder from a duplicate host.
    /// </summary>
    [Fact]
    public void ValidationReportsConsumedBuilderWithRemediation()
    {
        ClientBuilder client = new([]);
        IReadOnlyList<BuilderDiagnostic> before = client.Validate();
        client.Complete();
        BuilderDiagnostic diagnostic = Assert.Single(client.Validate());
        Assert.Empty(before);
        Assert.Equal("MSB002", diagnostic.Code);
        Assert.Contains("already been attached", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("UseMississippi", diagnostic.Remediation, StringComparison.Ordinal);
    }
}