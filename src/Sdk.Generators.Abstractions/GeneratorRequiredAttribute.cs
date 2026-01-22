using System;


namespace Mississippi.Sdk.Generators.Abstractions;

/// <summary>
///     Overrides the required inference for a property in generated DTOs.
/// </summary>
/// <remarks>
///     <para>
///         By default, the generator infers required properties based on:
///         <list type="bullet">
///             <item>Non-nullable reference types are required.</item>
///             <item>Properties with <c>required</c> modifier are required.</item>
///             <item>Properties with default values are optional.</item>
///         </list>
///     </para>
///     <para>
///         Use this attribute to override the default inference when needed.
///     </para>
/// </remarks>
/// <example>
///     <code>
///         public sealed record CreateUser
///         {
///             // Force this nullable property to be required
///             [GeneratorRequired]
///             public string? Email { get; init; }
/// 
///             // Force this non-nullable property to be optional
///             [GeneratorRequired(false)]
///             public string NickName { get; init; } = "Anonymous";
///         }
///     </code>
/// </example>
[AttributeUsage(AttributeTargets.Property)]
public sealed class GeneratorRequiredAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="GeneratorRequiredAttribute" /> class.
    /// </summary>
    /// <param name="isRequired">Whether the property should be required in generated DTOs.</param>
    public GeneratorRequiredAttribute(
        bool isRequired = true
    ) =>
        IsRequired = isRequired;

    /// <summary>
    ///     Gets a value indicating whether the property should be required.
    /// </summary>
    public bool IsRequired { get; }
}