using System.Threading.Tasks;

using Orleans;


namespace Cascade.Web.Contracts.Grains;

/// <summary>
///     Grain interface for greeting functionality.
/// </summary>
/// <remarks>
///     This grain is keyed by the name to greet and provides simple
///     operations to demonstrate end-to-end Orleans communication
///     from a web client through an API to an Orleans silo.
/// </remarks>
[Alias("Cascade.Web.Contracts.IGreeterGrain")]
public interface IGreeterGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Generates a greeting for the grain's key (the name).
    /// </summary>
    /// <returns>A response containing the greeting and metadata.</returns>
    [Alias("GreetAsync")]
    Task<GreetResponse> GreetAsync();

    /// <summary>
    ///     Converts the input string to uppercase.
    /// </summary>
    /// <param name="input">The string to convert.</param>
    /// <returns>The input string in uppercase.</returns>
    [Alias("ToUpperAsync")]
    Task<string> ToUpperAsync(
        string input
    );
}