using Mississippi.DomainModeling.Runtime;
using Mississippi.Tributary.Runtime;


namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     Registers the large-snapshot aggregate used by the Blob trust slice.
/// </summary>
internal static class LargeSnapshotScenarioRegistrations
{
    /// <summary>
    ///     Adds the large-snapshot aggregate support required by the Blob L2 trust slice.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddLargeSnapshotScenarioAggregate(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddAggregateSupport();
        services.AddEventType<LargeSnapshotStored>();
        services
            .AddCommandHandler<StoreLargeSnapshotCommand, LargeSnapshotAggregate, StoreLargeSnapshotCommandHandler>();
        services.AddReducer<LargeSnapshotStored, LargeSnapshotAggregate, LargeSnapshotStoredEventReducer>();
        services.AddSnapshotStateConverter<LargeSnapshotAggregate>();
        return services;
    }
}