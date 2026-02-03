using System.Threading;
using System.Threading.Tasks;

namespace Mississippi.EventSourcing.Sagas.Abstractions;

public interface ISagaStep<TSaga>
    where TSaga : class, ISagaState
{
    Task<StepResult> ExecuteAsync(TSaga state, CancellationToken cancellationToken);
}

public interface ICompensatable<TSaga>
    where TSaga : class, ISagaState
{
    Task<CompensationResult> CompensateAsync(TSaga state, CancellationToken cancellationToken);
}
