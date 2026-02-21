using System;


namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Marks a projection for MCP (Model Context Protocol) read tool generation.
/// </summary>
/// <remarks>
///     <para>
///         When applied to a projection record that also has <see cref="GenerateProjectionEndpointsAttribute" />,
///         the MCP generator produces a tool method that AI agents can invoke to read the projection state.
///     </para>
///     <para>
///         Projection read tools default to <c>ReadOnly = true</c>, <c>Destructive = false</c>,
///         <c>Idempotent = true</c>, and <c>OpenWorld = false</c>, matching the read-only
///         nature of projections. These defaults can be overridden when needed.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateMcpReadToolAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets an optional description for the generated read tool.
    /// </summary>
    /// <remarks>
    ///     This description is surfaced to AI models and should clearly explain what data
    ///     the tool returns and in what format. When not set, the generator produces a
    ///     default description from the projection name.
    /// </remarks>
    public string? Description { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the tool performs destructive updates.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see langword="false" /> for projection read tools since they only query state.
    /// </remarks>
    public bool Destructive { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether calling the tool repeatedly with the same arguments
    ///     has no additional effect on its environment.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see langword="true" /> for projection read tools since reads are inherently idempotent.
    /// </remarks>
    public bool Idempotent { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether the tool can interact with an "open world" of external entities.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see langword="false" /> for projection read tools since they query internal state.
    /// </remarks>
    public bool OpenWorld { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the tool only reads state without modifications.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see langword="true" /> for projection read tools since they have no side effects.
    /// </remarks>
    public bool ReadOnly { get; set; } = true;

    /// <summary>
    ///     Gets or sets a human-readable title for the tool that can be displayed to users.
    /// </summary>
    /// <remarks>
    ///     Unlike the tool name (which follows snake_case conventions), the title can include
    ///     spaces, proper casing, and natural language phrasing.
    /// </remarks>
    public string? Title { get; set; }

    /// <summary>
    ///     Gets or sets the tool name override. Defaults to a generated name based on the projection type.
    /// </summary>
    public string? ToolName { get; set; }
}