using Mississippi.DomainModeling.Runtime.Builders;
using Mississippi.Sdk.Runtime;


namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     Extension methods for registering counter aggregate services.
/// </summary>
internal static class CounterRegistrations
{
    /// <summary>
    ///     Adds the Crescent counter runtime registrations through the Mississippi runtime builder.
    /// </summary>
    /// <param name="runtime">The Mississippi runtime builder.</param>
    public static void AddCounterRuntime(
        this MississippiRuntimeBuilder runtime
    )
    {
        runtime.Aggregates(aggregates =>
        {
            aggregates.AddAggregate<CounterAggregate>();
            aggregates.AddEventType<CounterInitialized>();
            aggregates.AddEventType<CounterIncremented>();
            aggregates.AddEventType<CounterDecremented>();
            aggregates.AddEventType<CounterReset>();
            aggregates.AddCommandHandler<InitializeCounter, CounterAggregate, InitializeCounterHandler>();
            aggregates.AddCommandHandler<IncrementCounter, CounterAggregate, IncrementCounterHandler>();
            aggregates.AddCommandHandler<DecrementCounter, CounterAggregate, DecrementCounterHandler>();
            aggregates.AddCommandHandler<ResetCounter, CounterAggregate, ResetCounterHandler>();
            aggregates.AddReducer<CounterInitialized, CounterAggregate, CounterInitializedEventReducer>();
            aggregates.AddReducer<CounterIncremented, CounterAggregate, CounterIncrementedEventReducer>();
            aggregates.AddReducer<CounterDecremented, CounterAggregate, CounterDecrementedEventReducer>();
            aggregates.AddReducer<CounterReset, CounterAggregate, CounterResetEventReducer>();
            aggregates.AddSnapshotStateConverter<CounterAggregate>();
        });
        runtime.Projections(projections =>
        {
            projections.AddProjection<CounterSummaryProjection>();
            projections
                .AddReducer<CounterInitialized, CounterSummaryProjection, CounterSummaryInitializedEventReducer>();
            projections
                .AddReducer<CounterIncremented, CounterSummaryProjection, CounterSummaryIncrementedEventReducer>();
            projections
                .AddReducer<CounterDecremented, CounterSummaryProjection, CounterSummaryDecrementedEventReducer>();
            projections.AddReducer<CounterReset, CounterSummaryProjection, CounterSummaryResetEventReducer>();
            projections.AddSnapshotStateConverter<CounterSummaryProjection>();
        });
    }
}