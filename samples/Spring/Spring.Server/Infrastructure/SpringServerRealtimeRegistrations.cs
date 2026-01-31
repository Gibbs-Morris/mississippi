using Microsoft.Extensions.DependencyInjection;

using Mississippi.Aqueduct;
using Mississippi.EventSourcing.Aggregates;
using Mississippi.EventSourcing.Serialization.Json;
using Mississippi.EventSourcing.UxProjections;
using Mississippi.Inlet.Server;
using Mississippi.Inlet.Silo;

using Spring.Domain.Projections.BankAccountBalance;
using Spring.Server.Controllers.Aggregates.Mappers;
using Spring.Server.Controllers.Projections.Mappers;


namespace Spring.Server.Infrastructure;

/// <summary>
///     Real-time infrastructure (SignalR, Aqueduct, Inlet, aggregates, projections) for Spring server.
/// </summary>
internal static class SpringServerRealtimeRegistrations
{
    /// <summary>
    ///     Adds SignalR, Aqueduct, Inlet, and domain infrastructure.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSpringRealtime(
        this IServiceCollection services
    )
    {
        AddSerializationAndInfrastructure(services);
        AddSignalRAndAqueduct(services);
        AddInletServer(services);
        AddMappers(services);
        return services;
    }

    private static void AddInletServer(
        IServiceCollection services
    )
    {
        // Inlet Server for real-time projection updates
        services.AddInletServer();
        services.ScanProjectionAssemblies(typeof(BankAccountBalanceProjection).Assembly);
    }

    private static void AddMappers(
        IServiceCollection services
    )
    {
        // Aggregate DTO to command mappers
        services.AddBankAccountAggregateMappers();

        // Note: TransactionInvestigationQueue aggregate has no public commands
        // (only server-side effect dispatch) so no mappers are generated or needed.

        // Projection mappers
        services.AddBankAccountBalanceProjectionMappers();
        services.AddBankAccountLedgerProjectionMappers();
        services.AddFlaggedTransactionsProjectionMappers();
    }

    private static void AddSerializationAndInfrastructure(
        IServiceCollection services
    )
    {
        // JSON serialization (required by aggregate infrastructure)
        services.AddJsonSerialization();

        // Aggregate infrastructure (IAggregateGrainFactory, IBrookEventConverter, etc.)
        services.AddAggregateSupport();

        // UX projection infrastructure (IUxProjectionGrainFactory)
        services.AddUxProjections();
    }

    private static void AddSignalRAndAqueduct(
        IServiceCollection services
    )
    {
        // SignalR with Aqueduct Orleans backplane
        services.AddSignalR();
        services.AddAqueduct<InletHub>();
    }
}