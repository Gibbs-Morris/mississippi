using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Storage;


namespace Mississippi.EventSourcing.Abstractions.Tests;

/// <summary>
///     Tests for <see cref="BrookStorageProviderHelpers" /> registration helpers.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Brooks Abstractions")]
[AllureSubSuite("Brook Storage Provider Helpers")]
public sealed class BrookStorageProviderHelpersTests
{
    private sealed class FakeOptions
    {
        public string? Value { get; set; }
    }

    private sealed class FakeProvider : IBrookStorageProvider
    {
        public string Format => "fake";

        public Task<BrookPosition> AppendEventsAsync(
            BrookKey brookId,
            IReadOnlyList<BrookEvent> events,
            BrookPosition? expectedVersion = null,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(new BrookPosition(events.Count));

        public Task<BrookPosition> ReadCursorPositionAsync(
            BrookKey brookId,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(new BrookPosition(0));

        public async IAsyncEnumerable<BrookEvent> ReadEventsAsync(
            BrookRangeKey brookRange,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    /// <summary>
    ///     Verifies that registering a brook storage provider wires reader and writer services.
    /// </summary>
    [Fact]
    public void RegisterBrookStorageProviderRegistersReaderAndWriter()
    {
        ServiceCollection services = new();
        services.RegisterBrookStorageProvider<FakeProvider>();
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<IBrookStorageReader>());
        Assert.NotNull(provider.GetRequiredService<IBrookStorageWriter>());
    }

    /// <summary>
    ///     Verifies that configuration binding populates provider options.
    /// </summary>
    [Fact]
    public void RegisterBrookStorageProviderWithConfigurationBindsOptions()
    {
        Dictionary<string, string?> map = new()
        {
            ["Value"] = "y",
        };
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(map).Build();
        ServiceCollection services = new();
        services.RegisterBrookStorageProvider<FakeProvider, FakeOptions>(config);
        using ServiceProvider provider = services.BuildServiceProvider();
        FakeOptions opts = provider.GetRequiredService<IOptions<FakeOptions>>().Value;
        Assert.Equal("y", opts.Value);
    }

    /// <summary>
    ///     Verifies that the configure action binds options for the provider.
    /// </summary>
    [Fact]
    public void RegisterBrookStorageProviderWithConfigureActionBindsOptions()
    {
        ServiceCollection services = new();
        services.RegisterBrookStorageProvider<FakeProvider, FakeOptions>(o => o.Value = "x");
        using ServiceProvider provider = services.BuildServiceProvider();
        FakeOptions opts = provider.GetRequiredService<IOptions<FakeOptions>>().Value;
        Assert.Equal("x", opts.Value);
    }
}