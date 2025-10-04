namespace Mississippi.CrescentConsoleApp.Grains;

/// <summary>
///     A simple grain implementation that returns "Hello, world!".
/// </summary>
internal sealed class SampleGrain
    : Grain,
      ISampleGrain
{
    /// <inheritdoc />
    public Task<string> HelloWorldAsync()
    {
        return Task.FromResult("Hello, world!");
    }
}
