using System;
using System.Collections.Generic;
using System.Linq;

using Mississippi.DomainModeling.ReplicaSinks.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Performs the narrow Increment 1 startup validation needed for runnable onboarding.
/// </summary>
internal sealed class ReplicaSinkStartupValidator : IReplicaSinkStartupValidator
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkStartupValidator" /> class.
    /// </summary>
    /// <param name="projectionRegistry">The discovered projection bindings.</param>
    /// <param name="registrationDescriptors">The registered sink descriptors.</param>
    public ReplicaSinkStartupValidator(
        IReplicaSinkProjectionRegistry projectionRegistry,
        IEnumerable<ReplicaSinkRegistrationDescriptor> registrationDescriptors
    )
    {
        ArgumentNullException.ThrowIfNull(projectionRegistry);
        ArgumentNullException.ThrowIfNull(registrationDescriptors);
        ProjectionRegistry = projectionRegistry;
        RegistrationDescriptors = registrationDescriptors.ToArray();
    }

    private IReplicaSinkProjectionRegistry ProjectionRegistry { get; }

    private IReadOnlyList<ReplicaSinkRegistrationDescriptor> RegistrationDescriptors { get; }

    /// <inheritdoc />
    public void Validate()
    {
        IReadOnlyList<ReplicaSinkProjectionDescriptor> bindings = ProjectionRegistry.GetProjectionBindings();
        HashSet<string> registeredSinkKeys = RegistrationDescriptors.Select(descriptor => descriptor.SinkKey)
            .ToHashSet(StringComparer.Ordinal);
        List<string> errors = [];
        foreach (ReplicaSinkProjectionDescriptor binding in bindings)
        {
            if (!registeredSinkKeys.Contains(binding.SinkKey))
            {
                errors.Add(
                    $"Projection '{binding.ProjectionType.FullName}' declares replica sink '{binding.SinkKey}' for target '{binding.TargetName}', but no sink registration was found.");
            }

            if (binding.ContractType is null && !binding.IsDirectProjectionReplicationEnabled)
            {
                errors.Add(
                    $"Projection '{binding.ProjectionType.FullName}' declares replica sink '{binding.SinkKey}' for target '{binding.TargetName}' without a contract type. Direct projection replication must be explicitly enabled.");
            }

            if (binding.WriteMode == ReplicaWriteMode.History)
            {
                errors.Add(
                    $"Projection '{binding.ProjectionType.FullName}' declares replica sink '{binding.SinkKey}' for target '{binding.TargetName}' with write mode 'History'. Slice 1 supports only 'LatestState'.");
            }
        }

        if (errors.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Replica sink startup validation failed.{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
    }
}