// <copyright file="CrescentBlobTestCollection.cs" company="Gibbs-Morris LLC">
// Licensed under the Gibbs-Morris commercial license.
// </copyright>

namespace Crescent.Crescent.L2Tests;

/// <summary>
///     xUnit collection definition for tests using <see cref="CrescentBlobFixture" />.
/// </summary>
[CollectionDefinition(Name)]
#pragma warning disable CA1515 // Types can be made internal - xUnit collection must be public
public sealed class CrescentBlobTestCollectionDefinition : ICollectionFixture<CrescentBlobFixture>
{
    /// <summary>
    ///     The name of the test collection.
    /// </summary>
    public const string Name = "Crescent Blob Tests";
}
#pragma warning restore CA1515