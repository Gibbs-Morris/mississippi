#pragma warning disable SA1028 // Code should not contain trailing whitespace

namespace CrescentConsoleApp.Grains;

/// <summary>
///     A simple sample grain interface that returns a greeting.
/// </summary>
[Alias("ISampleGrain")]
internal interface ISampleGrain : IGrainWithIntegerKey
{
    /// <summary>
    ///     Returns a friendly greeting.
    /// </summary>
    /// <returns>A greeting string.</returns>
    [Alias("HelloWorld")]
    Task<string> HelloWorldAsync();
}

#pragma warning disable SA1028