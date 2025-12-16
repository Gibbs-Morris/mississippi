using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


namespace Mississippi.EventSourcing.Snapshots.Abstractions.L0Tests;

/// <summary>
///     Tests for snapshot abstractions and registration helpers.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Snapshots Abstractions")]
[AllureSubSuite("Snapshot Abstractions")]
public sealed class SnapshotAbstractionsTests
{
    private sealed class FakeOptions
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class FakeProvider : ISnapshotStorageProvider
    {
        public string Format => "fake";

        public Task DeleteAllAsync(
            SnapshotStreamKey streamKey,
            CancellationToken cancellationToken = default
        ) =>
            Task.CompletedTask;

        public Task DeleteAsync(
            SnapshotKey snapshotKey,
            CancellationToken cancellationToken = default
        ) =>
            Task.CompletedTask;

        public Task PruneAsync(
            SnapshotStreamKey streamKey,
            IReadOnlyCollection<int> retainModuli,
            CancellationToken cancellationToken = default
        ) =>
            Task.CompletedTask;

        public Task<SnapshotEnvelope?> ReadAsync(
            SnapshotKey snapshotKey,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult<SnapshotEnvelope?>(null);

        public Task WriteAsync(
            SnapshotKey snapshotKey,
            SnapshotEnvelope snapshot,
            CancellationToken cancellationToken = default
        ) =>
            Task.CompletedTask;
    }

    /// <summary>
    ///     Ensures the registration helper wires both reader and writer roles for a provider.
    /// </summary>
    [Fact]
    public void RegisterSnapshotStorageProviderShouldRegisterReaderAndWriter()
    {
        ServiceCollection services = new();
        services.RegisterSnapshotStorageProvider<FakeProvider>();
        using ServiceProvider provider = services.BuildServiceProvider();
        ISnapshotStorageReader reader = provider.GetRequiredService<ISnapshotStorageReader>();
        ISnapshotStorageWriter writer = provider.GetRequiredService<ISnapshotStorageWriter>();
        Assert.IsType<FakeProvider>(reader);
        Assert.IsType<FakeProvider>(writer);
    }

    /// <summary>
    ///     Ensures the overload binding configuration section wires options and registration.
    /// </summary>
    [Fact]
    public void RegisterSnapshotStorageProviderWithConfigurationShouldBind()
    {
        ConfigurationBuilder builder = new();
        builder.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Value"] = "section",
            });
        IConfiguration config = builder.Build();
        ServiceCollection services = new();
        services.RegisterSnapshotStorageProvider<FakeProvider, FakeOptions>(config);
        using ServiceProvider provider = services.BuildServiceProvider();
        FakeOptions options = provider.GetRequiredService<IOptions<FakeOptions>>().Value;
        Assert.Equal("section", options.Value);
    }

    /// <summary>
    ///     Ensures the overload binding configuration delegates wires options and registration.
    /// </summary>
    [Fact]
    public void RegisterSnapshotStorageProviderWithOptionsDelegateShouldConfigure()
    {
        ServiceCollection services = new();
        services.RegisterSnapshotStorageProvider<FakeProvider, FakeOptions>(o => o.Value = "ok");
        using ServiceProvider provider = services.BuildServiceProvider();
        FakeOptions options = provider.GetRequiredService<IOptions<FakeOptions>>().Value;
        Assert.Equal("ok", options.Value);
    }

    /// <summary>
    ///     Ensures the envelope defaults are empty and can be initialized.
    /// </summary>
    [Fact]
    public void SnapshotEnvelopeShouldRoundTripData()
    {
        SnapshotEnvelope envelope = new();
        Assert.Equal(ImmutableArray<byte>.Empty, envelope.Data);
        Assert.Equal(string.Empty, envelope.DataContentType);
        ImmutableArray<byte> data = ImmutableArray.Create((byte)1, (byte)2);
        SnapshotEnvelope populated = new()
        {
            Data = data,
            DataContentType = "application/octet-stream",
        };
        Assert.Equal(data, populated.Data);
        Assert.Equal("application/octet-stream", populated.DataContentType);
    }
}