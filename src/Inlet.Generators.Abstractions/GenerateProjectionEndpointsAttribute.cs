using System;


namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Marks a projection record for read endpoint code generation.
/// </summary>
/// <remarks>
///     <para>
///         When applied, the following code is generated:
///     </para>
///     <list type="bullet">
///         <item>
///             <term>Sdk.Silo.Generators</term>
///             <description>
///                 <c>Add{Projection}()</c> extension method that registers
///                 reducers, snapshot converters, and projection brook mappings.
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
///         <item>
///             <term>Sdk.Client.Generators</term>
///             <description>
///                 Client DTO, reducers, and projection DTO registration.
///             </description>
///         </item>
///     </list>
///     <para>
///         The HTTP route is derived from <see cref="Path" />.
///         If not set, defaults to kebab-case of the type name without the "Projection" suffix.
///         For example, <c>BankAccountBalanceProjection</c> becomes <c>bank-account-balance</c>.
///     </para>
///     <para>
///         The path is used for:
///     </para>
///     <list type="bullet">
///         <item>API routes: <c>GET /api/projections/{path}/{entityId}</c>.</item>
///         <item>SignalR subscriptions: subscribe to <c>{path}</c> with entity ID.</item>
///         <item>Projection brook registry: mapping <c>{path}</c> to a brook name.</item>
///     </list>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateProjectionEndpointsAttribute : Attribute
{
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
    ///     Gets or sets the path for this projection.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to kebab-case of the projection name without the "Projection" suffix.
    ///         For example, <c>BankAccountBalanceProjection</c> becomes <c>bank-account-balance</c>.
    ///     </para>
    ///     <para>
    ///         The full route pattern is: <c>api/projections/{Path}/{entityId}</c>.
    ///     </para>
    ///     <para>
    ///         The path is also used for SignalR subscriptions and projection brook registry mappings.
    ///         Enable <c>RequireExplicitProjectionPaths</c> in your builder options to fail at
    ///         startup when any projection relies on auto-derived paths.
    ///     </para>
    /// </remarks>
    public string? Path { get; set; }
}