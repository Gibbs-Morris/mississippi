using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas;

/// <summary>
///     Default implementation of <see cref="ISagaStepRegistry{TSaga}" /> using reflection-based discovery.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class SagaStepRegistry<TSaga> : ISagaStepRegistry<TSaga>
    where TSaga : class
{
    private readonly Lazy<string> lazyStepHash;

    private readonly Lazy<IReadOnlyList<ISagaStepInfo>> lazySteps;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaStepRegistry{TSaga}" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for discovering registered step types.</param>
    public SagaStepRegistry(
        IServiceProvider serviceProvider
    )
    {
        ServiceProvider = serviceProvider;
        lazySteps = new(DiscoverSteps);
        lazyStepHash = new(() => ComputeStepHash(Steps));
    }

    /// <inheritdoc />
    public string StepHash => lazyStepHash.Value;

    /// <inheritdoc />
    public IReadOnlyList<ISagaStepInfo> Steps => lazySteps.Value;

    private IServiceProvider ServiceProvider { get; }

    private static string ComputeStepHash(
        IReadOnlyList<ISagaStepInfo> steps
    )
    {
        StringBuilder sb = new();
        foreach (ISagaStepInfo step in steps)
        {
            sb.Append(step.Order);
            sb.Append(':');
            sb.Append(step.Name);
            sb.Append(':');
            sb.Append(step.Timeout?.TotalSeconds ?? 0);
            sb.Append(':');
            sb.Append(step.CompensationType?.Name ?? "none");
            sb.Append(';');
        }

        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToBase64String(hashBytes)[..12];
    }

    private static TimeSpan? ParseTimeout(
        string? timeoutString
    )
    {
        if (string.IsNullOrEmpty(timeoutString))
        {
            return null;
        }

        return TimeSpan.TryParse(timeoutString, CultureInfo.InvariantCulture, out TimeSpan timeout) ? timeout : null;
    }

    private List<SagaStepInfo> DiscoverSteps()
    {
        Type sagaType = typeof(TSaga);
        Assembly sagaAssembly = sagaType.Assembly;

        // Find all step types that extend SagaStepBase<TSaga>
        Type stepBaseType = typeof(SagaStepBase<TSaga>);
        List<(Type StepType, SagaStepAttribute Attribute)> stepTypes = sagaAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && stepBaseType.IsAssignableFrom(t))
            .Select(t => (StepType: t, Attribute: t.GetCustomAttribute<SagaStepAttribute>()))
            .Where(tuple => tuple.Attribute is not null)
            .Select(tuple => (tuple.StepType, tuple.Attribute!))
            .ToList();

        // Find all compensation types
        Type compensationBaseType = typeof(SagaCompensationBase<TSaga>);
        Dictionary<Type, Type> compensationsByStep = sagaAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && compensationBaseType.IsAssignableFrom(t))
            .Select(t => (CompensationType: t, Attribute: t.GetCustomAttribute<SagaCompensationAttribute>()))
            .Where(tuple => tuple.Attribute is not null)
            .ToDictionary(tuple => tuple.Attribute!.ForStep, tuple => tuple.CompensationType);

        // Build step info list ordered by step order
        List<SagaStepInfo> steps = stepTypes.OrderBy(tuple => tuple.Attribute.Order)
            .Select(tuple => new SagaStepInfo
            {
                Order = tuple.Attribute.Order,
                StepType = tuple.StepType,
                Name = tuple.StepType.Name,
                Timeout = ParseTimeout(tuple.Attribute.Timeout),
                CompensationType = compensationsByStep.GetValueOrDefault(tuple.StepType),
            })
            .ToList();
        return steps;
    }
}