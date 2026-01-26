using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Projections;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.EventSourcing.Sagas;

/// <summary>
///     Orleans grain that orchestrates saga step execution, state management,
///     compensation triggering, and status tracking.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
/// <typeparam name="TInput">The saga input type.</typeparam>
/// <remarks>
///     <para>
///         This is a simplified implementation that keeps state in-memory within
///         the grain. Future versions will integrate with brooks for event persistence.
///     </para>
/// </remarks>
[Alias("Mississippi.EventSourcing.Sagas.SagaGrain`2")]
internal sealed class SagaGrain<TSaga, TInput>
    : ISagaGrain<TInput>,
      IGrainBase
    where TSaga : class, ISagaDefinition, new()
    where TInput : class
{
    private TSaga? currentState;
    private int lastCompletedStepIndex = -1;
    private SagaPhase currentPhase = SagaPhase.NotStarted;
    private DateTimeOffset? startedAt;
    private DateTimeOffset? completedAt;
    private string? failureReason;
    private ImmutableList<SagaStepRecord> completedSteps = [];
    private ImmutableList<SagaStepRecord> failedSteps = [];
    private ImmutableList<SagaStepRecord> compensationFailures = [];

    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaGrain{TSaga,TInput}" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="rootReducer">The root event reducer for the saga state.</param>
    /// <param name="stepRegistry">Registry of saga steps and compensations.</param>
    /// <param name="serviceProvider">Service provider for resolving step instances.</param>
    /// <param name="logger">Logger instance.</param>
    public SagaGrain(
        IGrainContext grainContext,
        IRootReducer<TSaga> rootReducer,
        ISagaStepRegistry<TSaga> stepRegistry,
        IServiceProvider serviceProvider,
        ILogger<SagaGrain<TSaga, TInput>> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        RootReducer = rootReducer ?? throw new ArgumentNullException(nameof(rootReducer));
        StepRegistry = stepRegistry ?? throw new ArgumentNullException(nameof(stepRegistry));
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    private ILogger<SagaGrain<TSaga, TInput>> Logger { get; }

    private IRootReducer<TSaga> RootReducer { get; }

    private IServiceProvider ServiceProvider { get; }

    private Guid SagaId => this.GetPrimaryKey();

    private ISagaStepRegistry<TSaga> StepRegistry { get; }

    /// <inheritdoc />
    public async Task StartAsync(
        TInput input,
        string? correlationId = null
    )
    {
        ArgumentNullException.ThrowIfNull(input);

        if (currentPhase != SagaPhase.NotStarted)
        {
            throw new InvalidOperationException("Saga has already been started.");
        }

        Logger.SagaStarting(SagaId, typeof(TSaga).Name);

        // Create initial saga state from input
        currentState = CreateInitialState(input);
        startedAt = DateTimeOffset.UtcNow;
        currentPhase = SagaPhase.Running;

        // Execute first step
        await ExecuteNextStepAsync(
            currentState,
            correlationId ?? SagaId.ToString(),
            stepIndex: 0);
    }

    /// <inheritdoc />
    public Task<SagaStatusProjection> GetStatusAsync()
    {
        SagaStatusProjection status = new()
        {
            SagaId = SagaId.ToString(),
            SagaType = typeof(TSaga).Name,
            Phase = currentPhase,
            CompletedSteps = completedSteps,
            CurrentStep = null,
            FailedSteps = failedSteps,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            FailureReason = failureReason,
        };

        return Task.FromResult(status);
    }

    /// <inheritdoc />
    public async Task ResumeAsync()
    {
        if (currentState is null)
        {
            throw new InvalidOperationException("Cannot resume a saga that has not started.");
        }

        if (currentPhase != SagaPhase.Running)
        {
            throw new InvalidOperationException($"Cannot resume a saga in phase {currentPhase}.");
        }

        Logger.SagaResuming(SagaId, typeof(TSaga).Name);

        // Resume from the step after the last completed one
        int nextStepIndex = lastCompletedStepIndex + 1;

        await ExecuteNextStepAsync(
            currentState,
            SagaId.ToString(),
            nextStepIndex);
    }

    /// <inheritdoc />
    public async Task CancelAsync(
        string reason
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (currentState is null)
        {
            throw new InvalidOperationException("Cannot cancel a saga that has not started.");
        }

        Logger.SagaCancelling(SagaId, typeof(TSaga).Name, reason);

        // Trigger compensation from the last completed step
        if (lastCompletedStepIndex >= 0)
        {
            await CompensateFromStepAsync(currentState, lastCompletedStepIndex, reason);
        }
        else
        {
            currentPhase = SagaPhase.Failed;
            failureReason = reason;
        }
    }

    /// <inheritdoc />
    public Task OnActivateAsync(
        CancellationToken token
    ) =>
        Task.CompletedTask;

    /// <inheritdoc />
    public Task OnDeactivateAsync(
        DeactivationReason reason,
        CancellationToken token
    ) =>
        Task.CompletedTask;

    private static TSaga CreateInitialState(
        TInput input
    )
    {
        // Use reflection to map input properties to saga state properties
        TSaga state = new();
        Type stateType = typeof(TSaga);
        Type inputType = typeof(TInput);

        foreach (PropertyInfo inputProp in inputType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            PropertyInfo? stateProp = stateType.GetProperty(inputProp.Name, BindingFlags.Public | BindingFlags.Instance);
            if (stateProp is { CanWrite: true } && stateProp.PropertyType == inputProp.PropertyType)
            {
                object? value = inputProp.GetValue(input);
                stateProp.SetValue(state, value);
            }
        }

        return state;
    }

    private async Task ExecuteNextStepAsync(
        TSaga state,
        string correlationId,
        int stepIndex
    )
    {
        IReadOnlyList<ISagaStepInfo> steps = StepRegistry.Steps;

        if (stepIndex >= steps.Count)
        {
            // All steps completed
            currentPhase = SagaPhase.Completed;
            completedAt = DateTimeOffset.UtcNow;
            Logger.SagaCompleted(SagaId, typeof(TSaga).Name);
            return;
        }

        ISagaStepInfo stepInfo = steps[stepIndex];

        Logger.SagaStepStarting(SagaId, stepInfo.Name, stepInfo.Order);

        // Resolve step instance from DI
        object stepInstance = ServiceProvider.GetRequiredService(stepInfo.StepType);

        // Create context
        SagaContext context = new()
        {
            SagaId = SagaId,
            CorrelationId = correlationId,
            SagaName = typeof(TSaga).Name,
            Attempt = 1,
            StartedAt = startedAt ?? DateTimeOffset.UtcNow,
        };

        // Execute step
        StepResult result = await InvokeStepWithExceptionHandlingAsync(stepInstance, context, state, stepInfo);

        if (result.Success)
        {
            // Apply events to state
            currentState = RootReducer.Reduce(state, result.Events);
            lastCompletedStepIndex = stepIndex;

            // Track completed step
            SagaStepRecord stepRecord = new(
                stepInfo.Name,
                stepInfo.Order,
                DateTimeOffset.UtcNow,
                StepOutcome.Succeeded);

            completedSteps = completedSteps.Add(stepRecord);

            Logger.SagaStepCompleted(SagaId, stepInfo.Name, stepInfo.Order);

            // Move to next step
            await ExecuteNextStepAsync(currentState, correlationId, stepIndex + 1);
        }
        else
        {
            // Step failed - record the failure
            SagaStepRecord failedRecord = new(
                stepInfo.Name,
                stepInfo.Order,
                DateTimeOffset.UtcNow,
                StepOutcome.Failed,
                result.ErrorMessage);

            failedSteps = failedSteps.Add(failedRecord);

            Logger.SagaStepFailed(SagaId, stepInfo.Name, stepInfo.Order, result.ErrorCode, result.ErrorMessage);

            // Trigger compensation for previously completed steps
            if (stepIndex > 0)
            {
                await CompensateFromStepAsync(
                    state,
                    stepIndex - 1,
                    result.ErrorMessage ?? "Step failed");
            }
            else
            {
                // No steps to compensate, just fail the saga
                currentPhase = SagaPhase.Failed;
                failureReason = result.ErrorMessage ?? "First step failed.";
            }
        }
    }

    private async Task CompensateFromStepAsync(
        TSaga state,
        int fromStepIndex,
        string reason
    )
    {
        IReadOnlyList<ISagaStepInfo> steps = StepRegistry.Steps;

        currentPhase = SagaPhase.Compensating;

        Logger.SagaCompensating(SagaId, typeof(TSaga).Name, fromStepIndex);

        // Run compensations in reverse order
        for (int i = fromStepIndex; i >= 0; i--)
        {
            ISagaStepInfo stepInfo = steps[i];

            if (stepInfo.CompensationType is null)
            {
                // No compensation defined for this step
                continue;
            }

            try
            {
                object compensationInstance = ServiceProvider.GetRequiredService(stepInfo.CompensationType);

                SagaContext context = new()
                {
                    SagaId = SagaId,
                    CorrelationId = SagaId.ToString(),
                    SagaName = typeof(TSaga).Name,
                    Attempt = 1,
                    StartedAt = startedAt ?? DateTimeOffset.UtcNow,
                };

                await InvokeCompensationAsync(compensationInstance, context, state);

                Logger.SagaStepCompensated(SagaId, stepInfo.Name, stepInfo.Order);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                Logger.SagaCompensationException(SagaId, stepInfo.Name, ex.InnerException);

                compensationFailures = compensationFailures.Add(new SagaStepRecord(
                    stepInfo.Name,
                    stepInfo.Order,
                    DateTimeOffset.UtcNow,
                    StepOutcome.Failed,
                    ex.InnerException.Message));
            }
            catch (InvalidOperationException ex)
            {
                Logger.SagaCompensationException(SagaId, stepInfo.Name, ex);

                compensationFailures = compensationFailures.Add(new SagaStepRecord(
                    stepInfo.Name,
                    stepInfo.Order,
                    DateTimeOffset.UtcNow,
                    StepOutcome.Failed,
                    ex.Message));
            }
        }

        currentPhase = SagaPhase.Failed;
        failureReason = reason;

        Logger.SagaFailed(SagaId, typeof(TSaga).Name, reason);
    }

    private async Task<StepResult> InvokeStepWithExceptionHandlingAsync(
        object stepInstance,
        ISagaContext context,
        TSaga state,
        ISagaStepInfo stepInfo
    )
    {
        try
        {
            return await InvokeStepAsync(stepInstance, context, state);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            Logger.SagaStepException(SagaId, stepInfo.Name, stepInfo.Order, ex.InnerException);
            return StepResult.Failed("EXCEPTION", ex.InnerException.Message);
        }
        catch (InvalidOperationException ex)
        {
            Logger.SagaStepException(SagaId, stepInfo.Name, stepInfo.Order, ex);
            return StepResult.Failed("INVALID_OPERATION", ex.Message);
        }
    }

    private static async Task<StepResult> InvokeStepAsync(
        object stepInstance,
        ISagaContext context,
        TSaga state
    )
    {
        // Find and invoke ExecuteAsync method
        Type stepType = stepInstance.GetType();
        MethodInfo? executeMethod = stepType.GetMethod(
            "ExecuteAsync",
            [typeof(ISagaContext), typeof(TSaga), typeof(CancellationToken)]);

        if (executeMethod is null)
        {
            throw new InvalidOperationException(
                $"Step type {stepType.Name} does not have an ExecuteAsync method with expected signature.");
        }

        Task<StepResult>? task = executeMethod.Invoke(
            stepInstance,
            [context, state, CancellationToken.None]) as Task<StepResult>;

        return task is not null
            ? await task
            : throw new InvalidOperationException(
                $"Step type {stepType.Name}.ExecuteAsync did not return Task<StepResult>.");
    }

    private static async Task<CompensationResult> InvokeCompensationAsync(
        object compensationInstance,
        ISagaContext context,
        TSaga state
    )
    {
        Type compensationType = compensationInstance.GetType();
        MethodInfo? compensateMethod = compensationType.GetMethod(
            "CompensateAsync",
            [typeof(ISagaContext), typeof(TSaga), typeof(CancellationToken)]);

        if (compensateMethod is null)
        {
            throw new InvalidOperationException(
                $"Compensation type {compensationType.Name} does not have a CompensateAsync method with expected signature.");
        }

        Task<CompensationResult>? task = compensateMethod.Invoke(
            compensationInstance,
            [context, state, CancellationToken.None]) as Task<CompensationResult>;

        return task is not null
            ? await task
            : throw new InvalidOperationException(
                $"Compensation type {compensationType.Name}.CompensateAsync did not return Task<CompensationResult>.");
    }
}
