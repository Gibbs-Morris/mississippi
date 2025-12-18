using System;

using Orleans.TestingHost;


namespace Mississippi.Testing.Utilities.Orleans;

/// <summary>
///     Base class for Orleans <see cref="TestCluster" /> fixtures that provides common setup logic.
/// </summary>
/// <typeparam name="TSiloConfigurator">
///     The silo configurator type implementing <see cref="ISiloConfigurator" />.
/// </typeparam>
/// <typeparam name="TClientConfigurator">
///     The client configurator type implementing <see cref="IClientBuilderConfigurator" />.
/// </typeparam>
/// <remarks>
///     <para>
///         This base class handles the common cluster setup pattern:
///         <list type="bullet">
///             <item>Building the test cluster with a single silo</item>
///             <item>Deploying the cluster on construction</item>
///             <item>Setting <see cref="TestClusterAccess.Cluster" /> for test access</item>
///             <item>Stopping all silos on disposal</item>
///         </list>
///     </para>
/// </remarks>
public abstract class ClusterFixtureBase<TSiloConfigurator, TClientConfigurator> : IDisposable
    where TSiloConfigurator : ISiloConfigurator, new()
    where TClientConfigurator : IClientBuilderConfigurator, new()
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ClusterFixtureBase{TSiloConfigurator, TClientConfigurator}" />
    ///     class and deploys a single-node Orleans TestCluster.
    /// </summary>
    protected ClusterFixtureBase()
    {
        TestClusterBuilder builder = new();
        builder.AddSiloBuilderConfigurator<TSiloConfigurator>();
        builder.AddClientBuilderConfigurator<TClientConfigurator>();
        builder.Options.InitialSilosCount = 1;
        Cluster = builder.Build();
        Cluster.Deploy();
        TestClusterAccess.Cluster = Cluster;
    }

    /// <summary>
    ///     Gets the active Orleans test cluster.
    /// </summary>
    public TestCluster Cluster { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Disposes of resources used by the cluster fixture.
    /// </summary>
    /// <param name="disposing">Whether this is being called from <see cref="Dispose()" />.</param>
    protected virtual void Dispose(
        bool disposing
    )
    {
        if (disposing)
        {
            Cluster.StopAllSilos();
        }
    }
}