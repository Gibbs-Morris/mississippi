using System.Threading.Tasks;

using Orleans;


namespace Mississippi.DomainModeling.TestHarness.L0Tests.Architecture;

/// <summary>
///     Fixture used to prove that method-level aliases do not create type-level mismatches.
/// </summary>
internal interface IMethodAliasOnlyGrain
{
    /// <summary>
    ///     Executes a method alias fixture call.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Alias("ExecuteAsync")]
    Task ExecuteAsync();
}