using System.Threading.Tasks;

using Orleans;


namespace Crescent.ConsoleApp.Shared.Grains;

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