namespace Mississippi.Ripples.Generators.Models;

/// <summary>
///     Contains extracted information about a command method marked with [CommandRoute].
/// </summary>
internal sealed class CommandInfo
{
    /// <summary>
    ///     Gets a value indicating whether the command has a request body parameter.
    /// </summary>
    public bool HasParameter => ParameterType is not null;

    /// <summary>
    ///     Gets or sets the method name.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the parameter name, if any.
    /// </summary>
    public string? ParameterName { get; set; }

    /// <summary>
    ///     Gets or sets the fully qualified parameter type, if any.
    /// </summary>
    public string? ParameterType { get; set; }

    /// <summary>
    ///     Gets or sets the return type (typically OperationResult or Task&lt;OperationResult&gt;).
    /// </summary>
    public string ReturnType { get; set; } = "Task<OperationResult>";

    /// <summary>
    ///     Gets or sets the HTTP route segment for this command.
    /// </summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the XML documentation summary, if any.
    /// </summary>
    public string? XmlDocSummary { get; set; }
}