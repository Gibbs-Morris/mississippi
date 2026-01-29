using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Silo;

using Spring.Domain.Projections.BankAccountBalance;
using Spring.Domain.Services;
using Spring.Silo.Registrations;
using Spring.Silo.Services;


namespace Spring.Silo.Infrastructure;

/// <summary>
///     Spring domain aggregate and projection registrations.
/// </summary>
internal static class SpringDomainRegistrations
{
    /// <summary>
    ///     Adds Spring domain services, aggregates, and projections.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSpringDomain(
        this IServiceCollection services
    )
    {
        AddApplicationServices(services);
        AddAggregates(services);
        AddProjections(services);
        AddProjectionScanning(services);
        return services;
    }

    private static void AddAggregates(
        IServiceCollection services
    )
    {
        // Generated aggregate registrations
        services.AddBankAccountAggregate();
        services.AddTransactionInvestigationQueueAggregate();
    }

    private static void AddApplicationServices(
        IServiceCollection services
    )
    {
        // HTTP client factory for effects that call external APIs
        services.AddHttpClient();

        // Notification service (stub for demo, replace with real provider in production)
        services.AddSingleton<INotificationService, StubNotificationService>();
    }

    private static void AddProjectionScanning(
        IServiceCollection services
    )
    {
        // Inlet silo services for projection subscription management
        // Note: AddInletSilo must be called before ScanProjectionAssemblies
        services.AddInletSilo();
        services.ScanProjectionAssemblies(typeof(BankAccountBalanceProjection).Assembly);
    }

    private static void AddProjections(
        IServiceCollection services
    )
    {
        // Generated projection registrations
        services.AddBankAccountBalanceProjection();
        services.AddBankAccountLedgerProjection();
        services.AddFlaggedTransactionsProjection();
    }
}