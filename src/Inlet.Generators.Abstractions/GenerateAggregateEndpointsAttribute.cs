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
    ///     Gets or sets the default authorization policy for all commands on this aggregate.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When set, generated controller actions will include
    ///         <c>[Authorize(Policy = "...")]</c> unless the individual command specifies
    ///         its own authorization or <see cref="GenerateCommandAttribute.AllowAnonymous" />.
    ///     </para>
    ///     <para>
    ///         Policies must be registered in the application's authorization configuration.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///         [GenerateAggregateEndpoints(DefaultAuthorizePolicy = "RequireAdminClaim")]
    ///         public sealed record PolicyProtectedAggregate { }
    ///     </code>
    /// </example>
    public string? DefaultAuthorizePolicy { get; set; }

    /// <summary>
    ///     Gets or sets the default authorization roles for all commands on this aggregate.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When set, generated controller actions will include
    ///         <c>[Authorize(Roles = "...")]</c> unless the individual command specifies
    ///         its own authorization or <see cref="GenerateCommandAttribute.AllowAnonymous" />.
    ///     </para>
    ///     <para>
    ///         Multiple roles can be specified as a comma-separated string.
    ///         The authorization check passes if the user has any of the listed roles.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///         [GenerateAggregateEndpoints(DefaultAuthorizeRoles = "Admin,Manager")]
    ///         public sealed record SecureAggregate { }
    ///     </code>
    /// </example>
    public string? DefaultAuthorizeRoles { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether all commands on this aggregate require authentication by default.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to <see langword="false" />. When set to <see langword="true" />,
    ///         generated controller actions will include <c>[Authorize]</c> (with no
    ///         specific roles or policy) unless the individual command specifies
    ///         <see cref="GenerateCommandAttribute.AllowAnonymous" />.
    ///     </para>
    ///     <para>
    ///         This is the simplest form of authentication requirement—any authenticated
    ///         user can access the endpoint.
    ///     </para>
    /// </remarks>
    public bool DefaultRequiresAuthentication { get; set; }

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
    ///     Gets or sets a value indicating whether to generate the ASP.NET controller for this aggregate.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to <see langword="true" />. When set to <see langword="false" />,
    ///         the <c>Sdk.Server.Generators</c> will skip controller generation for this aggregate.
    ///     </para>
    ///     <para>
    ///         Use this when you want to define a custom controller or when the aggregate
    ///         should not be exposed via HTTP endpoints.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///         [GenerateAggregateEndpoints(GenerateController = false)]
    ///         public sealed record InternalAggregate { }
    ///     </code>
    /// </example>
    public bool GenerateController { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether to generate silo registration code for this aggregate.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to <see langword="true" />. When set to <see langword="false" />,
    ///         the <c>Sdk.Silo.Generators</c> will skip generating the <c>Add{Aggregate}()</c>
    ///         extension method.
    ///     </para>
    ///     <para>
    ///         Use this when you need custom silo configuration or when the aggregate
    ///         registrations are managed manually.
    ///     </para>
    /// </remarks>
    public bool GenerateSiloRegistrations { get; set; } = true;

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