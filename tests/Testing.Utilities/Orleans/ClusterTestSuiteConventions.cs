namespace Mississippi.Testing.Utilities.Orleans;

/// <summary>
///     Marker interface for xUnit collection definitions that share an Orleans test cluster fixture.
/// </summary>
/// <remarks>
///     <para>
///         Test projects should create their own collection definition class that inherits from
///         <c>ICollectionFixture&lt;TFixture&gt;</c> with their specific cluster fixture type:
///     </para>
///     <para>
///         Test classes can then use the <c>[Collection(MyClusterTestSuite.Name)]</c> attribute
///         to participate in the shared cluster collection.
///     </para>
/// </remarks>
public static class ClusterTestSuiteConventions
{
    /// <summary>
    ///     The conventional name for cluster test suite collections.
    /// </summary>
    /// <remarks>
    ///     Test projects may use this constant or define their own collection name.
    /// </remarks>
    public const string DefaultCollectionName = "ClusterTestSuite";
}