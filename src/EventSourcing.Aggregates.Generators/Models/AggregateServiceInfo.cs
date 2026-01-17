using System.Collections.Generic;


namespace Mississippi.EventSourcing.Aggregates.Generators.Models;

/// <summary>
///     Contains extracted information about an aggregate type marked with [AggregateService].
/// </summary>
internal sealed class AggregateServiceInfo
{
    /// <summary>
    ///     Gets or sets the authorization policy name, if any.
    /// </summary>
    public string? Authorize { get; set; }

    /// <summary>
    ///     Gets or sets the list of commands that can be executed on this aggregate.
    /// </summary>
    public List<CommandInfo> Commands { get; set; } = new();

    /// <summary>
    ///     Gets or sets the fully qualified type name of the aggregate.
    /// </summary>
    public string FullTypeName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a value indicating whether to generate API controllers.
    /// </summary>
    public bool GenerateApi { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the aggregate type is internal.
    /// </summary>
    /// <remarks>
    ///     When true, generated services will be internal. When false, they will be public.
    /// </remarks>
    public bool IsInternal { get; set; }

    /// <summary>
    ///     Gets or sets the namespace of the aggregate type.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the HTTP route for the API controller.
    /// </summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the simple type name (without namespace and without 'Aggregate' suffix).
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the simple type name (without namespace).
    /// </summary>
    public string TypeName { get; set; } = string.Empty;
}