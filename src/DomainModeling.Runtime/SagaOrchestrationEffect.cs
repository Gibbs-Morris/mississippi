using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Runtime.Sagas;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Orchestrates saga steps and compensation in response to saga lifecycle events.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public sealed class SagaOrchestrationEffect<TSaga> : IEventEffect<TSaga>
    where TSaga : class, ISagaState
{
    private const string CompensationExceptionErrorCode = "COMPENSATION_EXCEPTION";

    private const string SagaStepExceptionErrorCode = "SAGA_STEP_EXCEPTION";

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

    private static bool ShouldPropagateException(
        Exception exception,
        CancellationToken cancellationToken
    ) =>
        exception is OutOfMemoryException or StackOverflowException or ThreadInterruptedException ||
        (exception is OperationCanceledException && cancellationToken.IsCancellationRequested);

    private static bool TryGetStep(
        ImmutableArray<SagaStepInfo> steps,
        int stepIndex,
        out SagaStepInfo stepInfo
    )
    {
        if ((stepIndex < 0) || (stepIndex >= steps.Length))
        {
            stepInfo = default!;
            return false;
        }

        stepInfo = steps[stepIndex];
        return true;
    }

    /// <inheritdoc />
    public bool CanHandle(
        object eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return SagaLifecycleEventClassifier.IsOrchestrationLifecycleEvent(eventData);
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
        ArgumentNullException.ThrowIfNull(currentState);
        return HandleCoreAsync(eventData, currentState, brookKey, cancellationToken);
    }

    private async IAsyncEnumerable<object> ExecuteCompensationAsync(
        TSaga state,
        ImmutableArray<SagaStepInfo> steps,
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

        if (!TryGetStep(steps, stepIndex, out SagaStepInfo? stepInfo))
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
        CompensationResult result;
        try
        {
            result = await compensatable.CompensateAsync(state, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ShouldPropagateException(ex, cancellationToken))
        {
            Logger?.SagaStepCompensationException(typeof(TSaga).Name, stepInfo.StepName, stepIndex, ex);
            result = CompensationResult.Failed(CompensationExceptionErrorCode, ex.Message);
        }

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
        ImmutableArray<SagaStepInfo> steps,
        int completedStepIndex,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        int nextStepIndex = completedStepIndex + 1;
        if (!TryGetStep(steps, nextStepIndex, out SagaStepInfo _))
        {
            yield return new SagaCompleted
            {
                CompletedAt = TimeProvider.GetUtcNow(),
            };
            yield break;
        }

        await foreach (object evt in ExecuteStepAsync(state, steps, nextStepIndex, cancellationToken))
        {
            yield return evt;
        }
    }

    private async IAsyncEnumerable<object> ExecutePreviousCompensationAsync(
        TSaga state,
        ImmutableArray<SagaStepInfo> steps,
        int compensatedStepIndex,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        int nextIndex = compensatedStepIndex - 1;
        await foreach (object evt in ExecuteCompensationAsync(state, steps, nextIndex, cancellationToken))
        {
            yield return evt;
        }
    }

    private async IAsyncEnumerable<object> ExecuteStepAsync(
        TSaga state,
        ImmutableArray<SagaStepInfo> steps,
        int stepIndex,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        if (!TryGetStep(steps, stepIndex, out SagaStepInfo? stepInfo))
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
        StepResult result;
        try
        {
            result = await step.ExecuteAsync(state, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ShouldPropagateException(ex, cancellationToken))
        {
            Logger?.SagaStepExecutionException(typeof(TSaga).Name, stepInfo.StepName, stepIndex, ex);
            result = StepResult.Failed(SagaStepExceptionErrorCode, ex.Message);
        }

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

    private async IAsyncEnumerable<object> HandleCoreAsync(
        object eventData,
        TSaga currentState,
        string brookKey,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        if (!SagaLifecycleEventClassifier.IsReplayBoundaryEvent(eventData))
        {
            yield break;
        }

        if (!TryGetMatchingWorkflow(currentState, brookKey, cancellationToken, out ImmutableArray<SagaStepInfo> steps))
        {
            yield return new SagaFailed
            {
                ErrorCode = "SAGA_STEP_HASH_MISMATCH",
                ErrorMessage =
                    "The registered saga steps differ from the persisted workflow definition or cannot be hashed.",
                FailedAt = TimeProvider.GetUtcNow(),
            };
            yield break;
        }

        IAsyncEnumerable<object> events = eventData switch
        {
            SagaStartedEvent => ExecuteStepAsync(currentState, steps, 0, cancellationToken),
            SagaStepCompleted completed => ExecuteNextOrCompleteAsync(
                currentState,
                steps,
                completed.StepIndex,
                cancellationToken),
            SagaCompensating compensating => ExecuteCompensationAsync(
                currentState,
                steps,
                compensating.FromStepIndex,
                cancellationToken),
            SagaStepCompensated compensated => ExecutePreviousCompensationAsync(
                currentState,
                steps,
                compensated.StepIndex,
                cancellationToken),
            var _ => AsyncEnumerable.Empty<object>(),
        };
        await foreach (object resultEvent in events)
        {
            yield return resultEvent;
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

    private bool TryGetMatchingWorkflow(
        TSaga currentState,
        string brookKey,
        CancellationToken cancellationToken,
        out ImmutableArray<SagaStepInfo> steps
    )
    {
        steps = default;
        try
        {
            steps = StepInfoProvider.Steps.ToImmutableArray();
            if (string.Equals(currentState.StepHash, SagaStepHash.Compute(steps), StringComparison.Ordinal))
            {
                return true;
            }
        }
        catch (Exception exception) when (!ShouldPropagateException(exception, cancellationToken))
        {
            Logger?.SagaWorkflowChanged(typeof(TSaga).Name, brookKey, exception);
            return false;
        }

        Logger?.SagaWorkflowChanged(typeof(TSaga).Name, brookKey);
        return false;
    }
}