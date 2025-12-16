using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.AspNetCore.Orleans.OutputCaching.Grains;

/// <summary>
///     Orleans grain implementation for tracking cache keys by tag.
/// </summary>
internal sealed class TagIndexGrain
    : IGrainBase,
      ITagIndexGrain
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TagIndexGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The grain context.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="state">The persistent state.</param>
    public TagIndexGrain(
        IGrainContext grainContext,
        ILogger<TagIndexGrain> logger,
        [PersistentState("tagIndex", "OutputCacheStorage")]
        IPersistentState<TagIndexState> state
    )
    {
        GrainContext = grainContext;
        Logger = logger;
        State = state;
    }

    /// <summary>
    ///     Gets the grain context required by IGrainBase.
    /// </summary>
    public IGrainContext GrainContext { get; }

    private ILogger<TagIndexGrain> Logger { get; }

    private IPersistentState<TagIndexState> State { get; }

    /// <inheritdoc />
    public async Task AddKeyAsync(
        string key
    )
    {
        if (!State.State.Keys.Contains(key))
        {
            State.State.Keys.Add(key);
            await State.WriteStateAsync();
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetKeysAsync() => Task.FromResult<IReadOnlyList<string>>(State.State.Keys);

    /// <inheritdoc />
    public async Task RemoveKeyAsync(
        string key
    )
    {
        if (State.State.Keys.Remove(key))
        {
            await State.WriteStateAsync();
        }
    }

    [GenerateSerializer]
    [Alias("Mississippi.AspNetCore.Orleans.OutputCaching.TagIndexState")]
    internal sealed record TagIndexState
    {
        [Id(0)]
        public List<string> Keys { get; set; } = [];
    }
}