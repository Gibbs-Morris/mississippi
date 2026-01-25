using System;


namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     An assembly-level attribute that enforces secure endpoints across all Mississippi-generated APIs.
///     When present, the source generators will emit compile-time diagnostics if any endpoint lacks
///     proper authorization configuration.
/// </summary>
/// <remarks>
///     <para>
///         This attribute is intended for enterprise scenarios where security compliance requires all
///         endpoints to have explicit authorization. When applied, the following diagnostics may be emitted:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>MG010: Aggregate endpoint command lacks authorization configuration.</description>
///         </item>
///         <item>
///             <description>MG011: Projection endpoint lacks authorization configuration.</description>
///         </item>
///         <item>
///             <description>MG012: AllowAnonymous explicitly set on endpoint when secure endpoints are required.</description>
///         </item>
///     </list>
///     <para>
///         For a command to be considered secure, it must have at least one of the following set:
///     </para>
///     <list type="bullet">
///         <item>
///             <description><c>AuthorizeRoles</c> specifying one or more roles.</description>
///         </item>
///         <item>
///             <description><c>AuthorizePolicy</c> specifying a policy name.</description>
///         </item>
///         <item>
///             <description><c>RequiresAuthentication = true</c> for simple authentication checks.</description>
///         </item>
///     </list>
///     <para>
///         Similarly, projection endpoints must have controller-level authorization configured via
///         <see cref="GenerateProjectionEndpointsAttribute" />.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // In AssemblyInfo.cs or a GlobalUsings.cs file:
/// [assembly: RequireSecureEndpoints]
///
/// // This will cause a compile-time error if any command lacks authorization:
/// [GenerateAggregateEndpoints]
/// public record CreateOrderAggregate { ... }
///
/// [GenerateCommand(GenerateServerDto = true)]
/// public record CreateOrderCommand  // MG010: Missing authorization
/// {
///     public required string OrderId { get; init; }
/// }
///
/// // Fixed version:
/// [GenerateCommand(GenerateServerDto = true, AuthorizePolicy = "OrderManager")]
/// public record CreateOrderCommand
/// {
///     public required string OrderId { get; init; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class RequireSecureEndpointsAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets the list of endpoint type names that are exempt from secure endpoint requirements.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use this property to allow specific endpoints to bypass security checks without disabling
    ///         the entire secure endpoints requirement. This is useful for health checks, metrics endpoints,
    ///         or other infrastructure APIs that must remain unauthenticated.
    ///     </para>
    ///     <para>
    ///         Specify the full type name of the aggregate or projection to exempt. Multiple types can be
    ///         separated by semicolons.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// [assembly: RequireSecureEndpoints(ExemptTypes = "MyApp.HealthCheckProjection;MyApp.MetricsProjection")]
    /// </code>
    /// </example>
    /// <value>
    ///     A semicolon-separated list of fully-qualified type names to exempt from security requirements.
    /// </value>
    public string? ExemptTypes { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether MG012 should be treated as an error when
    ///     <c>AllowAnonymous = true</c> is explicitly set on an endpoint.
    /// </summary>
    /// <remarks>
    ///     When set to <c>true</c> (the default), any endpoint marked with <c>AllowAnonymous = true</c>
    ///     will emit MG012 as an error, preventing compilation. Set to <c>false</c> to emit MG012 as a
    ///     warning only, allowing anonymous endpoints but flagging them for review.
    /// </remarks>
    /// <value>
    ///     <c>true</c> if anonymous endpoints should fail compilation; otherwise, <c>false</c> for warnings only.
    ///     The default is <c>true</c>.
    /// </value>
    public bool TreatAnonymousAsError { get; set; } = true;
}