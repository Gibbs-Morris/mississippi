using System.Threading.Tasks;

using Orleans;


namespace Crescent.ConsoleApp.Shared.Grains;

/// <summary>
///     A simple grain implementation that returns "Hello, world!".
/// </summary>
internal sealed class SampleGrain
    : Grain,
      ISampleGrain
{
    /// <inheritdoc />
    public Task<string> HelloWorldAsync() => Task.FromResult("Hello, world!");
}