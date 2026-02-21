using System;


namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Marks an aggregate for MCP (Model Context Protocol) tool generation.
/// </summary>
/// <remarks>
///     <para>
///         When applied to an aggregate record that also has <see cref="GenerateAggregateEndpointsAttribute" />,
///         the MCP generator produces a tools class exposing each command as an MCP tool
///         that AI agents can invoke via the MCP HTTP transport.
///     </para>
///     <para>
///         The generated tools class contains one method per command marked with <see cref="GenerateCommandAttribute" />,
///         with command properties mapped to tool parameters.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateMcpToolsAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets an optional description for the generated tools class.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Gets or sets the prefix for generated tool names. Defaults to the kebab-case aggregate name.
    /// </summary>
    public string? ToolPrefix { get; set; }
}