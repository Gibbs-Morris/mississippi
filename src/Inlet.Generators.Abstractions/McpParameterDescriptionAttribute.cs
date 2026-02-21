using System;


namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Provides a description for a command property when exposed as an MCP tool parameter.
/// </summary>
/// <remarks>
///     <para>
///         Apply this attribute to properties on command records to provide rich descriptions
///         that help LLMs understand parameter purpose, constraints, and expected values.
///         The description is emitted as a <c>[Description("...")]</c> attribute on the
///         corresponding tool method parameter.
///     </para>
///     <para>
///         When this attribute is absent, the generator produces a default description
///         derived from the property name (e.g., <c>InitialDeposit</c> becomes <c>"initial deposit"</c>).
///     </para>
/// </remarks>
/// <example>
///     <code>
///     public sealed record DepositFunds
///     {
///         [McpParameterDescription("The amount to deposit in the account's currency. Must be greater than zero.")]
///         public decimal Amount { get; init; }
///     }
///     </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class McpParameterDescriptionAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="McpParameterDescriptionAttribute" /> class
    ///     with the specified description text.
    /// </summary>
    /// <param name="description">The description text for the parameter.</param>
    public McpParameterDescriptionAttribute(
        string description
    ) =>
        Description = description;

    /// <summary>
    ///     Gets the description text for the parameter.
    /// </summary>
    public string Description { get; }
}