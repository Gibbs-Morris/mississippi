using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.DomainModeling.Abstractions;


namespace MississippiSamples.Spring.Domain.Aggregates.AuthProof.Steps;

/// <summary>
///     Minimal saga step used to keep the auth-proof saga executable for endpoint authorization tests.
/// </summary>
[SagaStep<AuthProofSagaState>(0, SagaStepRecoveryPolicy.Automatic)]
internal sealed class AuthProofSagaNoOpStep : ISagaStep<AuthProofSagaState>
{
    /// <inheritdoc />
    public Task<StepResult> ExecuteAsync(
        AuthProofSagaState state,
        SagaStepExecutionContext context,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(StepResult.Succeeded());
    }
}