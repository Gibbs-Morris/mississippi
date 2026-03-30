using System;
using System.Collections.Generic;
using System.Linq;

using Mississippi.DomainModeling.ReplicaSinks.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Publishes stable diagnostic identifiers and message factories for replica sink startup validation.
/// </summary>
internal static class ReplicaSinkStartupDiagnostics
{
    /// <summary>
    ///     Gets the direct replication opt-in diagnostic identifier.
    /// </summary>
    public const string DirectReplicationRequiresExplicitOptInId = "RS0005";

    /// <summary>
    ///     Gets the duplicate logical binding diagnostic identifier.
    /// </summary>
    public const string DuplicateLogicalBindingId = "RS0004";

    /// <summary>
    ///     Gets the missing mapper diagnostic identifier.
    /// </summary>
    public const string MissingMapperId = "RS0003";

    /// <summary>
    ///     Gets the missing replica contract identity diagnostic identifier.
    /// </summary>
    public const string MissingReplicaContractId = "RS0002";

    /// <summary>
    ///     Gets the runtime prerequisite diagnostic identifier.
    /// </summary>
    public const string MissingRuntimePrerequisiteId = "RS0008";

    /// <summary>
    ///     Gets the missing sink registration diagnostic identifier.
    /// </summary>
    public const string MissingSinkRegistrationId = "RS0001";

    /// <summary>
    ///     Gets the overlapping physical target diagnostic identifier.
    /// </summary>
    public const string OverlappingPhysicalTargetId = "RS0007";

    /// <summary>
    ///     Gets the unsupported history mode diagnostic identifier.
    /// </summary>
    public const string UnsupportedHistoryWriteModeId = "RS0006";

    /// <summary>
    ///     Creates an <c>RS0005</c> diagnostic.
    /// </summary>
    /// <param name="binding">The failing binding.</param>
    /// <returns>The diagnostic.</returns>
    public static ReplicaSinkStartupDiagnostic CreateDirectReplicationRequiresExplicitOptIn(
        ReplicaSinkProjectionDescriptor binding
    ) =>
        new(
            DirectReplicationRequiresExplicitOptInId,
            $"Projection '{GetTypeDisplayName(binding.ProjectionType)}' declares replica sink '{binding.SinkKey}' for target '{binding.TargetName}' without a contract type, but direct projection replication is not explicitly enabled. Set IsDirectProjectionReplicationEnabled = true or configure a mapped replica contract and mapper.");

    /// <summary>
    ///     Creates an <c>RS0004</c> diagnostic.
    /// </summary>
    /// <param name="bindingIdentity">The duplicate binding identity.</param>
    /// <returns>The diagnostic.</returns>
    public static ReplicaSinkStartupDiagnostic CreateDuplicateLogicalBinding(
        ReplicaSinkBindingIdentity bindingIdentity
    ) =>
        new(
            DuplicateLogicalBindingId,
            $"Projection '{bindingIdentity.ProjectionTypeName}' declares duplicate logical replica binding for sink '{bindingIdentity.SinkKey}' and target '{bindingIdentity.TargetName}'. Keep only one binding per projection, sink, and target combination.");

    /// <summary>
    ///     Creates an <c>RS0008</c> diagnostic for ambiguous sink registration descriptors.
    /// </summary>
    /// <param name="binding">The failing binding.</param>
    /// <param name="registrationCount">The conflicting registration count.</param>
    /// <returns>The diagnostic.</returns>
    public static ReplicaSinkStartupDiagnostic CreateInvalidSinkRegistrationMultiplicity(
        ReplicaSinkProjectionDescriptor binding,
        int registrationCount
    ) =>
        new(
            MissingRuntimePrerequisiteId,
            $"Projection '{GetTypeDisplayName(binding.ProjectionType)}' declares replica sink '{binding.SinkKey}' for target '{binding.TargetName}', but sink '{binding.SinkKey}' resolved to {registrationCount} provider registrations. Register each sink key exactly once so startup validation can resolve a single provider handle.");

    /// <summary>
    ///     Creates an <c>RS0003</c> diagnostic.
    /// </summary>
    /// <param name="binding">The failing binding.</param>
    /// <returns>The diagnostic.</returns>
    public static ReplicaSinkStartupDiagnostic CreateMissingMapper(
        ReplicaSinkProjectionDescriptor binding
    ) =>
        new(
            MissingMapperId,
            $"Projection '{GetTypeDisplayName(binding.ProjectionType)}' declares replica sink '{binding.SinkKey}' for target '{binding.TargetName}' with contract '{GetTypeDisplayName(binding.ContractType)}', but no IMapper<{GetTypeDisplayName(binding.ProjectionType)}, {GetTypeDisplayName(binding.ContractType)}> is registered. Register the mapper so the runtime can materialize replica payloads.");

    /// <summary>
    ///     Creates an <c>RS0008</c> diagnostic for a missing keyed provider handle.
    /// </summary>
    /// <param name="binding">The failing binding.</param>
    /// <returns>The diagnostic.</returns>
    public static ReplicaSinkStartupDiagnostic CreateMissingProviderHandle(
        ReplicaSinkProjectionDescriptor binding
    ) =>
        new(
            MissingRuntimePrerequisiteId,
            $"Projection '{GetTypeDisplayName(binding.ProjectionType)}' declares replica sink '{binding.SinkKey}' for target '{binding.TargetName}', but the runtime could not resolve an IReplicaSinkProvider keyed by '{binding.SinkKey}'. Ensure AddReplicaSinks() and the matching Add<Provider>ReplicaSink(...) registration are both configured before startup.");

    /// <summary>
    ///     Creates an <c>RS0002</c> diagnostic.
    /// </summary>
    /// <param name="binding">The failing binding.</param>
    /// <returns>The diagnostic.</returns>
    public static ReplicaSinkStartupDiagnostic CreateMissingReplicaContractName(
        ReplicaSinkProjectionDescriptor binding
    ) =>
        new(
            MissingReplicaContractId,
            $"Projection '{GetTypeDisplayName(binding.ProjectionType)}' declares replica sink '{binding.SinkKey}' for target '{binding.TargetName}' with contract '{GetTypeDisplayName(binding.ContractType)}', but that contract does not declare [ReplicaContractName]. Annotate the contract with ReplicaContractNameAttribute so the runtime can compute a stable contract identity.");

    /// <summary>
    ///     Creates an <c>RS0001</c> diagnostic.
    /// </summary>
    /// <param name="binding">The failing binding.</param>
    /// <returns>The diagnostic.</returns>
    public static ReplicaSinkStartupDiagnostic CreateMissingSinkRegistration(
        ReplicaSinkProjectionDescriptor binding
    ) =>
        new(
            MissingSinkRegistrationId,
            $"Projection '{GetTypeDisplayName(binding.ProjectionType)}' declares replica sink '{binding.SinkKey}' for target '{binding.TargetName}', but no sink registration was found. Register the sink with Add<Provider>ReplicaSink(\"{binding.SinkKey}\", ...) before startup.");

    /// <summary>
    ///     Creates an <c>RS0007</c> diagnostic.
    /// </summary>
    /// <param name="bindingIdentities">The overlapping logical bindings.</param>
    /// <param name="registrationDescriptor">The shared registration descriptor.</param>
    /// <param name="targetDescriptor">The overlapping physical target.</param>
    /// <returns>The diagnostic.</returns>
    public static ReplicaSinkStartupDiagnostic CreatePhysicalTargetOverlap(
        IEnumerable<ReplicaSinkBindingIdentity> bindingIdentities,
        ReplicaSinkRegistrationDescriptor registrationDescriptor,
        ReplicaTargetDescriptor targetDescriptor
    )
    {
        ArgumentNullException.ThrowIfNull(bindingIdentities);
        ArgumentNullException.ThrowIfNull(registrationDescriptor);
        ArgumentNullException.ThrowIfNull(targetDescriptor);
        string bindings = string.Join(
            ", ",
            bindingIdentities.OrderBy(identity => identity.ProjectionTypeName, StringComparer.Ordinal)
                .ThenBy(identity => identity.SinkKey, StringComparer.Ordinal)
                .ThenBy(identity => identity.TargetName, StringComparer.Ordinal)
                .Select(identity => $"'{identity}'"));
        return new(
            OverlappingPhysicalTargetId,
            $"Physical target '{targetDescriptor.DestinationIdentity}' for provider '{GetTypeDisplayName(registrationDescriptor.ProviderType)}' is used by multiple logical replica bindings: {bindings}. Use a unique sink registration or target name for each binding.");
    }

    /// <summary>
    ///     Creates an <c>RS0006</c> diagnostic.
    /// </summary>
    /// <param name="binding">The failing binding.</param>
    /// <returns>The diagnostic.</returns>
    public static ReplicaSinkStartupDiagnostic CreateUnsupportedHistoryWriteMode(
        ReplicaSinkProjectionDescriptor binding
    ) =>
        new(
            UnsupportedHistoryWriteModeId,
            $"Projection '{GetTypeDisplayName(binding.ProjectionType)}' declares replica sink '{binding.SinkKey}' for target '{binding.TargetName}' with write mode '{binding.WriteMode}'. Slice 1 supports only '{ReplicaWriteMode.LatestState}'. Switch the binding to LatestState.");

    /// <summary>
    ///     Formats the startup validation failure message.
    /// </summary>
    /// <param name="diagnostics">The startup diagnostics.</param>
    /// <returns>The formatted failure message.</returns>
    public static string FormatValidationFailure(
        IReadOnlyList<ReplicaSinkStartupDiagnostic> diagnostics
    ) =>
        diagnostics.Count == 0
            ? "Replica sink startup validation failed."
            : $"Replica sink startup validation failed.{Environment.NewLine}{string.Join(Environment.NewLine, diagnostics.Select(diagnostic => diagnostic.ToString()))}";

    /// <summary>
    ///     Gets a stable display name for a type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The stable display name.</returns>
    public static string GetTypeDisplayName(
        Type? type
    ) =>
        type?.FullName ?? type?.Name ?? "<unknown>";
}