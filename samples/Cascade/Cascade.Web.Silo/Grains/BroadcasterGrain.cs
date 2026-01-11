using System;
using System.Threading;
using System.Threading.Tasks;

using Cascade.Web.Contracts.Grains;

using Microsoft.Extensions.Logging;

using Orleans;
using Orleans.Runtime;
using Orleans.Streams;


namespace Cascade.Web.Silo.Grains;

/// <summary>
///     Orleans grain that broadcasts messages to all stream subscribers.
/// </summary>
/// <remarks>
///     This grain demonstrates Orleans streaming by publishing messages
///     to a memory stream. Subscribers (grains or clients) can receive
///     these messages in real-time via the stream subscription.
/// </remarks>
[Alias("Cascade.Web.Silo.BroadcasterGrain")]
internal sealed class BroadcasterGrain : IGrainBase, IBroadcasterGrain
{
    private const string StreamProviderName = "StreamProvider";
    private const string StreamNamespace = "broadcast";

    private IAsyncStream<StreamMessage>? Stream { get; set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BroadcasterGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="logger">The logger instance.</param>
    public BroadcasterGrain(
        IGrainContext grainContext,
        ILogger<BroadcasterGrain> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private ILogger<BroadcasterGrain> Logger { get; }

    /// <inheritdoc />
    public Task OnActivateAsync(CancellationToken token)
    {
        // Get the stream provider and create a stream reference
        IStreamProvider streamProvider = this.GetStreamProvider(StreamProviderName);
        string grainKey = this.GetPrimaryKeyString();

        // Create a stream using the grain's key as the stream key
        Stream = streamProvider.GetStream<StreamMessage>(StreamId.Create(StreamNamespace, grainKey));

        Logger.LogBroadcasterActivated(grainKey);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task BroadcastAsync(string message)
    {
        if (Stream is null)
        {
            throw new InvalidOperationException("Stream not initialized. Grain may not be activated.");
        }

        string grainKey = this.GetPrimaryKeyString();
        StreamMessage streamMessage = new()
        {
            Content = message,
            Sender = grainKey,
            Timestamp = DateTime.UtcNow,
        };

        Logger.LogBroadcastingSent(grainKey, message);

        await Stream.OnNextAsync(streamMessage);
    }
}
