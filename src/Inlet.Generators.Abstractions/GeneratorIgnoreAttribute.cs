using System;


namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Indicates that a property should be excluded from generated DTOs.
/// </summary>
/// <remarks>
///     <para>
///         Use this attribute to prevent a property from appearing in generated
///         request or response DTOs. This is useful for internal properties that
///         should not be exposed via APIs.
///     </para>
/// </remarks>
/// <example>
///     <code>
///         public sealed record CreateOrder
///         {
///             public string ProductId { get; init; }
///
///             [GeneratorIgnore]
///             public DateTimeOffset InternalTimestamp { get; init; }
///         }
///     </code>
/// </example>
[AttributeUsage(AttributeTargets.Property)]
public sealed class GeneratorIgnoreAttribute : Attribute
{
}