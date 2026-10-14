using System;


namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Selects HTTP read endpoints, response DTOs, mappings, and matching client contracts for a projection.
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
///             <term>Inlet.Gateway.Generators</term>
///             <description>
///                 <c>{Projection}Controller</c> - Read-only GET endpoint.<br />
///                 <c>{Projection}Dto</c> - Response DTO.<br />
///                 <c>{Projection}Mapper</c> - Maps projection to DTO.
///             </description>
///         </item>
///         <item>
///             <term>Inlet.Client.Generators</term>
///             <description>
///                 Matching projection DTO and shared projection-feature inclusion in domain client registration.
///                 Feature registration does not activate subscriptions.
///             </description>
///         </item>
///     </list>
///     <para>
///         The HTTP route is derived from <c>ProjectionPathAttribute.Path</c>.
///         Silo registration requires <see cref="GenerateProjectionRegistrationAttribute" /> independently.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateProjectionEndpointsAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets a value indicating whether the legacy client subscription setting is enabled.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Current generators do not consume this setting. Client subscriptions use explicit
    ///         actions and host projection scanning independently of this HTTP marker.
    ///     </para>
    ///     <para>
    ///         This property remains available for source compatibility.
    ///     </para>
    /// </remarks>
    public bool GenerateClientSubscription { get; set; } = true;
}