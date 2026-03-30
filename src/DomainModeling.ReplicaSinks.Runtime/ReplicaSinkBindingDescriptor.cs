using System;

using Mississippi.DomainModeling.ReplicaSinks.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Represents a precomputed immutable runtime binding descriptor for replica sink delivery.
/// </summary>
internal sealed class ReplicaSinkBindingDescriptor
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkBindingDescriptor" /> class.
    /// </summary>
    /// <param name="bindingIdentity">The logical binding identity.</param>
    /// <param name="projectionType">The projection type.</param>
    /// <param name="writeMode">The configured write mode.</param>
    /// <param name="contractType">The mapped contract type, when present.</param>
    /// <param name="contractIdentity">The stable contract identity string.</param>
    /// <param name="mapperDelegate">The cached mapper delegate, when mapped replication is used.</param>
    /// <param name="usesDirectMaterialization">A value indicating whether direct materialization is used.</param>
    /// <param name="registrationDescriptor">The contributing sink registration descriptor.</param>
    /// <param name="providerHandle">The cached keyed provider handle.</param>
    /// <param name="validatedTargetDescriptor">The validated destination descriptor.</param>
    public ReplicaSinkBindingDescriptor(
        ReplicaSinkBindingIdentity bindingIdentity,
        Type projectionType,
        ReplicaWriteMode writeMode,
        Type? contractType,
        string contractIdentity,
        Func<object, object>? mapperDelegate,
        bool usesDirectMaterialization,
        ReplicaSinkRegistrationDescriptor registrationDescriptor,
        IReplicaSinkProvider providerHandle,
        ReplicaTargetDescriptor validatedTargetDescriptor
    )
    {
        ArgumentNullException.ThrowIfNull(bindingIdentity);
        ArgumentNullException.ThrowIfNull(projectionType);
        ArgumentNullException.ThrowIfNull(contractIdentity);
        ArgumentNullException.ThrowIfNull(registrationDescriptor);
        ArgumentNullException.ThrowIfNull(providerHandle);
        ArgumentNullException.ThrowIfNull(validatedTargetDescriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(contractIdentity);
        BindingIdentity = bindingIdentity;
        ProjectionType = projectionType;
        WriteMode = writeMode;
        ContractType = contractType;
        ContractIdentity = contractIdentity;
        MapperDelegate = mapperDelegate;
        UsesDirectMaterialization = usesDirectMaterialization;
        RegistrationDescriptor = registrationDescriptor;
        ProviderHandle = providerHandle;
        ValidatedTargetDescriptor = validatedTargetDescriptor;
    }

    /// <summary>
    ///     Gets the logical binding identity.
    /// </summary>
    public ReplicaSinkBindingIdentity BindingIdentity { get; }

    /// <summary>
    ///     Gets the stable contract identity string.
    /// </summary>
    public string ContractIdentity { get; }

    /// <summary>
    ///     Gets the mapped contract type, when present.
    /// </summary>
    public Type? ContractType { get; }

    /// <summary>
    ///     Gets the logical binding identity.
    /// </summary>
    public ReplicaSinkBindingIdentity Identity => BindingIdentity;

    /// <summary>
    ///     Gets the cached mapper delegate, when mapped replication is used.
    /// </summary>
    public Func<object, object>? MapperDelegate { get; }

    /// <summary>
    ///     Gets the projection type.
    /// </summary>
    public Type ProjectionType { get; }

    /// <summary>
    ///     Gets the cached provider handle.
    /// </summary>
    public IReplicaSinkProvider ProviderHandle { get; }

    /// <summary>
    ///     Gets the contributing sink registration descriptor.
    /// </summary>
    public ReplicaSinkRegistrationDescriptor RegistrationDescriptor { get; }

    /// <summary>
    ///     Gets the named sink key.
    /// </summary>
    public string SinkKey => BindingIdentity.SinkKey;

    /// <summary>
    ///     Gets the validated target descriptor.
    /// </summary>
    public ReplicaTargetDescriptor Target => ValidatedTargetDescriptor;

    /// <summary>
    ///     Gets the provider-neutral target name.
    /// </summary>
    public string TargetName => BindingIdentity.TargetName;

    /// <summary>
    ///     Gets a value indicating whether direct materialization is used.
    /// </summary>
    public bool UsesDirectMaterialization { get; }

    /// <summary>
    ///     Gets the validated destination descriptor.
    /// </summary>
    public ReplicaTargetDescriptor ValidatedTargetDescriptor { get; }

    /// <summary>
    ///     Gets the configured write mode.
    /// </summary>
    public ReplicaWriteMode WriteMode { get; }

    /// <summary>
    ///     Materializes the outbound replica payload for this binding.
    /// </summary>
    /// <param name="projection">The projection instance to materialize.</param>
    /// <returns>The outbound payload.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="projection" /> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mapped materialization is requested without a mapper.</exception>
    public object Map(
        object projection
    )
    {
        ArgumentNullException.ThrowIfNull(projection);
        if (UsesDirectMaterialization)
        {
            return projection;
        }

        if (MapperDelegate is null)
        {
            throw new InvalidOperationException(
                $"Replica sink binding '{BindingIdentity}' does not have a mapper delegate for mapped materialization.");
        }

        return MapperDelegate(projection);
    }
}