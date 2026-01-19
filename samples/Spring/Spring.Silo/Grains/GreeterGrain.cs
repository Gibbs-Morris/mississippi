using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Orleans;
using Orleans.Runtime;

using Spring.Domain;


namespace Spring.Silo.Grains;

/// <summary>
///     Orleans grain that provides greeting functionality.
/// </summary>
/// <remarks>
///     This grain demonstrates a simple end-to-end flow from WebAssembly through
///     an ASP.NET API to an Orleans grain running in a separate silo process.
///     The grain is keyed by the name being greeted.
/// </remarks>
[Alias("Spring.Silo.GreeterGrain")]
internal sealed class GreeterGrain
    : IGrainBase,
      IGreeterGrain
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="GreeterGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="logger">The logger instance.</param>
    public GreeterGrain(
        IGrainContext grainContext,
        ILogger<GreeterGrain> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private ILogger<GreeterGrain> Logger { get; }

    /// <inheritdoc />
    public Task<GreetResult> GreetAsync()
    {
        string name = this.GetPrimaryKeyString();
        string greeting = $"Hello, {name}!";
        Logger.LogGreetingGenerated(name, greeting);
        GreetResult response = new()
        {
            Greeting = greeting,
            GeneratedAt = DateTime.UtcNow,
        };
        return Task.FromResult(response);
    }
}