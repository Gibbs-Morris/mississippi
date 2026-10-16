using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Aqueduct.Gateway;

using NSubstitute;

using Orleans;


namespace Mississippi.Inlet.Gateway.L0Tests;

/// <summary>
///     Tests that the Inlet parent composes its Aqueduct child in every registration order.
/// </summary>
public sealed class InletServerRegistrationOrderTests
{
    /// <summary>
    ///     The parent and child resolve the actual Aqueduct lifetime manager in any order.
    /// </summary>
    /// <param name="aqueductFirst">The child registration order, or null for the parent alone.</param>
    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    [InlineData(true)]
    public void ParentAndChildShouldResolveHubInEveryOrder(
        bool? aqueductFirst
    )
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IClusterClient>());
        services.AddSingleton(Substitute.For<IGrainFactory>());
        if (aqueductFirst == true)
        {
            services.AddAqueduct<InletHub>();
        }

        services.AddInletServer();
        if (aqueductFirst == false)
        {
            services.AddAqueduct<InletHub>();
        }

        using ServiceProvider provider = services.BuildServiceProvider();
        HubLifetimeManager<InletHub> manager = provider.GetRequiredService<HubLifetimeManager<InletHub>>();
        Assert.IsType<AqueductHubLifetimeManager<InletHub>>(manager);
        Assert.Same(manager, provider.GetRequiredService<HubLifetimeManager<InletHub>>());
        Assert.Equal(0, provider.GetRequiredKeyedService<IConnectionRegistry>(typeof(InletHub)).Count);
    }
}