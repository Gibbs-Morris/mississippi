namespace Mississippi.EventSourcing.Aggregates.Generators.Models;

/// <summary>
///     Contains information about a command that can be executed on an aggregate.
/// </summary>
internal sealed class CommandInfo
{
    /// <summary>
    ///     Gets or sets the fully qualified type name of the command.
    /// </summary>
    public string FullTypeName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the method name to generate (command name without common prefixes).
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the namespace of the command type.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the simple type name of the command.
    /// </summary>
    public string TypeName { get; set; } = string.Empty;
}