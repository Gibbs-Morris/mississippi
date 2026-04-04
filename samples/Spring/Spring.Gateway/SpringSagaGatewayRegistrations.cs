using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.DomainModeling.Runtime;

using MississippiSamples.Spring.Domain.Aggregates.AuthProof;
using MississippiSamples.Spring.Domain.Aggregates.MoneyTransferSaga;


namespace MississippiSamples.Spring.Gateway;

/// <summary>
///     Registers gateway-side saga recovery services required by generated saga controllers.
/// </summary>
public static class SpringSagaGatewayRegistrations
{
    /// <summary>
    ///     Adds the Spring sample saga recovery services used by generated gateway controllers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddSpringSagaRecoverySupport(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        Type authProofStepType = ResolveDomainType<AuthProofSagaState>(
            "MississippiSamples.Spring.Domain.Aggregates.AuthProof.Steps.AuthProofSagaNoOpStep");
        Type withdrawFromSourceStepType = ResolveDomainType<MoneyTransferSagaState>(
            "MississippiSamples.Spring.Domain.Aggregates.MoneyTransferSaga.Steps.WithdrawFromSourceStep");
        Type depositToDestinationStepType = ResolveDomainType<MoneyTransferSagaState>(
            "MississippiSamples.Spring.Domain.Aggregates.MoneyTransferSaga.Steps.DepositToDestinationStep");
        services.AddSagaRecoveryServices<AuthProofSagaState>();
        services.AddSagaRecoveryInfo<AuthProofSagaState>(new(SagaRecoveryMode.Automatic, null));
        services.AddSagaStepInfo<AuthProofSagaState>(
            new SagaStepInfo[]
            {
                new(0, "AuthProofSagaNoOpStep", authProofStepType, false, SagaStepRecoveryPolicy.Automatic, null),
            });
        services.AddSagaRecoveryServices<MoneyTransferSagaState>();
        services.AddSagaRecoveryInfo<MoneyTransferSagaState>(new(SagaRecoveryMode.Automatic, null));
        services.AddSagaStepInfo<MoneyTransferSagaState>(
            new SagaStepInfo[]
            {
                new(
                    0,
                    "WithdrawFromSourceStep",
                    withdrawFromSourceStepType,
                    true,
                    SagaStepRecoveryPolicy.ManualOnly,
                    SagaStepRecoveryPolicy.ManualOnly),
                new(
                    1,
                    "DepositToDestinationStep",
                    depositToDestinationStepType,
                    false,
                    SagaStepRecoveryPolicy.ManualOnly,
                    null),
            });
        return services;
    }

    private static Type ResolveDomainType<TMarker>(
        string fullTypeName
    ) =>
        typeof(TMarker).Assembly.GetType(fullTypeName, true)!;
}