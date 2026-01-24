using System;


namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Marks a projection record for read endpoint code generation.
/// </summary>
/// <remarks>
///     <para>
///         Projections must also have <c>ProjectionPathAttribute</c> applied for full generation.
///     </para>
///     <para>
///         When applied, the following code is generated:
///     </para>
///     <list type="bullet">
///         <item>
///             <term>Sdk.Silo.Generators</term>
///             <description>
///                 <c>Add{Projection}()</c> extension method that registers
///                 reducers and snapshot converters.
///             </description>
///         </item>
///         <item>
///             <term>Sdk.Server.Generators</term>
///             <description>
///                 <c>{Projection}Controller</c> - Read-only GET endpoint.<br />
///                 <c>{Projection}Dto</c> - Response DTO.<br />
///                 <c>{Projection}Mapper</c> - Maps projection to DTO.
///             </description>
///         </item>
///     </list>
///     <para>
///         The HTTP route is derived from <c>ProjectionPathAttribute.Path</c>.
///     </para>
/// </remarks>
/// <example>
///     <code>
///         [ProjectionPath("bank-account-balance")]
///         [GenerateProjectionEndpoints]
///         [BrookName("CONTOSO", "BANKING", "ACCOUNT")]
///         public sealed record BankAccountBalanceProjection
///         {
///             public decimal Balance { get; init; }
///         }
///     </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateProjectionEndpointsAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets a value indicating whether this projection's endpoints allow anonymous access.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When set to <see langword="true" />, the generated projection controller will
    ///         include <c>[AllowAnonymous]</c>, overriding any authorization requirements
    ///         set via assembly-level security attributes.
    ///     </para>
    ///     <para>
    ///         Use sparingly for projections that must be publicly accessible,
    ///         such as public dashboards or status endpoints.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///         [GenerateProjectionEndpoints(AllowAnonymous = true)]
    ///         public sealed record PublicStatusProjection { }
    ///     </code>
    /// </example>
    public bool AllowAnonymous { get; set; }

    /// <summary>
    ///     Gets or sets the authorization policy required for this projection's endpoints.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When set, the generated projection controller will include
    ///         <c>[Authorize(Policy = "...")]</c> at the controller level.
    ///     </para>
    ///     <para>
    ///         Policies must be registered in the application's authorization configuration.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///         [GenerateProjectionEndpoints(AuthorizePolicy = "CanViewReports")]
    ///         public sealed record SalesReportProjection { }
    ///     </code>
    /// </example>
    public string? AuthorizePolicy { get; set; }

    /// <summary>
    ///     Gets or sets the authorization roles required for this projection's endpoints.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When set, the generated projection controller will include
    ///         <c>[Authorize(Roles = "...")]</c> at the controller level.
    ///     </para>
    ///     <para>
    ///         Multiple roles can be specified as a comma-separated string.
    ///         The authorization check passes if the user has any of the listed roles.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///         [GenerateProjectionEndpoints(AuthorizeRoles = "Analyst,Admin")]
    ///         public sealed record FinancialReportProjection { }
    ///     </code>
    /// </example>
    public string? AuthorizeRoles { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether to generate client subscription code.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When <c>true</c> (default), the Client generator produces code for
    ///         real-time subscription to projection updates via SignalR.
    ///     </para>
    ///     <para>
    ///         Set to <c>false</c> for projections that don't need real-time updates
    ///         on the client.
    ///     </para>
    /// </remarks>
    public bool GenerateClientSubscription { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether to generate the server-side controller for this projection.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to <see langword="true" />. When set to <see langword="false" />,
    ///         the <c>{Projection}Controller</c> and associated DTOs/mappers will not be generated.
    ///     </para>
    ///     <para>
    ///         Use this when you have a custom controller implementation or want
    ///         to expose the projection through a different mechanism.
    ///     </para>
    /// </remarks>
    public bool GenerateController { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether to generate silo registration code for this projection.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to <see langword="true" />. When set to <see langword="false" />,
    ///         the <c>Add{Projection}()</c> extension method for silo configuration
    ///         will not be generated.
    ///     </para>
    ///     <para>
    ///         Use this when you have custom silo configuration or want to handle
    ///         projection registration manually.
    ///     </para>
    /// </remarks>
    public bool GenerateSiloRegistrations { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether this projection's endpoints require authentication.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When set to <see langword="true" />, the generated projection controller will
    ///         include <c>[Authorize]</c> at the controller level (with no specific roles or policy).
    ///     </para>
    ///     <para>
    ///         This is the simplest form of authentication requirement—any authenticated
    ///         user can access the projection endpoints.
    ///     </para>
    /// </remarks>
    public bool RequiresAuthentication { get; set; }
}