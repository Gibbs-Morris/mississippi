using System;


namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Marks an aggregate for infrastructure code generation.
/// </summary>
/// <remarks>
///     <para>
///         When applied to an aggregate record that also has
///         <c>BrookNameAttribute</c> and <c>SnapshotStorageNameAttribute</c>,
///         the following generators will produce code:
///     </para>
///     <list type="bullet">
///         <item>
///             <term>Sdk.Silo.Generators</term>
///             <description>
///                 Generates <c>Add{Aggregate}()</c> extension method that registers
///                 event types, command handlers, reducers, and snapshot converters.
///             </description>
///         </item>
///         <item>
///             <term>Sdk.Server.Generators</term>
///             <description>
///                 Generates an aggregate controller with endpoints for each command
///                 marked with <see cref="GenerateCommandAttribute" />.
///             </description>
///         </item>
///         <item>
///             <term>Sdk.Client.Generators</term>
///             <description>
///                 Generates feature state, reducers, and registration for Flux-style
///                 state management.
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
///         [BrookName("CONTOSO", "BANKING", "ACCOUNT")]
///         [SnapshotStorageName("CONTOSO", "BANKING", "ACCOUNTSTATE")]
///         [GenerateAggregateEndpoints]
///         [GenerateSerializer]
///         public sealed record BankAccountAggregate
///         {
///             public decimal Balance { get; init; }
///             public bool IsOpen { get; init; }
///         }
///     </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateAggregateEndpointsAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets the feature key for client-side state management.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to camelCase of the aggregate name without the "Aggregate" suffix.
    ///         For example, <c>BankAccountAggregate</c> becomes <c>bankAccount</c>.
    ///     </para>
    ///     <para>
    ///         This key is used by Reservoir/Fluxor for state slice identification.
    ///     </para>
    /// </remarks>
    public string? FeatureKey { get; set; }

    /// <summary>
    ///     Gets or sets the route prefix for aggregate command endpoints.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to kebab-case of the aggregate name without the "Aggregate" suffix.
    ///         For example, <c>BankAccountAggregate</c> becomes <c>bank-account</c>.
    ///     </para>
    ///     <para>
    ///         The full route pattern is: <c>api/aggregates/{RoutePrefix}/{entityId}/{command}</c>.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///         [GenerateAggregateEndpoints(RoutePrefix = "accounts")]
    ///         public sealed record BankAccountAggregate { }
    ///         // Generates: api/aggregates/accounts/{entityId}/...
    ///     </code>
    /// </example>
    public string? RoutePrefix { get; set; }
}