using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Describes a saga step for orchestration.
/// </summary>
public sealed record SagaStepInfo
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaStepInfo" /> record.
    /// </summary>
    /// <param name="stepIndex">The zero-based step index.</param>
    /// <param name="stepName">The step name.</param>
    /// <param name="stepType">The step implementation type.</param>
    /// <param name="hasCompensation">Whether the step supports compensation.</param>
    public SagaStepInfo(
        int stepIndex,
        string stepName,
        Type stepType,
        bool hasCompensation
    )
    {
        ArgumentNullException.ThrowIfNull(stepName);
        ArgumentNullException.ThrowIfNull(stepType);
        StepIndex = stepIndex;
        StepName = stepName;
        StepType = stepType;
        HasCompensation = hasCompensation;
    }

    /// <summary>
    ///     Gets the zero-based step index.
    /// </summary>
    public int StepIndex { get; }

    /// <summary>
    ///     Gets the step name.
    /// </summary>
    public string StepName { get; }

    /// <summary>
    ///     Gets the step implementation type.
    /// </summary>
    public Type StepType { get; }

    /// <summary>
    ///     Gets a value indicating whether the step supports compensation.
    /// </summary>
    public bool HasCompensation { get; }
}

/// <summary>
///     Provides saga step metadata for orchestration.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public interface ISagaStepInfoProvider<TSaga>
    where TSaga : class, ISagaState
{
    /// <summary>
    ///     Gets the ordered set of saga step metadata.
    /// </summary>
    IReadOnlyList<SagaStepInfo> Steps { get; }
}

/// <summary>
///     Default implementation of <see cref="ISagaStepInfoProvider{TSaga}" />.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public sealed class SagaStepInfoProvider<TSaga> : ISagaStepInfoProvider<TSaga>
    where TSaga : class, ISagaState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaStepInfoProvider{TSaga}" /> class.
    /// </summary>
    /// <param name="steps">The ordered step metadata.</param>
    public SagaStepInfoProvider(
        IReadOnlyList<SagaStepInfo> steps
    )
    {
        ArgumentNullException.ThrowIfNull(steps);
        Steps = steps;
    }

    /// <inheritdoc />
    public IReadOnlyList<SagaStepInfo> Steps { get; }
}

/// <summary>
///     Provides registration helpers for saga step metadata.
/// </summary>
public static class SagaStepInfoRegistrations
{
    /// <summary>
    ///     Adds saga step metadata to the service collection.
    /// </summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="steps">The ordered step metadata.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddSagaStepInfo<TSaga>(
        this IServiceCollection services,
        IReadOnlyList<SagaStepInfo> steps
    )
        where TSaga : class, ISagaState
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(steps);
        services.TryAddSingleton<ISagaStepInfoProvider<TSaga>>(_ => new SagaStepInfoProvider<TSaga>(steps));
        return services;
    }
}
