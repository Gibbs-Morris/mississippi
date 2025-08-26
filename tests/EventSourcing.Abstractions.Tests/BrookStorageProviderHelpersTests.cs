using System.Runtime.CompilerServices;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Abstractions.Storage;


namespace Mississippi.EventSourcing.Abstractions.Tests;

public class BrookStorageProviderHelpersTests
{
    [Fact]
    public void RegisterBrookStorageProvider_RegistersReaderAndWriter()
    {
        ServiceCollection services = new();
        services.RegisterBrookStorageProvider<FakeProvider>();
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<IBrookStorageReader>());
        Assert.NotNull(provider.GetRequiredService<IBrookStorageWriter>());
    }

    [Fact]
    public void RegisterBrookStorageProvider_WithConfigureActionBindsOptions()
    {
        ServiceCollection services = new();
        services.RegisterBrookStorageProvider<FakeProvider, FakeOptions>(o => o.Value = "x");
        using ServiceProvider provider = services.BuildServiceProvider();
        FakeOptions opts = provider.GetRequiredService<IOptions<FakeOptions>>().Value;
        Assert.Equal("x", opts.Value);
    }

    [Fact]
    public void RegisterBrookStorageProvider_WithConfigurationBindsOptions()
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

    private sealed class FakeProvider : IBrookStorageProvider
    {
        public string Format => "fake";

        public Task<BrookPosition> ReadHeadPositionAsync(
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

        public Task<BrookPosition> AppendEventsAsync(
            BrookKey brookId,
            IReadOnlyList<BrookEvent> events,
            BrookPosition? expectedVersion = null,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(new BrookPosition(events.Count));
    }

    private sealed class FakeOptions
    {
        public string? Value { get; set; }
    }
}