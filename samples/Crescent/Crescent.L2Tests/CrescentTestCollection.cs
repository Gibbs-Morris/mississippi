// <copyright file="CrescentTestCollection.cs" company="Gibbs-Morris LLC">
// Licensed under the Gibbs-Morris commercial license.
// </copyright>

namespace Crescent.L2Tests;

/// <summary>
///     xUnit collection definition that ensures all Crescent tests share a single AppHost instance.
///     This prevents container thrashing and significantly reduces test execution time.
/// </summary>
[CollectionDefinition(Name)]
#pragma warning disable CA1515 // Types can be made internal - xUnit collection must be public
#pragma warning disable CA1711 // Rename type so it does not end in 'Collection' - required by xUnit
public sealed class CrescentTestCollection : ICollectionFixture<CrescentFixture>
#pragma warning restore CA1711
#pragma warning restore CA1515
{
    /// <summary>
    ///     The name of the test collection.
    /// </summary>
    public const string Name = "Crescent Integration Tests";
}