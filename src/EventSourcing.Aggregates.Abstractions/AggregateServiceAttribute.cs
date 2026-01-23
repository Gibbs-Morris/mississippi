using System;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Marks an aggregate type for source generation of a strongly-typed aggregate service and optional HTTP API.
/// </summary>
/// <remarks>
///     <para>
///         When applied to an aggregate type, the source generator will:
///         <list type="number">
///             <item>
///                 Scan for <see cref="CommandHandlerBase{TCommand, TSnapshot}" /> implementations
///                 where TSnapshot matches the decorated aggregate type.
///             </item>
///             <item>
///                 Generate an <c>I{AggregateName}Service</c> interface with a typed method per command.
///             </item>
///             <item>
///                 Generate a <c>{AggregateName}Service</c> implementation that wraps
///                 <see cref="IGenericAggregateGrain{TAggregate}" />.
///             </item>
///             <item>
///                 Optionally generate an HTTP API controller if <see cref="GenerateApi" /> is true.
///             </item>
///         </list>
///     </para>
///     <para>
///         The aggregate type must also be decorated with
///         <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />
///         to define the event stream identity.
///     </para>
/// </remarks>
/// <example>
///     <code>
///         [AggregateService("users")]
///         [BrookName("SPRING", "CHAT", "USER")]
///         [GenerateSerializer]
///         public sealed record class UserAggregate { ... }
///     </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AggregateServiceAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AggregateServiceAttribute" /> class.
    /// </summary>
    /// <param name="route">The route segment for the generated HTTP API (e.g., "users").</param>
    public AggregateServiceAttribute(
        string route
    ) =>
        Route = route ?? throw new ArgumentNullException(nameof(route));

    /// <summary>
    ///     Gets or sets the authorization policy name for the generated API controller.
    /// </summary>
    /// <remarks>
    ///     When set, the generated controller will be decorated with
    ///     <c>[Authorize(Policy = "...")]</c>.
    /// </remarks>
    public string? Authorize { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether to generate an HTTP API controller.
    /// </summary>
    /// <remarks>
    ///     Defaults to <c>true</c>. Set to <c>false</c> to generate only the service layer
    ///     without an HTTP API.
    /// </remarks>
    public bool GenerateApi { get; set; } = true;

    /// <summary>
    ///     Gets the route segment for the generated HTTP API.
    /// </summary>
    public string Route { get; }
}