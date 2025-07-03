#pragma warning disable SA1028 // Code should not contain trailing whitespace

namespace CrescentConsoleApp.Grains;

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