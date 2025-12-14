using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.AspNetCore.Orleans.OutputCaching.Grains;

/// <summary>
///     Orleans grain implementation for output cache entry storage.
/// </summary>
internal sealed class OutputCacheEntryGrain
    : IGrainBase,
      IOutputCacheEntryGrain
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="OutputCacheEntryGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The grain context.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="timeProvider">The time provider.</param>
    /// <param name="state">The persistent state.</param>
    public OutputCacheEntryGrain(
        IGrainContext grainContext,
        ILogger<OutputCacheEntryGrain> logger,
        TimeProvider timeProvider,
        [PersistentState("outputCacheEntry", "OutputCacheStorage")]
        IPersistentState<OutputCacheEntryState> state
    )
    {
        GrainContext = grainContext;
        Logger = logger;
        TimeProvider = timeProvider;
        State = state;
    }

    /// <summary>
    ///     Gets the grain context required by IGrainBase.
    /// </summary>
    public IGrainContext GrainContext { get; }

    private ILogger<OutputCacheEntryGrain> Logger { get; }

    private IPersistentState<OutputCacheEntryState> State { get; }

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public async Task EvictAsync()
    {
        await State.ClearStateAsync();
    }

    /// <inheritdoc />
    public Task<OutputCacheEntryData?> GetAsync()
    {
        if (!State.RecordExists || State.State.Data is null)
        {
            return Task.FromResult<OutputCacheEntryData?>(null);
        }

        DateTimeOffset now = TimeProvider.GetUtcNow();
        OutputCacheEntryData data = State.State.Data;
        if (data.ExpiresAt.HasValue && (data.ExpiresAt.Value <= now))
        {
            return Task.FromResult<OutputCacheEntryData?>(null);
        }

        return Task.FromResult<OutputCacheEntryData?>(data);
    }

    /// <inheritdoc />
    public Task<bool> HasTagAsync(
        string tag
    )
    {
        if (!State.RecordExists || State.State.Data is null)
        {
            return Task.FromResult(false);
        }

        bool hasTag = State.State.Data.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
        return Task.FromResult(hasTag);
    }

    /// <inheritdoc />
    public async Task SetAsync(
        OutputCacheEntryData data,
        string[]? tags
    )
    {
        OutputCacheEntryData entryData = data with
        {
            Tags = tags ?? [],
        };
        State.State = new()
        {
            Data = entryData,
        };
        await State.WriteStateAsync();
    }

    [GenerateSerializer]
    [Alias("Mississippi.AspNetCore.Orleans.OutputCaching.OutputCacheEntryState")]
    internal sealed record OutputCacheEntryState
    {
        [Id(0)]
        public OutputCacheEntryData? Data { get; set; }
    }
}