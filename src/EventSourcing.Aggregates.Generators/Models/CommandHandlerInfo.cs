namespace Mississippi.EventSourcing.Aggregates.Generators.Models;

/// <summary>
///     Internal model for command handler information discovered during source generation.
/// </summary>
internal sealed class CommandHandlerInfo
{
    /// <summary>
    ///     Gets or sets the fully qualified type name of the aggregate.
    /// </summary>
    public string AggregateFullTypeName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the fully qualified type name of the command.
    /// </summary>
    public string CommandFullTypeName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the namespace of the command type.
    /// </summary>
    public string CommandNamespace { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the simple type name of the command.
    /// </summary>
    public string CommandTypeName { get; set; } = string.Empty;
}