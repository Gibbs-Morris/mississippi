using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas;

/// <summary>
///     Orchestrates saga steps and compensation in response to saga lifecycle events.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public sealed class SagaOrchestrationEffect<TSaga> : IEventEffect<TSaga>
    where TSaga : class, ISagaState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaOrchestrationEffect{TSaga}" /> class.
    /// </summary>
    /// <param name="stepInfoProvider">The step metadata provider.</param>
    /// <param name="serviceProvider">The service provider used to resolve steps.</param>
    /// <param name="timeProvider">The time provider.</param>
    /// <param name="logger">The logger.</param>
    public SagaOrchestrationEffect(
        ISagaStepInfoProvider<TSaga> stepInfoProvider,
        IServiceProvider serviceProvider,
        TimeProvider timeProvider,
        ILogger<SagaOrchestrationEffect<TSaga>>? logger = null
    )
    {
        ArgumentNullException.ThrowIfNull(stepInfoProvider);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);
        StepInfoProvider = stepInfoProvider;
        ServiceProvider = serviceProvider;
        TimeProvider = timeProvider;
        Logger = logger;
    }

    private ILogger<SagaOrchestrationEffect<TSaga>>? Logger { get; }

    private IServiceProvider ServiceProvider { get; }

    private ISagaStepInfoProvider<TSaga> StepInfoProvider { get; }

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public bool CanHandle(
        object eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return eventData is SagaStartedEvent or SagaStepCompleted or SagaStepFailed or SagaCompensating
            or SagaStepCompensated;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<object> HandleAsync(
        object eventData,
        TSaga currentState,
        string brookKey,
        long eventPosition,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return eventData switch
        {
            SagaStartedEvent => ExecuteStepAsync(currentState, 0, cancellationToken),
            SagaStepCompleted completed => ExecuteNextOrCompleteAsync(
                currentState,
                completed.StepIndex,
                cancellationToken),
            SagaStepFailed => AsyncEnumerable.Empty<object>(),
            SagaCompensating compensating => ExecuteCompensationAsync(
                currentState,
                compensating.FromStepIndex,
                cancellationToken),
            SagaStepCompensated compensated => ExecutePreviousCompensationAsync(
                currentState,
                compensated.StepIndex,
                cancellationToken),
            var _ => AsyncEnumerable.Empty<object>(),
        };
    }

    private async IAsyncEnumerable<object> ExecuteCompensationAsync(
        TSaga state,
        int stepIndex,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        if (stepIndex < 0)
        {
            yield return new SagaCompensated
            {
                CompletedAt = TimeProvider.GetUtcNow(),
            };
            yield break;
        }

        if (!TryGetStep(stepIndex, out SagaStepInfo? stepInfo))
        {
            yield return new SagaFailed
            {
                ErrorCode = "COMPENSATION_FAILED",
                ErrorMessage = "Step metadata not found.",
                FailedAt = TimeProvider.GetUtcNow(),
            };
            yield break;
        }

        object stepInstance = ServiceProvider.GetRequiredService(stepInfo.StepType);
        if (stepInstance is not ICompensatable<TSaga> compensatable)
        {
            yield return new SagaStepCompensated
            {
                StepIndex = stepIndex,
                StepName = stepInfo.StepName,
            };
            yield break;
        }

        Logger?.SagaStepCompensating(typeof(TSaga).Name, stepInfo.StepName, stepIndex);
        CompensationResult result = await compensatable.CompensateAsync(state, cancellationToken).ConfigureAwait(false);
        if (result.Success || result.Skipped)
        {
            yield return new SagaStepCompensated
            {
                StepIndex = stepIndex,
                StepName = stepInfo.StepName,
            };
        }
        else
        {
            yield return new SagaFailed
            {
                ErrorCode = result.ErrorCode ?? "COMPENSATION_FAILED",
                ErrorMessage = result.ErrorMessage,
                FailedAt = TimeProvider.GetUtcNow(),
            };
        }
    }

    private async IAsyncEnumerable<object> ExecuteNextOrCompleteAsync(
        TSaga state,
        int completedStepIndex,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        int nextStepIndex = completedStepIndex + 1;
        if (!TryGetStep(nextStepIndex, out SagaStepInfo _))
        {
            yield return new SagaCompleted
            {
                CompletedAt = TimeProvider.GetUtcNow(),
            };
            yield break;
        }

        await foreach (object evt in ExecuteStepAsync(state, nextStepIndex, cancellationToken))
        {
            yield return evt;
        }
    }

    private async IAsyncEnumerable<object> ExecutePreviousCompensationAsync(
        TSaga state,
        int compensatedStepIndex,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        int nextIndex = compensatedStepIndex - 1;
        await foreach (object evt in ExecuteCompensationAsync(state, nextIndex, cancellationToken))
        {
            yield return evt;
        }
    }

    private async IAsyncEnumerable<object> ExecuteStepAsync(
        TSaga state,
        int stepIndex,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        if (!TryGetStep(stepIndex, out SagaStepInfo? stepInfo))
        {
            yield return new SagaFailed
            {
                ErrorCode = "STEP_METADATA_MISSING",
                ErrorMessage = "Step metadata not found.",
                FailedAt = TimeProvider.GetUtcNow(),
            };
            yield break;
        }

        ISagaStep<TSaga> step = ResolveStep(stepInfo);
        Logger?.SagaStepExecuting(typeof(TSaga).Name, stepInfo.StepName, stepIndex);
        StepResult result = await step.ExecuteAsync(state, cancellationToken).ConfigureAwait(false);
        if (result.Success)
        {
            foreach (object evt in result.Events)
            {
                yield return evt;
            }

            yield return new SagaStepCompleted
            {
                StepIndex = stepIndex,
                StepName = stepInfo.StepName,
                CompletedAt = TimeProvider.GetUtcNow(),
            };
        }
        else
        {
            yield return new SagaStepFailed
            {
                StepIndex = stepIndex,
                StepName = stepInfo.StepName,
                ErrorCode = result.ErrorCode ?? "SAGA_STEP_FAILED",
                ErrorMessage = result.ErrorMessage,
            };
            yield return new SagaCompensating
            {
                FromStepIndex = stepIndex - 1,
            };
        }
    }

    private ISagaStep<TSaga> ResolveStep(
        SagaStepInfo stepInfo
    )
    {
        object stepInstance = ServiceProvider.GetRequiredService(stepInfo.StepType);
        if (stepInstance is ISagaStep<TSaga> typedStep)
        {
            return typedStep;
        }

        throw new InvalidOperationException(
            $"Step type '{stepInfo.StepType.FullName}' does not implement ISagaStep<{typeof(TSaga).Name}>.");
    }

    private bool TryGetStep(
        int stepIndex,
        out SagaStepInfo stepInfo
    )
    {
        IReadOnlyList<SagaStepInfo> steps = StepInfoProvider.Steps;
        if ((stepIndex < 0) || (stepIndex >= steps.Count))
        {
            stepInfo = default!;
            return false;
        }

        stepInfo = steps[stepIndex];
        return true;
    }
}