using System;


namespace Mississippi.Sdk.Generators.Abstractions;

/// <summary>
///     Overrides the property name in generated DTOs.
/// </summary>
/// <remarks>
///     <para>
///         By default, generated DTOs use camelCase property names for JSON
///         serialization. Use this attribute to specify a custom name.
///     </para>
/// </remarks>
/// <example>
///     <code>
///         public sealed record UpdateProfile
///         {
///             [GeneratorPropertyName("display_name")]
///             public string DisplayName { get; init; }
///         }
///         // Generates: [JsonPropertyName("display_name")]
///     </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class GeneratorPropertyNameAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="GeneratorPropertyNameAttribute" /> class.
    /// </summary>
    /// <param name="name">The property name to use in generated DTOs.</param>
    public GeneratorPropertyNameAttribute(string name) => Name = name;

    /// <summary>
    ///     Gets the property name to use in generated DTOs.
    /// </summary>
    public string Name { get; }
}
