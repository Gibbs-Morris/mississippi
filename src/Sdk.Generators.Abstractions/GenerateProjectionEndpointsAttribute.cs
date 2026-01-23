using System;


namespace Mississippi.Sdk.Generators.Abstractions;

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
}