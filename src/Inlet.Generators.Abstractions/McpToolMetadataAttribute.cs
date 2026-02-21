using System;


namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Provides MCP (Model Context Protocol) metadata for a generated tool method.
/// </summary>
/// <remarks>
///     <para>
///         Apply this attribute to command records that also have <see cref="GenerateCommandAttribute" />
///         to control the MCP tool annotations emitted by the source generator. These annotations
///         help LLMs understand the tool's behavior, side effects, and intended usage.
///     </para>
///     <para>
///         When this attribute is absent, the generator uses sensible defaults based on
///         the MCP specification: commands default to <c>Destructive = true</c>,
///         <c>ReadOnly = false</c>, <c>Idempotent = false</c>, <c>OpenWorld = false</c>.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     [GenerateCommand(Route = "deposit")]
///     [McpToolMetadata(
///         Title = "Deposit Funds",
///         Description = "Deposits funds into a bank account. Increases the account balance by the specified amount.",
///         Destructive = false,
///         Idempotent = false,
///         ReadOnly = false,
///         OpenWorld = false)]
///     public sealed record DepositFunds { ... }
///     </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class McpToolMetadataAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets a human-readable description of the tool for LLM consumption.
    /// </summary>
    /// <remarks>
    ///     This description is surfaced to AI models and should clearly explain what the tool does,
    ///     what side effects it has, and any constraints on its usage. When not set, the generator
    ///     produces a default description from the command and aggregate names.
    /// </remarks>
    public string? Description { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the tool performs destructive updates to its environment.
    /// </summary>
    /// <remarks>
    ///     When <see langword="true" />, the tool might delete or irreversibly modify data.
    ///     When <see langword="false" />, the tool only performs additive updates.
    ///     Defaults to <see langword="true" /> per MCP specification when not set.
    /// </remarks>
    public bool Destructive { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether calling the tool repeatedly with the same arguments
    ///     has no additional effect on its environment.
    /// </summary>
    /// <remarks>
    ///     When <see langword="true" />, duplicate calls are safe and produce the same result.
    ///     When <see langword="false" />, each call may produce additional side effects.
    ///     Defaults to <see langword="false" /> per MCP specification when not set.
    /// </remarks>
    public bool Idempotent { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the tool can interact with an "open world" of external entities.
    /// </summary>
    /// <remarks>
    ///     When <see langword="true" />, the tool interacts with unpredictable external systems (e.g., web APIs).
    ///     When <see langword="false" />, the tool operates within a closed, well-defined domain.
    ///     Defaults to <see langword="false" /> for domain commands (they operate on internal aggregates).
    /// </remarks>
    public bool OpenWorld { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the tool only reads state without modifications.
    /// </summary>
    /// <remarks>
    ///     When <see langword="true" />, the tool has no side effects beyond computation.
    ///     When <see langword="false" />, the tool may modify state.
    ///     Defaults to <see langword="false" /> for commands (they are state-changing by nature).
    /// </remarks>
    public bool ReadOnly { get; set; }

    /// <summary>
    ///     Gets or sets a human-readable title for the tool that can be displayed to users.
    /// </summary>
    /// <remarks>
    ///     Unlike the tool name (which follows snake_case conventions), the title can include
    ///     spaces, proper casing, and natural language phrasing. When not set, no title is emitted.
    /// </remarks>
    public string? Title { get; set; }
}