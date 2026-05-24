using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Orchestrates saga steps and compensation in response to saga lifecycle events.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public sealed class SagaOrchestrationEffect<TSaga> : IEventEffect<TSaga>
    where TSaga : class, ISagaState
{
    private const string CompensationExceptionErrorCode = "COMPENSATION_EXCEPTION";

    private const string CompensationFailedErrorCode = "COMPENSATION_FAILED";

    private const string SagaStepExceptionErrorCode = "SAGA_STEP_EXCEPTION";

    private const string SagaStepFailedErrorCode = "SAGA_STEP_FAILED";

    private const string StepMetadataMissingErrorCode = "STEP_METADATA_MISSING";

    private const string StepMetadataMissingErrorMessage = "Step metadata not found.";

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
        return eventData switch
        {
            SagaStartedEvent => ExecuteStepAsync(currentState, 0, brookKey, eventPosition, cancellationToken),
            SagaStepCompleted completed => ExecuteNextOrCompleteAsync(
                currentState,
                completed.StepIndex,
                brookKey,
                eventPosition,
                cancellationToken),
            SagaStepFailed => AsyncEnumerable.Empty<object>(),
            SagaCompensating compensating => ExecuteCompensationAsync(
                currentState,
                compensating.FromStepIndex,
                brookKey,
                eventPosition,
                cancellationToken),
            SagaStepCompensated compensated => ExecutePreviousCompensationAsync(
                currentState,
                compensated.StepIndex,
                brookKey,
                eventPosition,
                cancellationToken),
            var _ => AsyncEnumerable.Empty<object>(),
        };
    }

    private async IAsyncEnumerable<object> ExecuteCompensationAsync(
        TSaga state,
        int stepIndex,
        string brookKey,
        long eventPosition,
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
            Logger?.SagaStepCompensationMetadataMissing(
                typeof(TSaga).Name,
                state.SagaId,
                state.CorrelationId,
                brookKey,
                eventPosition,
                stepIndex,
                CompensationFailedErrorCode,
                StepMetadataMissingErrorMessage);
            yield return new SagaFailed
            {
                ErrorCode = CompensationFailedErrorCode,
                ErrorMessage = StepMetadataMissingErrorMessage,
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
        bool failureAlreadyLogged = false;
        try
        {
            result = await compensatable.CompensateAsync(state, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ShouldPropagateException(ex, cancellationToken))
        {
            Logger?.SagaStepCompensationException(
                typeof(TSaga).Name,
                state.SagaId,
                state.CorrelationId,
                brookKey,
                eventPosition,
                stepInfo.StepName,
                stepIndex,
                CompensationExceptionErrorCode,
                ex);
            result = CompensationResult.Failed(CompensationExceptionErrorCode, ex.Message);
            failureAlreadyLogged = true;
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
            string errorCode = result.ErrorCode ?? CompensationFailedErrorCode;
            if (!failureAlreadyLogged)
            {
                Logger?.SagaStepCompensationFailed(
                    typeof(TSaga).Name,
                    state.SagaId,
                    state.CorrelationId,
                    brookKey,
                    eventPosition,
                    stepInfo.StepName,
                    stepIndex,
                    errorCode,
                    result.ErrorMessage);
            }

            yield return new SagaFailed
            {
                ErrorCode = errorCode,
                ErrorMessage = result.ErrorMessage,
                FailedAt = TimeProvider.GetUtcNow(),
            };
        }
    }

    private async IAsyncEnumerable<object> ExecuteNextOrCompleteAsync(
        TSaga state,
        int completedStepIndex,
        string brookKey,
        long eventPosition,
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

        await foreach (object evt in ExecuteStepAsync(state, nextStepIndex, brookKey, eventPosition, cancellationToken))
        {
            yield return evt;
        }
    }

    private async IAsyncEnumerable<object> ExecutePreviousCompensationAsync(
        TSaga state,
        int compensatedStepIndex,
        string brookKey,
        long eventPosition,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        int nextIndex = compensatedStepIndex - 1;
        await foreach (object evt in ExecuteCompensationAsync(
                           state,
                           nextIndex,
                           brookKey,
                           eventPosition,
                           cancellationToken))
        {
            yield return evt;
        }
    }

    private async IAsyncEnumerable<object> ExecuteStepAsync(
        TSaga state,
        int stepIndex,
        string brookKey,
        long eventPosition,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        if (!TryGetStep(stepIndex, out SagaStepInfo? stepInfo))
        {
            Logger?.SagaStepMetadataMissing(
                typeof(TSaga).Name,
                state.SagaId,
                state.CorrelationId,
                brookKey,
                eventPosition,
                stepIndex,
                StepMetadataMissingErrorCode,
                StepMetadataMissingErrorMessage);
            yield return new SagaFailed
            {
                ErrorCode = StepMetadataMissingErrorCode,
                ErrorMessage = StepMetadataMissingErrorMessage,
                FailedAt = TimeProvider.GetUtcNow(),
            };
            yield break;
        }

        ISagaStep<TSaga> step = ResolveStep(stepInfo);
        Logger?.SagaStepExecuting(typeof(TSaga).Name, stepInfo.StepName, stepIndex);
        StepResult result;
        bool failureAlreadyLogged = false;
        try
        {
            result = await step.ExecuteAsync(state, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ShouldPropagateException(ex, cancellationToken))
        {
            Logger?.SagaStepExecutionException(
                typeof(TSaga).Name,
                state.SagaId,
                state.CorrelationId,
                brookKey,
                eventPosition,
                stepInfo.StepName,
                stepIndex,
                SagaStepExceptionErrorCode,
                ex);
            result = StepResult.Failed(SagaStepExceptionErrorCode, ex.Message);
            failureAlreadyLogged = true;
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
            string errorCode = result.ErrorCode ?? SagaStepFailedErrorCode;
            if (!failureAlreadyLogged)
            {
                Logger?.SagaStepExecutionFailed(
                    typeof(TSaga).Name,
                    state.SagaId,
                    state.CorrelationId,
                    brookKey,
                    eventPosition,
                    stepInfo.StepName,
                    stepIndex,
                    errorCode,
                    result.ErrorMessage);
            }

            yield return new SagaStepFailed
            {
                StepIndex = stepIndex,
                StepName = stepInfo.StepName,
                ErrorCode = errorCode,
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