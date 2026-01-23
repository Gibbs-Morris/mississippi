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
    ///     Gets or sets the HTTP method for this command endpoint.
    /// </summary>
    /// <remarks>
    ///     Defaults to <c>POST</c>. Commands typically use POST for
    ///     state-changing operations.
    /// </remarks>
    public string HttpMethod { get; set; } = "POST";

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