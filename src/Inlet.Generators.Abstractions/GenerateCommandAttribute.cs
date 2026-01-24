using System;


namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Marks a command record for endpoint code generation.
/// </summary>
/// <remarks>
///     <para>
///         Commands must be defined in a <c>Commands</c> sub-namespace of an aggregate
///         that has <see cref="GenerateAggregateEndpointsAttribute" /> applied.
///     </para>
///     <para>
///         When applied, the following code is generated:
///     </para>
///     <list type="bullet">
///         <item>
///             <term>Server</term>
///             <description>
///                 <c>{Command}Dto</c> - Request DTO with JSON serialization attributes.<br />
///                 <c>{Command}DtoMapper</c> - Maps DTO to domain command.<br />
///                 Controller action - <c>[HttpPost("{Route}")]</c> endpoint.
///             </description>
///         </item>
///         <item>
///             <term>Client</term>
///             <description>
///                 <c>{Command}Action</c> - Flux action record with EntityId.<br />
///                 <c>{Command}RequestDto</c> - HTTP request body DTO.<br />
///                 <c>{Command}ActionMapper</c> - Maps action to request DTO.<br />
///                 <c>{Command}Effect</c> - HTTP POST effect handler.
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
///         [GenerateCommand(Route = "deposit")]
///         [GenerateSerializer]
///         public sealed record DepositFunds
///         {
///             [Id(0)] public decimal Amount { get; init; }
///         }
///     </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateCommandAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets a value indicating whether this command endpoint allows anonymous access.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When set to <see langword="true" />, the generated controller action will
    ///         include <c>[AllowAnonymous]</c>, overriding any authorization requirements
    ///         set at the aggregate level or via assembly-level security attributes.
    ///     </para>
    ///     <para>
    ///         Use sparingly for endpoints that must be accessible without authentication,
    ///         such as health checks or public queries.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///         [GenerateCommand(Route = "ping", AllowAnonymous = true)]
    ///         public sealed record Ping { }
    ///     </code>
    /// </example>
    public bool AllowAnonymous { get; set; }

    /// <summary>
    ///     Gets or sets the authorization policy required for this command endpoint.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When set, the generated controller action will include
    ///         <c>[Authorize(Policy = "...")]</c>. This overrides any
    ///         <see cref="GenerateAggregateEndpointsAttribute.DefaultAuthorizePolicy" />
    ///         set at the aggregate level.
    ///     </para>
    ///     <para>
    ///         Policies must be registered in the application's authorization configuration.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///         [GenerateCommand(Route = "delete", AuthorizePolicy = "CanDeleteRecords")]
    ///         public sealed record DeleteRecord { }
    ///     </code>
    /// </example>
    public string? AuthorizePolicy { get; set; }

    /// <summary>
    ///     Gets or sets the authorization roles required for this command endpoint.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When set, the generated controller action will include
    ///         <c>[Authorize(Roles = "...")]</c>. This overrides any
    ///         <see cref="GenerateAggregateEndpointsAttribute.DefaultAuthorizeRoles" />
    ///         set at the aggregate level.
    ///     </para>
    ///     <para>
    ///         Multiple roles can be specified as a comma-separated string.
    ///         The authorization check passes if the user has any of the listed roles.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///         [GenerateCommand(Route = "approve", AuthorizeRoles = "Manager,Admin")]
    ///         public sealed record ApproveTransaction { }
    ///     </code>
    /// </example>
    public string? AuthorizeRoles { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether to generate the Flux action for this command.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to <see langword="true" />. When set to <see langword="false" />,
    ///         the <c>{Command}Action</c>, <c>{Command}ExecutingAction</c>,
    ///         <c>{Command}SucceededAction</c>, and <c>{Command}FailedAction</c>
    ///         records will not be generated.
    ///     </para>
    /// </remarks>
    public bool GenerateClientActions { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether to generate the client-side DTO for this command.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to <see langword="true" />. When set to <see langword="false" />,
    ///         the <c>{Command}RequestDto</c> record will not be generated.
    ///     </para>
    /// </remarks>
    public bool GenerateClientDto { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether to generate the HTTP effect for this command.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to <see langword="true" />. When set to <see langword="false" />,
    ///         the <c>{Command}Effect</c> class will not be generated.
    ///     </para>
    /// </remarks>
    public bool GenerateClientEffect { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether to generate the client-side action mapper for this command.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to <see langword="true" />. When set to <see langword="false" />,
    ///         the <c>{Command}ActionMapper</c> class will not be generated.
    ///     </para>
    /// </remarks>
    public bool GenerateClientMapper { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether to generate the state reducers for this command.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to <see langword="true" />. When set to <see langword="false" />,
    ///         the reducer methods for this command's actions will not be generated.
    ///     </para>
    /// </remarks>
    public bool GenerateClientReducers { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether to generate state properties for this command.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to <see langword="true" />. When set to <see langword="false" />,
    ///         this command will not contribute properties to the feature state.
    ///     </para>
    /// </remarks>
    public bool GenerateClientState { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether to generate the server-side DTO for this command.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to <see langword="true" />. When set to <see langword="false" />,
    ///         the <c>{Command}Dto</c> record will not be generated.
    ///     </para>
    ///     <para>
    ///         Use this when you have a custom DTO or the command record itself
    ///         is suitable for direct serialization.
    ///     </para>
    /// </remarks>
    public bool GenerateServerDto { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether to generate the server-side DTO mapper for this command.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to <see langword="true" />. When set to <see langword="false" />,
    ///         the <c>{Command}DtoMapper</c> class will not be generated.
    ///     </para>
    ///     <para>
    ///         Use this when you have custom mapping logic or when using
    ///         <see cref="GenerateServerDto" /> = <see langword="false" />.
    ///     </para>
    /// </remarks>
    public bool GenerateServerMapper { get; set; } = true;

    /// <summary>
    ///     Gets or sets the HTTP method for this command endpoint.
    /// </summary>
    /// <remarks>
    ///     Defaults to <c>POST</c>. Commands typically use POST for
    ///     state-changing operations.
    /// </remarks>
    public string HttpMethod { get; set; } = "POST";

    /// <summary>
    ///     Gets or sets a value indicating whether this command endpoint requires authentication.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When set to <see langword="true" />, the generated controller action will
    ///         include <c>[Authorize]</c> (with no specific roles or policy).
    ///         This overrides <see cref="GenerateAggregateEndpointsAttribute.DefaultRequiresAuthentication" />.
    ///     </para>
    ///     <para>
    ///         This is the simplest form of authentication requirement—any authenticated
    ///         user can access the endpoint.
    ///     </para>
    /// </remarks>
    public bool RequiresAuthentication { get; set; }

    /// <summary>
    ///     Gets or sets the HTTP route segment for this command endpoint.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to kebab-case of the command name.
    ///         For example, <c>DepositFunds</c> becomes <c>deposit-funds</c>.
    ///     </para>
    ///     <para>
    ///         The full route is: <c>api/aggregates/{aggregate}/{entityId}/{Route}</c>.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///         [GenerateCommand(Route = "deposit")]
    ///         public sealed record DepositFunds { }
    ///         // Generates: POST api/aggregates/bank-account/{entityId}/deposit
    ///     </code>
    /// </example>
    public string? Route { get; set; }
}