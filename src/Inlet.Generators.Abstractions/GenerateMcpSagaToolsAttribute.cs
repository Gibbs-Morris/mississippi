using System;


namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Marks a saga for MCP (Model Context Protocol) tool generation.
/// </summary>
/// <remarks>
///     <para>
///         When applied to a saga state record that also has <see cref="GenerateSagaEndpointsAttribute" />,
///         the MCP generator produces a tools class with:
///         <list type="bullet">
///             <item>A start tool that initiates the saga with input parameters.</item>
///             <item>A status tool that reads the current saga state.</item>
///         </list>
///     </para>
///     <para>
///         Descriptions and titles are defined once on this attribute and the generator derives
///         context-specific descriptions for the start and status tools automatically.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateMcpSagaToolsAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets a description of what this saga does.
    /// </summary>
    /// <remarks>
    ///     The generator derives start and status tool descriptions from this base description.
    ///     When not set, a default description is generated from the saga name.
    /// </remarks>
    public string? Description { get; set; }

    /// <summary>
    ///     Gets or sets the tool name prefix for generated saga tools.
    /// </summary>
    /// <remarks>
    ///     Defaults to the snake_case saga name. The generator appends <c>_status</c>
    ///     for the status tool and uses the prefix directly for the start tool.
    /// </remarks>
    public string? ToolPrefix { get; set; }

    /// <summary>
    ///     Gets or sets a human-readable title for the saga tools.
    /// </summary>
    /// <remarks>
    ///     The generator derives context-specific titles: e.g., "Transfer Funds" becomes
    ///     "Transfer Funds" (start) and "Transfer Funds Status" (status).
    /// </remarks>
    public string? Title { get; set; }
}
