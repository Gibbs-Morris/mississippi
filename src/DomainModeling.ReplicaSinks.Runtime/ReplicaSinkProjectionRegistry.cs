using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.DomainModeling.ReplicaSinks.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Stores the replica sink binding descriptors and startup diagnostics computed during startup.
/// </summary>
internal sealed class ReplicaSinkProjectionRegistry : IReplicaSinkProjectionRegistry
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkProjectionRegistry" /> class.
    /// </summary>
    /// <param name="serviceProvider">The application service provider.</param>
    /// <param name="projectionBindings">The discovered projection bindings.</param>
    /// <param name="registrationDescriptors">The registered sink descriptors.</param>
    public ReplicaSinkProjectionRegistry(
        IServiceProvider serviceProvider,
        IEnumerable<ReplicaSinkProjectionDescriptor>? projectionBindings = null,
        IEnumerable<ReplicaSinkRegistrationDescriptor>? registrationDescriptors = null
    ) =>
        (BindingDescriptors, Diagnostics) = BuildCache(serviceProvider, projectionBindings, registrationDescriptors);

    private IReadOnlyList<ReplicaSinkBindingDescriptor> BindingDescriptors { get; }

    private IReadOnlyList<ReplicaSinkStartupDiagnostic> Diagnostics { get; }

    private static (IReadOnlyList<ReplicaSinkBindingDescriptor> BindingDescriptors,
        IReadOnlyList<ReplicaSinkStartupDiagnostic> Diagnostics) BuildCache(
            IServiceProvider serviceProvider,
            IEnumerable<ReplicaSinkProjectionDescriptor>? projectionBindings,
            IEnumerable<ReplicaSinkRegistrationDescriptor>? registrationDescriptors
        )
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        List<ReplicaSinkProjectionDescriptor> discoveredBindings = projectionBindings
                                                                       ?.OrderBy(
                                                                           binding => ReplicaSinkStartupDiagnostics
                                                                               .GetTypeDisplayName(
                                                                                   binding.ProjectionType),
                                                                           StringComparer.Ordinal)
                                                                       .ThenBy(
                                                                           binding => binding.SinkKey,
                                                                           StringComparer.Ordinal)
                                                                       .ThenBy(
                                                                           binding => binding.TargetName,
                                                                           StringComparer.Ordinal)
                                                                       .ThenBy(
                                                                           binding => binding.ContractType is null
                                                                               ? string.Empty
                                                                               : ReplicaSinkStartupDiagnostics
                                                                                   .GetTypeDisplayName(
                                                                                       binding.ContractType),
                                                                           StringComparer.Ordinal)
                                                                       .ThenBy(binding =>
                                                                           binding.IsDirectProjectionReplicationEnabled)
                                                                       .ThenBy(binding => binding.WriteMode)
                                                                       .ToList() ??
                                                                   [];
        List<ReplicaSinkRegistrationDescriptor> registrations = registrationDescriptors
                                                                    ?.OrderBy(
                                                                        descriptor => descriptor.SinkKey,
                                                                        StringComparer.Ordinal)
                                                                    .ThenBy(
                                                                        descriptor => descriptor.ClientKey,
                                                                        StringComparer.Ordinal)
                                                                    .ThenBy(
                                                                        descriptor => descriptor.Format,
                                                                        StringComparer.Ordinal)
                                                                    .ThenBy(
                                                                        descriptor =>
                                                                            ReplicaSinkStartupDiagnostics
                                                                                .GetTypeDisplayName(
                                                                                    descriptor.ProviderType),
                                                                        StringComparer.Ordinal)
                                                                    .ToList() ??
                                                                [];
        Dictionary<string, List<ReplicaSinkRegistrationDescriptor>> registrationsBySinkKey = registrations
            .GroupBy(descriptor => descriptor.SinkKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        Dictionary<ReplicaSinkBindingIdentity, int> logicalBindingCounts = discoveredBindings
            .GroupBy(CreateBindingIdentity)
            .ToDictionary(group => group.Key, group => group.Count());
        List<ReplicaSinkStartupDiagnostic> diagnostics = logicalBindingCounts.Where(pair => pair.Value > 1)
            .OrderBy(pair => pair.Key.ProjectionTypeName, StringComparer.Ordinal)
            .ThenBy(pair => pair.Key.SinkKey, StringComparer.Ordinal)
            .ThenBy(pair => pair.Key.TargetName, StringComparer.Ordinal)
            .Select(pair => ReplicaSinkStartupDiagnostics.CreateDuplicateLogicalBinding(pair.Key))
            .ToList();
        List<ReplicaSinkBindingDescriptor> provisionalDescriptors = [];
        List<(ReplicaSinkBindingIdentity BindingIdentity, string PhysicalTargetKey, ReplicaSinkRegistrationDescriptor
            RegistrationDescriptor, ReplicaTargetDescriptor TargetDescriptor)> physicalTargetCandidates = [];
        foreach (ReplicaSinkProjectionDescriptor binding in discoveredBindings)
        {
            ReplicaSinkBindingIdentity bindingIdentity = CreateBindingIdentity(binding);
            bool hasBindingDiagnostics = false;
            if (binding.WriteMode == ReplicaWriteMode.History)
            {
                diagnostics.Add(ReplicaSinkStartupDiagnostics.CreateUnsupportedHistoryWriteMode(binding));
                hasBindingDiagnostics = true;
            }

            if (!registrationsBySinkKey.TryGetValue(
                    binding.SinkKey,
                    out List<ReplicaSinkRegistrationDescriptor>? matchingRegistrations))
            {
                diagnostics.Add(ReplicaSinkStartupDiagnostics.CreateMissingSinkRegistration(binding));
                continue;
            }

            if (matchingRegistrations.Count != 1)
            {
                diagnostics.Add(
                    ReplicaSinkStartupDiagnostics.CreateInvalidSinkRegistrationMultiplicity(
                        binding,
                        matchingRegistrations.Count));
                continue;
            }

            ReplicaSinkRegistrationDescriptor registrationDescriptor = matchingRegistrations[0];
            IReplicaSinkProvider? providerHandle =
                serviceProvider.GetKeyedService<IReplicaSinkProvider>(binding.SinkKey);
            if (providerHandle is null)
            {
                diagnostics.Add(ReplicaSinkStartupDiagnostics.CreateMissingProviderHandle(binding));
                continue;
            }

            ReplicaTargetDescriptor validatedTargetDescriptor = new(
                new(registrationDescriptor.ClientKey, binding.TargetName),
                registrationDescriptor.ProvisioningMode);
            physicalTargetCandidates.Add(
                (bindingIdentity, CreatePhysicalTargetKey(registrationDescriptor, validatedTargetDescriptor),
                    registrationDescriptor, validatedTargetDescriptor));
            string? contractIdentity = null;
            Func<object, object>? mapperDelegate = null;
            bool usesDirectMaterialization = false;
            if (binding.ContractType is null)
            {
                if (!binding.IsDirectProjectionReplicationEnabled)
                {
                    diagnostics.Add(
                        ReplicaSinkStartupDiagnostics.CreateDirectReplicationRequiresExplicitOptIn(binding));
                    hasBindingDiagnostics = true;
                }
                else
                {
                    usesDirectMaterialization = true;
                    contractIdentity = ReplicaSinkStartupDiagnostics.GetTypeDisplayName(binding.ProjectionType);
                }
            }
            else
            {
                ReplicaContractNameAttribute? contractNameAttribute =
                    binding.ContractType.GetCustomAttribute<ReplicaContractNameAttribute>();
                if (contractNameAttribute is null)
                {
                    diagnostics.Add(ReplicaSinkStartupDiagnostics.CreateMissingReplicaContractName(binding));
                    hasBindingDiagnostics = true;
                }
                else
                {
                    contractIdentity = contractNameAttribute.ContractIdentity;
                }

                object? mapper = serviceProvider.GetService(
                    typeof(IMapper<,>).MakeGenericType(binding.ProjectionType, binding.ContractType));
                if (mapper is null)
                {
                    diagnostics.Add(ReplicaSinkStartupDiagnostics.CreateMissingMapper(binding));
                    hasBindingDiagnostics = true;
                }
                else
                {
                    mapperDelegate = CreateMapperDelegate(binding.ProjectionType, binding.ContractType, mapper);
                }
            }

            if ((!logicalBindingCounts.TryGetValue(bindingIdentity, out int logicalBindingCount) ||
                 (logicalBindingCount == 1)) &&
                !hasBindingDiagnostics &&
                contractIdentity is not null &&
                (usesDirectMaterialization || mapperDelegate is not null))
            {
                provisionalDescriptors.Add(
                    new(
                        bindingIdentity,
                        binding.ProjectionType,
                        binding.WriteMode,
                        binding.ContractType,
                        contractIdentity,
                        mapperDelegate,
                        usesDirectMaterialization,
                        registrationDescriptor,
                        providerHandle,
                        validatedTargetDescriptor));
            }
        }

        HashSet<ReplicaSinkBindingIdentity> overlappingBindingIdentities = [];
        foreach (IGrouping<string, (ReplicaSinkBindingIdentity BindingIdentity, string PhysicalTargetKey,
                     ReplicaSinkRegistrationDescriptor RegistrationDescriptor, ReplicaTargetDescriptor TargetDescriptor
                     )> group in physicalTargetCandidates
                     .GroupBy(candidate => candidate.PhysicalTargetKey, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1)
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            diagnostics.Add(
                ReplicaSinkStartupDiagnostics.CreatePhysicalTargetOverlap(
                    group.Select(candidate => candidate.BindingIdentity),
                    group.First().RegistrationDescriptor,
                    group.First().TargetDescriptor));
            foreach ((ReplicaSinkBindingIdentity BindingIdentity, string PhysicalTargetKey,
                     ReplicaSinkRegistrationDescriptor RegistrationDescriptor, ReplicaTargetDescriptor TargetDescriptor)
                     candidate in group)
            {
                overlappingBindingIdentities.Add(candidate.BindingIdentity);
            }
        }

        IReadOnlyList<ReplicaSinkBindingDescriptor> finalDescriptors = provisionalDescriptors
            .Where(descriptor => !overlappingBindingIdentities.Contains(descriptor.BindingIdentity))
            .OrderBy(descriptor => descriptor.BindingIdentity.ProjectionTypeName, StringComparer.Ordinal)
            .ThenBy(descriptor => descriptor.SinkKey, StringComparer.Ordinal)
            .ThenBy(descriptor => descriptor.TargetName, StringComparer.Ordinal)
            .ToArray();
        return (finalDescriptors, diagnostics.ToArray());
    }

    private static ReplicaSinkBindingIdentity CreateBindingIdentity(
        ReplicaSinkProjectionDescriptor binding
    ) =>
        new(
            ReplicaSinkStartupDiagnostics.GetTypeDisplayName(binding.ProjectionType),
            binding.SinkKey,
            binding.TargetName);

    private static Func<object, object> CreateMapperDelegate(
        Type projectionType,
        Type contractType,
        object mapper
    )
    {
        ArgumentNullException.ThrowIfNull(projectionType);
        ArgumentNullException.ThrowIfNull(contractType);
        ArgumentNullException.ThrowIfNull(mapper);
        Type mapperType = typeof(IMapper<,>).MakeGenericType(projectionType, contractType);
        MethodInfo mapMethod = mapperType.GetMethod(nameof(IMapper<object, object>.Map), [projectionType]) ??
                               throw new InvalidOperationException(
                                   $"Mapper '{mapperType}' does not expose a supported Map method.");
        ParameterExpression input = Expression.Parameter(typeof(object), "input");
        UnaryExpression typedMapper = Expression.Convert(Expression.Constant(mapper), mapperType);
        UnaryExpression typedInput = Expression.Convert(input, projectionType);
        MethodCallExpression mapCall = Expression.Call(typedMapper, mapMethod, typedInput);
        UnaryExpression boxedResult = Expression.Convert(mapCall, typeof(object));
        return Expression.Lambda<Func<object, object>>(boxedResult, input).Compile();
    }

    private static string CreatePhysicalTargetKey(
        ReplicaSinkRegistrationDescriptor registrationDescriptor,
        ReplicaTargetDescriptor targetDescriptor
    ) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{ReplicaSinkStartupDiagnostics.GetTypeDisplayName(registrationDescriptor.ProviderType)}|{registrationDescriptor.ClientKey}|{targetDescriptor.DestinationIdentity.TargetName}");

    /// <inheritdoc />
    public IReadOnlyList<ReplicaSinkBindingDescriptor> GetBindingDescriptors() => BindingDescriptors;

    /// <inheritdoc />
    public IReadOnlyList<ReplicaSinkStartupDiagnostic> GetDiagnostics() => Diagnostics;
}