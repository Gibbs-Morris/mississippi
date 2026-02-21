using System;


namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Marks an aggregate for MCP (Model Context Protocol) tool generation.
/// </summary>
/// <remarks>
///     <para>
///         When applied to an aggregate record, the MCP generator produces a tools class
///         exposing each command as an MCP tool that AI agents can invoke via the MCP HTTP transport.
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
    ///     Gets or sets an optional prefix for generated tool names.
    ///     When this value is <see langword="null" /> or empty, no prefix is applied and tool names are
    ///     derived solely from the aggregate and command names.
    ///     The generator normalizes the final tool name (including any prefix) to <c>snake_case</c>, so callers
    ///     should expect <c>snake_case</c> tool identifiers.
    /// </summary>
    public string? ToolPrefix { get; set; }
}